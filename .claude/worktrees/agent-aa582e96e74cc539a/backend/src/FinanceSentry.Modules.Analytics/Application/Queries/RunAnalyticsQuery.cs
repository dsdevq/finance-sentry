namespace FinanceSentry.Modules.Analytics.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Analytics.API.Responses;
using FinanceSentry.Modules.Analytics.Application.Services;
using FinanceSentry.Modules.Analytics.Domain;
using FinanceSentry.Modules.Analytics.Domain.Repositories;

/// <summary>
/// Runs a caller-supplied read-only SELECT (feature 033, US1): guard → execute → audit. The guard
/// blocks non-SELECTs before the DB is touched; the executor enforces read-only + per-user + budget at
/// the DB. Every path — rejected, executed, or budget-capped — is recorded (US3, FR-008).
/// </summary>
public sealed record RunAnalyticsQuery(Guid UserId, string Sql) : IQuery<AnalyticsQueryResponse>;

public sealed class RunAnalyticsQueryHandler(
    ISqlGuard guard,
    IReadOnlyQueryExecutor executor,
    IQueryAuditRepository audit)
    : IQueryHandler<RunAnalyticsQuery, AnalyticsQueryResponse>
{
    private const int MaxAuditedSqlLength = 8000;

    public async Task<AnalyticsQueryResponse> Handle(RunAnalyticsQuery query, CancellationToken cancellationToken)
    {
        var sql = query.Sql ?? string.Empty;

        var verdict = guard.Validate(sql);
        if (!verdict.IsValid)
        {
            await audit.AppendAsync(
                new QueryAuditRecord
                {
                    UserId = query.UserId,
                    Sql = Truncate(sql),
                    Outcome = QueryOutcome.Rejected,
                    RejectReason = verdict.Reason,
                },
                cancellationToken);

            return AnalyticsQueryResponse.Rejected(sql, verdict.Reason!);
        }

        var execution = await executor.ExecuteAsync(query.UserId, sql, cancellationToken);

        await audit.AppendAsync(
            new QueryAuditRecord
            {
                UserId = query.UserId,
                Sql = Truncate(sql),
                Outcome = QueryOutcome.Executed,
                RejectReason = execution.TooLarge ? execution.Reason : null,
                RowCount = execution.TooLarge ? null : execution.Rows.Count,
                DurationMs = execution.DurationMs,
            },
            cancellationToken);

        return execution.TooLarge
            ? AnalyticsQueryResponse.TooLarge(sql, execution.Reason!)
            : AnalyticsQueryResponse.Success(sql, execution.Columns, execution.Rows, execution.Truncated);
    }

    private static string Truncate(string sql)
        => sql.Length <= MaxAuditedSqlLength ? sql : sql[..MaxAuditedSqlLength];
}
