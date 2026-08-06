namespace FinanceSentry.Modules.Analytics.Application.Services;

using System.Diagnostics;
using Microsoft.Extensions.Options;
using Npgsql;

/// <summary>
/// Executes validated SELECTs on the SELECT-only <c>fs_readonly</c> connection. Every query runs in a
/// <c>READ ONLY</c> transaction with two transaction-local settings applied via <c>set_config</c>:
/// <c>app.current_user_id</c> (the RLS/security-barrier pin — the agent's SQL can't widen it) and
/// <c>statement_timeout</c> (the runaway-time guard). Rows are read up to <c>MaxRows</c>; one extra row
/// beyond the cap flips <c>Truncated</c>. The transaction is always rolled back — nothing is persisted.
/// </summary>
public sealed class ReadOnlyQueryExecutor(IOptions<AnalyticsOptions> options) : IReadOnlyQueryExecutor
{
    private const string QueryCanceledSqlState = "57014"; // statement_timeout fired
    private readonly AnalyticsOptions _options = options.Value;

    public async Task<QueryExecution> ExecuteAsync(Guid userId, string sql, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        await using var connection = new NpgsqlConnection(_options.ReadOnlyConnectionString);
        await connection.OpenAsync(ct);

        await using var transaction = await connection.BeginTransactionAsync(ct);

        // Layered per-query setup, all transaction-local so a pooled connection resets on ROLLBACK:
        //   1. SET TRANSACTION READ ONLY  — must precede any query in the txn; blocks writes outright.
        //   2. app.current_user_id        — the security-barrier views filter on this; the agent's SQL
        //                                    cannot widen it (a missing value fails closed to zero rows).
        //   3. statement_timeout          — the runaway-time guard.
        //   4. SET LOCAL ROLE fs_readonly — drops to the SELECT-on-views-only role; even a validator
        //                                    bypass has no write path and no base-table reach.
        await using (var setup = new NpgsqlCommand(
            "SET TRANSACTION READ ONLY; "
            + "SELECT set_config('app.current_user_id', @uid, true), set_config('statement_timeout', @timeout, true); "
            + "SET LOCAL ROLE fs_readonly;",
            connection, transaction))
        {
            setup.Parameters.AddWithValue("uid", userId.ToString());
            setup.Parameters.AddWithValue("timeout", _options.StatementTimeoutMs.ToString());
            await setup.ExecuteNonQueryAsync(ct);
        }

        try
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync(ct);

            var columns = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                columns[i] = reader.GetName(i);
            }

            var rows = new List<IReadOnlyList<object?>>();
            var truncated = false;
            while (await reader.ReadAsync(ct))
            {
                if (rows.Count >= _options.MaxRows)
                {
                    truncated = true;
                    break;
                }

                var row = new object?[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    row[i] = value is DBNull ? null : value;
                }

                rows.Add(row);
            }

            stopwatch.Stop();
            return new QueryExecution(columns, rows, truncated, TooLarge: false, Reason: null, (int)stopwatch.ElapsedMilliseconds);
        }
        catch (PostgresException ex) when (ex.SqlState == QueryCanceledSqlState)
        {
            stopwatch.Stop();
            return new QueryExecution(
                [],
                [],
                Truncated: false,
                TooLarge: true,
                "query exceeded the time/row budget — narrow it (add filters, a date range, or LIMIT)",
                (int)stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            await transaction.RollbackAsync(ct);
        }
    }
}
