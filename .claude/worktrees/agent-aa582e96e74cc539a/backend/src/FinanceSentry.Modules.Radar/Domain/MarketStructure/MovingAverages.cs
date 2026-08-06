namespace FinanceSentry.Modules.Radar.Domain.MarketStructure;

/// <summary>Pure simple-moving-average + extension math over an ordered (oldest→newest) series.</summary>
public static class MovingAverages
{
    /// <summary>Simple MA of the last <paramref name="period"/> values; null if fewer exist.</summary>
    public static decimal? Sma(IReadOnlyList<decimal> values, int period)
    {
        if (period <= 0 || values.Count < period)
        {
            return null;
        }

        decimal sum = 0m;
        for (var i = values.Count - period; i < values.Count; i++)
        {
            sum += values[i];
        }

        return sum / period;
    }

    /// <summary>Extension from the 50-day MA: (close − ma) / ma; null if ma is null or zero.</summary>
    public static decimal? Extension(decimal close, decimal? ma)
    {
        if (ma is null || ma.Value == 0m)
        {
            return null;
        }

        return (close - ma.Value) / ma.Value;
    }
}
