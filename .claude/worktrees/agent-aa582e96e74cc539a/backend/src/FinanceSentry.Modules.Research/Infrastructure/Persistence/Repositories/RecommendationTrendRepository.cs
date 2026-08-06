namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class RecommendationTrendRepository(ResearchDbContext db) : IRecommendationTrendRepository
{
    private const int MaxMonths = 60;

    public async Task<int> UpsertAsync(
        IReadOnlyList<RecommendationTrend> trends, CancellationToken ct = default)
    {
        if (trends.Count == 0)
        {
            return 0;
        }

        var tickers = trends.Select(t => t.Ticker).Distinct().ToArray();
        var periods = trends.Select(t => t.Period).Distinct().ToArray();
        var existing = await db.RecommendationTrends
            .Where(t => tickers.Contains(t.Ticker) && periods.Contains(t.Period))
            .ToDictionaryAsync(t => (t.Ticker, t.Period), ct);

        var inserted = 0;
        foreach (var trend in trends)
        {
            if (existing.TryGetValue((trend.Ticker, trend.Period), out var row))
            {
                row.StrongBuy = trend.StrongBuy;
                row.Buy = trend.Buy;
                row.Hold = trend.Hold;
                row.Sell = trend.Sell;
                row.StrongSell = trend.StrongSell;
                row.Source = trend.Source;
                row.IngestedAt = trend.IngestedAt;
            }
            else
            {
                db.RecommendationTrends.Add(trend);
                existing[(trend.Ticker, trend.Period)] = trend;
                inserted++;
            }
        }

        await db.SaveChangesAsync(ct);
        return inserted;
    }

    public async Task<IReadOnlyList<RecommendationTrend>> GetLatestAsync(
        string ticker, int months, CancellationToken ct = default)
    {
        var upper = ticker.Trim().ToUpperInvariant();
        var effective = Math.Clamp(months, 1, MaxMonths);
        return await db.RecommendationTrends.AsNoTracking()
            .Where(t => t.Ticker == upper)
            .OrderByDescending(t => t.Period)
            .Take(effective)
            .ToListAsync(ct);
    }
}
