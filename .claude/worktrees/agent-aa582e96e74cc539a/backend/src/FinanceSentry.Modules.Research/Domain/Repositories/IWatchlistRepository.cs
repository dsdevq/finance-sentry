namespace FinanceSentry.Modules.Research.Domain.Repositories;

public interface IWatchlistRepository
{
    Task<IReadOnlyList<WatchlistItem>> ListAsync(Guid userId, CancellationToken ct = default);

    Task<WatchlistItem?> FindAsync(Guid userId, string ticker, CancellationToken ct = default);

    Task AddAsync(WatchlistItem item, CancellationToken ct = default);

    Task<bool> RemoveAsync(Guid userId, Guid itemId, CancellationToken ct = default);
}
