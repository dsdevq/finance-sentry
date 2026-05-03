namespace FinanceSentry.Modules.Wealth.Domain.Repositories;

public interface INetWorthSnapshotRepository
{
    Task PersistAsync(NetWorthSnapshot snapshot, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, DateOnly snapshotDate, CancellationToken ct = default);
    Task<IReadOnlyList<NetWorthSnapshot>> GetByUserIdAsync(Guid userId, DateOnly? from, DateOnly? to, CancellationToken ct = default);
}
