namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class WatchlistRepository(ResearchDbContext db) : IWatchlistRepository
{
    public async Task<IReadOnlyList<WatchlistItem>> ListAsync(Guid userId, CancellationToken ct = default)
        => await db.WatchlistItems.AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.Ticker)
            .ToListAsync(ct);

    public Task<WatchlistItem?> FindAsync(Guid userId, string ticker, CancellationToken ct = default)
        => db.WatchlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Ticker == ticker, ct);

    public async Task AddAsync(WatchlistItem item, CancellationToken ct = default)
    {
        db.WatchlistItems.Add(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid itemId, CancellationToken ct = default)
    {
        var affected = await db.WatchlistItems
            .Where(w => w.UserId == userId && w.Id == itemId)
            .ExecuteDeleteAsync(ct);
        return affected > 0;
    }
}
