namespace FinanceSentry.Modules.Research.Domain.Repositories;

using FinanceSentry.Modules.Research.Domain;

public interface IValuationSnapshotRepository
{
    Task AddAsync(ValuationSnapshot snapshot, CancellationToken ct = default);

    /// <summary>Recent snapshots for a ticker, newest first (self-built history accrual).</summary>
    Task<IReadOnlyList<ValuationSnapshot>> GetRecentAsync(string ticker, int limit, CancellationToken ct = default);
}
