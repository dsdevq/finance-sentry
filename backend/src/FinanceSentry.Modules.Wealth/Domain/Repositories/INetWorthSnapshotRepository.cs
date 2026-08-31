namespace FinanceSentry.Modules.Wealth.Domain.Repositories;

public interface INetWorthSnapshotRepository
{
    Task PersistAsync(NetWorthSnapshot snapshot, CancellationToken ct = default);

    /// <summary>
    /// Inserts the snapshot, replacing any existing row for the same (user, date) — a
    /// day's snapshot is refreshed throughout the day rather than frozen at first write.
    /// </summary>
    Task UpsertAsync(NetWorthSnapshot snapshot, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid userId, DateOnly snapshotDate, CancellationToken ct = default);
    Task<NetWorthSnapshot?> GetLatestByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Latest snapshot strictly before <paramref name="date"/> — the carry-forward baseline.</summary>
    Task<NetWorthSnapshot?> GetLatestBeforeAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<NetWorthSnapshot>> GetByUserIdAsync(Guid userId, DateOnly? from, DateOnly? to, CancellationToken ct = default);
}
