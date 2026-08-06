namespace FinanceSentry.Modules.Radar.Domain.MarketStructure;

/// <summary>
/// Composes the pure metric functions into a per-ticker <see cref="TickerStructure"/>. All inputs
/// are the persisted bars (ordered oldest→newest) → identical bars in, identical numbers out.
/// </summary>
public static class MarketStructureCalculator
{
    private const int Ma20 = 20;
    private const int Ma50 = 50;
    private const int Ma200 = 200;

    /// <summary>
    /// Computes structure for one ticker against a benchmark adjusted-close series. Benchmark returns
    /// are supplied precomputed (so the caller computes them once for the whole run).
    /// </summary>
    public static TickerStructure Compute(
        string ticker,
        IReadOnlyList<DailyBar> bars,
        IReadOnlyDictionary<int, decimal?> benchmarkReturnByWindow,
        bool stale)
    {
        var adjCloses = bars.Select(b => b.AdjClose).ToArray();
        var volumes = bars.Select(b => b.Volume).ToArray();

        var returnByWindow = new Dictionary<int, decimal?>();
        var rsByWindow = new Dictionary<int, decimal?>();
        foreach (var window in StructureWindows.All)
        {
            var ret = ReturnMath.Return(adjCloses, window);
            returnByWindow[window] = ret;
            benchmarkReturnByWindow.TryGetValue(window, out var benchReturn);
            rsByWindow[window] = ReturnMath.RelativeStrength(ret, benchReturn);
        }

        var ma20 = MovingAverages.Sma(adjCloses, Ma20);
        var ma50 = MovingAverages.Sma(adjCloses, Ma50);
        var ma200 = MovingAverages.Sma(adjCloses, Ma200);
        var close = adjCloses.Length > 0 ? adjCloses[^1] : 0m;

        return new TickerStructure(
            ticker,
            returnByWindow,
            rsByWindow,
            ma20,
            ma50,
            ma200,
            adjCloses.Length > 0 ? MovingAverages.Extension(close, ma50) : null,
            Volatility.StdDev(adjCloses, Volatility.DefaultVolWindow),
            Volatility.TodayZScore(adjCloses, Volatility.DefaultVolWindow),
            Volatility.VolumeRatio(volumes, Volatility.DefaultVolumeWindow),
            stale);
    }

    /// <summary>Returns by window for a benchmark adjusted-close series (computed once per run).</summary>
    public static IReadOnlyDictionary<int, decimal?> BenchmarkReturns(IReadOnlyList<DailyBar> benchmarkBars)
    {
        var adjCloses = benchmarkBars.Select(b => b.AdjClose).ToArray();
        var result = new Dictionary<int, decimal?>();
        foreach (var window in StructureWindows.All)
        {
            result[window] = ReturnMath.Return(adjCloses, window);
        }

        return result;
    }
}
