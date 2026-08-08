namespace FinanceSentry.Modules.Radar.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Radar.Domain.Regime;
using FinanceSentry.Modules.Radar.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public sealed class RegimeReadingRepository(RadarDbContext db) : IRegimeReadingRepository
{
    public async Task AppendAsync(RegimeReading reading, CancellationToken ct = default)
    {
        db.RegimeReadings.Add(reading);
        await db.SaveChangesAsync(ct);
    }

    public Task<RegimeReading?> LatestAsync(CancellationToken ct = default)
        => db.RegimeReadings.AsNoTracking()
            .OrderByDescending(r => r.ComputedAt)
            .FirstOrDefaultAsync(ct);

    public Task<RegimeReading?> PriorAsync(DateTimeOffset before, CancellationToken ct = default)
        => db.RegimeReadings.AsNoTracking()
            .Where(r => r.ComputedAt < before)
            .OrderByDescending(r => r.ComputedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<RegimeReading>> RecentAsync(int limit, CancellationToken ct = default)
        => await db.RegimeReadings.AsNoTracking()
            .OrderByDescending(r => r.ComputedAt)
            .Take(limit)
            .ToListAsync(ct);
}
