namespace FinanceSentry.Modules.Retention.Application.Services;

using System.Text.Json;
using FinanceSentry.Infrastructure.Observability;
using FinanceSentry.Modules.Retention.Application.Downsamplers;
using FinanceSentry.Modules.Retention.Domain;
using FinanceSentry.Modules.Retention.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

/// <summary>
/// Compacts old fine-grained history to weekly resolution (feature 024, US3). Each target keeps the
/// newest row per (partition, ISO-week) beyond its window and deletes the rest, inside a transaction so
/// a chart never observes a half-collapsed series. Records a <see cref="RetentionRunType.Downsample"/> run.
/// </summary>
public sealed class DownsampleService(
    RetentionDbContext db,
    JobMetrics metrics,
    ILogger<DownsampleService> logger)
{
    public async Task<RetentionRun> RunAsync(CancellationToken ct = default)
    {
        var run = new RetentionRun { RunType = RetentionRunType.Downsample, StartedAt = DateTimeOffset.UtcNow };
        var results = new List<TableResult>();
        var failures = 0;
        var targets = DownsampleTargets.All;

        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        foreach (var target in targets)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-target.WindowDays);
            try
            {
                var removable = await ScalarAsync(connection, DownsampleSql.CountRemovable(target), cutoff, ct);
                long removed = 0;
                if (removable > 0)
                {
                    await using var tx = await connection.BeginTransactionAsync(ct);
                    removed = await ExecuteAsync(connection, tx, DownsampleSql.KeepLatestPerWeek(target), cutoff, ct);
                    await tx.CommitAsync(ct);
                }

                results.Add(new TableResult(target.QualifiedName, removable, removed));
                if (removed > 0)
                    metrics.RecordRetentionRowsRemoved(target.QualifiedName, removed);

                logger.LogInformation(
                    "Downsample {Table}: collapsed {Removed} rows older than {Cutoff:yyyy-MM-dd}",
                    target.QualifiedName, removed, cutoff.UtcDateTime);
            }
            catch (Exception ex)
            {
                failures++;
                results.Add(new TableResult(target.QualifiedName, -1, 0));
                logger.LogError(ex, "Downsample failed for {Table}", target.QualifiedName);
            }
        }

        run.CompletedAt = DateTimeOffset.UtcNow;
        run.TableResults = JsonSerializer.Serialize(results);
        run.Outcome = failures == 0
            ? RetentionOutcome.Success
            : failures == targets.Count ? RetentionOutcome.Failed : RetentionOutcome.PartialSuccess;

        db.RetentionRuns.Add(run);
        await db.SaveChangesAsync(ct);
        metrics.SetLastRetentionRun(RetentionRunType.Downsample.ToString(), run.CompletedAt.Value);
        return run;
    }

    private static async Task<long> ScalarAsync(
        NpgsqlConnection conn, string sql, DateTimeOffset cutoff, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("cutoff", cutoff);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is long l ? l : 0;
    }

    private static async Task<long> ExecuteAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx, string sql, DateTimeOffset cutoff, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("cutoff", cutoff);
        return await cmd.ExecuteNonQueryAsync(ct);
    }
}
