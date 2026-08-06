namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class ValuationSnapshotRepository(ResearchDbContext db) : IValuationSnapshotRepository
{
    private const int MaxLimit = 500;

    public async Task AddAsync(ValuationSnapshot snapshot, CancellationToken ct = default)
    {
        db.ValuationSnapshots.Add(snapshot);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ValuationSnapshot>> GetRecentAsync(
        string ticker, int limit, CancellationToken ct = default)
    {
        var upper = ticker.Trim().ToUpperInvariant();
        var effective = Math.Clamp(limit, 1, MaxLimit);
        return await db.ValuationSnapshots.AsNoTracking()
            .Where(s => s.Ticker == upper)
            .OrderByDescending(s => s.CapturedAt)
            .Take(effective)
            .ToListAsync(ct);
    }
}
