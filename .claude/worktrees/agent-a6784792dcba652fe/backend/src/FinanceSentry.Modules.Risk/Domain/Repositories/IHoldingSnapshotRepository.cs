namespace FinanceSentry.Modules.Risk.Domain.Repositories;

public interface IHoldingSnapshotRepository
{
    Task AddRangeAsync(IReadOnlyList<HoldingSnapshot> snapshots, CancellationToken ct = default);

    Task<IReadOnlyList<HoldingSnapshot>> ListSinceAsync(Guid userId, DateTimeOffset since, CancellationToken ct = default);

    Task<IReadOnlyList<HoldingSnapshot>> ListForSymbolAsync(
        Guid userId, string symbol, string sleeve, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetUserIdsAsync(CancellationToken ct = default);
}
