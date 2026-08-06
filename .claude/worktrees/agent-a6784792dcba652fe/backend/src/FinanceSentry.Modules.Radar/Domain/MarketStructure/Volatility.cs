namespace FinanceSentry.Modules.Radar.Domain.MarketStructure;

/// <summary>Pure volatility / unusual-move / volume math over an ordered (oldest→newest) series.</summary>
public static class Volatility
{
    public const int DefaultVolWindow = 63;
    public const int DefaultVolumeWindow = 20;

    /// <summary>Daily simple returns from an adjusted-close series (length = closes.Count - 1).</summary>
    public static IReadOnlyList<decimal> DailyReturns(IReadOnlyList<decimal> adjCloses)
    {
        if (adjCloses.Count < 2)
        {
            return [];
        }

        var returns = new List<decimal>(adjCloses.Count - 1);
        for (var i = 1; i < adjCloses.Count; i++)
        {
            var prev = adjCloses[i - 1];
            returns.Add(prev == 0m ? 0m : (adjCloses[i] - prev) / prev);
        }

        return returns;
    }

    /// <summary>Sample standard deviation of the last <paramref name="window"/> daily returns; null if too few.</summary>
    public static decimal? StdDev(IReadOnlyList<decimal> adjCloses, int window)
    {
        var returns = DailyReturns(adjCloses);
        if (window <= 1 || returns.Count < window)
        {
            return null;
        }

        var slice = returns.Skip(returns.Count - window).ToArray();
        var mean = slice.Average();
        decimal sumSq = 0m;
        foreach (var r in slice)
        {
            var d = r - mean;
            sumSq += d * d;
        }

        var variance = sumSq / (window - 1);
        return (decimal)Math.Sqrt((double)variance);
    }

    /// <summary>Today's move in units of σ = last daily return / σ; null if σ is null or zero.</summary>
    public static decimal? TodayZScore(IReadOnlyList<decimal> adjCloses, int window)
    {
        var sigma = StdDev(adjCloses, window);
        if (sigma is null || sigma.Value == 0m)
        {
            return null;
        }

        var returns = DailyReturns(adjCloses);
        if (returns.Count == 0)
        {
            return null;
        }

        return returns[^1] / sigma.Value;
    }

    /// <summary>
    /// Today's volume / average of the prior <paramref name="window"/> days' volume; null if too few
    /// bars or the trailing average is zero (zero-volume edge).
    /// </summary>
    public static decimal? VolumeRatio(IReadOnlyList<long> volumes, int window)
    {
        if (window <= 0 || volumes.Count < window + 1)
        {
            return null;
        }

        long sum = 0;
        for (var i = volumes.Count - 1 - window; i < volumes.Count - 1; i++)
        {
            sum += volumes[i];
        }

        if (sum == 0)
        {
            return null;
        }

        var avg = (decimal)sum / window;
        return volumes[^1] / avg;
    }
}
