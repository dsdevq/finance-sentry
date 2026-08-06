namespace FinanceSentry.Modules.Radar.Domain.MarketStructure;

/// <summary>
/// Pure return + relative-strength math over an ordered (oldest→newest) adjusted-close series.
/// A window returns null when fewer than <c>window + 1</c> bars exist (not evaluable, never zero).
/// </summary>
public static class ReturnMath
{
    /// <summary>Simple return over <paramref name="window"/> trading days: last / close[last-window] - 1.</summary>
    public static decimal? Return(IReadOnlyList<decimal> adjCloses, int window)
    {
        if (window <= 0 || adjCloses.Count < window + 1)
        {
            return null;
        }

        var last = adjCloses[^1];
        var past = adjCloses[^(window + 1)];
        if (past == 0m)
        {
            return null;
        }

        return (last - past) / past;
    }

    /// <summary>Relative strength = ticker return − benchmark return; null if either is not evaluable.</summary>
    public static decimal? RelativeStrength(decimal? tickerReturn, decimal? benchmarkReturn)
    {
        if (tickerReturn is null || benchmarkReturn is null)
        {
            return null;
        }

        return tickerReturn.Value - benchmarkReturn.Value;
    }
}
