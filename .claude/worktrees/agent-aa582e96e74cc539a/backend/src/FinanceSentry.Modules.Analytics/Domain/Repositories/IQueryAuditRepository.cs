namespace FinanceSentry.Modules.Analytics.Domain.Repositories;

using FinanceSentry.Modules.Analytics.Domain;

/// <summary>Append-only audit sink for analytics queries (feature 033, FR-008).</summary>
public interface IQueryAuditRepository
{
    Task AppendAsync(QueryAuditRecord record, CancellationToken ct = default);
}
