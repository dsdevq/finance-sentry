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

        // Tracked read: Yahoo retroactively rescales AdjClose after splits/dividends, so
        // existing rows must be updated in place — insert-only storage leaves a scale
        // discontinuity that corrupts returns and z-scores.
        var existing = await db.DailyBars
            .Where(b => tickers.Contains(b.Ticker) && b.Date >= minDate)
            .ToDictionaryAsync(b => (b.Ticker, b.Date), ct);

        var incoming = bars
            .GroupBy(b => (b.Ticker, b.Date))
            .Select(g => g.First())
            .ToList();

        var added = 0;
        foreach (var bar in incoming)
        {
            if (existing.TryGetValue((bar.Ticker, bar.Date), out var row))
            {
                row.Open = bar.Open;
                row.High = bar.High;
                row.Low = bar.Low;
                row.Close = bar.Close;
                row.AdjClose = bar.AdjClose;
                row.Volume = bar.Volume;
            }
            else
            {
                db.DailyBars.Add(bar);
                added++;
            }
        }

        await db.SaveChangesAsync(ct);
        return added;
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
