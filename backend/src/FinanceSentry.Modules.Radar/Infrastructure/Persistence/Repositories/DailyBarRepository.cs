namespace FinanceSentry.Modules.Radar.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public sealed class DailyBarRepository(RadarDbContext db) : IDailyBarRepository
{
    public async Task<int> UpsertRangeAsync(IReadOnlyCollection<DailyBar> bars, CancellationToken ct = default)
    {
        if (bars.Count == 0)
        {
            return 0;
        }

        var tickers = bars.Select(b => b.Ticker).Distinct().ToArray();
        var minDate = bars.Min(b => b.Date);

        var existing = await db.DailyBars.AsNoTracking()
            .Where(b => tickers.Contains(b.Ticker) && b.Date >= minDate)
            .Select(b => new { b.Ticker, b.Date })
            .ToListAsync(ct);

        var existingSet = existing.Select(e => (e.Ticker, e.Date)).ToHashSet();

        var toAdd = bars
            .Where(b => !existingSet.Contains((b.Ticker, b.Date)))
            .GroupBy(b => (b.Ticker, b.Date))
            .Select(g => g.First())
            .ToList();

        if (toAdd.Count == 0)
        {
            return 0;
        }

        db.DailyBars.AddRange(toAdd);
        await db.SaveChangesAsync(ct);
        return toAdd.Count;
    }

    public async Task<IReadOnlyList<DailyBar>> GetSinceAsync(
        string ticker, DateOnly since, CancellationToken ct = default)
        => await db.DailyBars.AsNoTracking()
            .Where(b => b.Ticker == ticker && b.Date >= since)
            .OrderBy(b => b.Date)
            .ToListAsync(ct);

    public async Task<DateOnly?> GetLatestDateAsync(string ticker, CancellationToken ct = default)
    {
        var dates = await db.DailyBars.AsNoTracking()
            .Where(b => b.Ticker == ticker)
            .OrderByDescending(b => b.Date)
            .Select(b => b.Date)
            .Take(1)
            .ToListAsync(ct);

        return dates.Count > 0 ? dates[0] : null;
    }

    public async Task<IReadOnlyDictionary<string, DateOnly>> GetLatestDatesAsync(
        IReadOnlyCollection<string> tickers, CancellationToken ct = default)
    {
        if (tickers.Count == 0)
        {
            return new Dictionary<string, DateOnly>();
        }

        var rows = await db.DailyBars.AsNoTracking()
            .Where(b => tickers.Contains(b.Ticker))
            .GroupBy(b => b.Ticker)
            .Select(g => new { Ticker = g.Key, Latest = g.Max(b => b.Date) })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.Ticker, r => r.Latest);
    }
}
