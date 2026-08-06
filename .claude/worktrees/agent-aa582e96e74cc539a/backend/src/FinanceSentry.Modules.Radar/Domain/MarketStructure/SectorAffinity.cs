namespace FinanceSentry.Modules.Radar.Domain.MarketStructure;

/// <summary>
/// Assigns a ticker to the sector ETF whose daily returns it correlates with most over a window.
/// A statistical proxy computed from persisted bars — not a hand-maintained membership table.
/// Below <see cref="MinCorrelation"/> no sector is assigned (conglomerates, crypto proxies, thin
/// history) rather than forcing a bad match.
/// </summary>
public static class SectorAffinity
{
    public const decimal MinCorrelation = 0.4m;
    private const int MinOverlappingReturns = 40;

    /// <summary>
    /// Best-correlated sector for <paramref name="tickerCloses"/> (date-keyed adjusted closes)
    /// against each candidate sector's closes, or null when nothing clears the bar.
    /// </summary>
    public static string? BestSector(
        IReadOnlyDictionary<DateOnly, decimal> tickerCloses,
        IReadOnlyDictionary<string, IReadOnlyDictionary<DateOnly, decimal>> sectorCloses)
    {
        string? best = null;
        var bestCorrelation = MinCorrelation;

        foreach (var (sector, closes) in sectorCloses)
        {
            var correlation = ReturnCorrelation(tickerCloses, closes);
            if (correlation is not null && correlation.Value > bestCorrelation)
            {
                bestCorrelation = correlation.Value;
                best = sector;
            }
        }

        return best;
    }

    /// <summary>Pearson correlation of overlapping daily returns; null when overlap is too thin.</summary>
    public static decimal? ReturnCorrelation(
        IReadOnlyDictionary<DateOnly, decimal> a,
        IReadOnlyDictionary<DateOnly, decimal> b)
    {
        var returnsA = DailyReturns(a);
        var returnsB = DailyReturns(b);

        var dates = returnsA.Keys.Intersect(returnsB.Keys).OrderBy(d => d).ToList();
        if (dates.Count < MinOverlappingReturns)
        {
            return null;
        }

        var xs = dates.Select(d => (double)returnsA[d]).ToArray();
        var ys = dates.Select(d => (double)returnsB[d]).ToArray();

        var meanX = xs.Average();
        var meanY = ys.Average();
        double cov = 0, varX = 0, varY = 0;
        for (var i = 0; i < xs.Length; i++)
        {
            var dx = xs[i] - meanX;
            var dy = ys[i] - meanY;
            cov += dx * dy;
            varX += dx * dx;
            varY += dy * dy;
        }

        if (varX == 0 || varY == 0)
        {
            return null;
        }

        return (decimal)(cov / Math.Sqrt(varX * varY));
    }

    private static Dictionary<DateOnly, decimal> DailyReturns(IReadOnlyDictionary<DateOnly, decimal> closes)
    {
        var ordered = closes.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key).ToList();
        var returns = new Dictionary<DateOnly, decimal>(ordered.Count);
        for (var i = 1; i < ordered.Count; i++)
        {
            returns[ordered[i].Key] = ordered[i].Value / ordered[i - 1].Value - 1m;
        }

        return returns;
    }
}
