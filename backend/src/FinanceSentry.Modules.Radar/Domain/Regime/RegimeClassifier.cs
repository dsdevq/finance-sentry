namespace FinanceSentry.Modules.Radar.Domain.Regime;

using FinanceSentry.Modules.Radar.Application.Services;

/// <summary>
/// Pure, deterministic market-regime classification (feature 021). No EF, no HTTP, no LLM — same
/// inputs always yield the same bands. Every threshold comes from <see cref="RegimeOptions"/>
/// (FR-009). The two axes are classified independently and never merged (FR-010).
/// </summary>
public static class RegimeClassifier
{
    /// <summary>Documented growth-vs-value tilt hint per rates band (context only — never an action).</summary>
    public const string TiltInverted = "quality/defensive (recession-warning)";
    public const string TiltFlat = "late-cycle, neutral";
    public const string TiltNormal = "mid-cycle, balanced";
    public const string TiltSteep = "early-cycle, cyclical/value-supportive";

    // ── Volatility axis ──────────────────────────────────────────────────────

    /// <summary>
    /// Assesses the volatility axis from VIX closes ordered oldest→newest. Returns null when there
    /// are no closes (axis unavailable — never fabricates a band).
    /// </summary>
    public static VolatilityAssessment? AssessVolatility(
        IReadOnlyList<decimal> closesOldestToNewest, RegimeOptions options)
    {
        if (closesOldestToNewest.Count == 0)
        {
            return null;
        }

        var level = closesOldestToNewest[^1];
        var sma = Sma(closesOldestToNewest, options.VixSmaWindow);
        var trend = ClassifyTrend(level, sma, options.VixTrendBand);
        return new VolatilityAssessment(ClassifyVolatilityBand(level, options), level, sma, trend);
    }

    public static VolatilityRegime ClassifyVolatilityBand(decimal vix, RegimeOptions options)
    {
        if (vix < options.VixCalmMax)
        {
            return VolatilityRegime.Calm;
        }

        if (vix < options.VixNormalMax)
        {
            return VolatilityRegime.Normal;
        }

        return vix < options.VixStressedMax ? VolatilityRegime.Stressed : VolatilityRegime.Panic;
    }

    /// <summary>Simple moving average of the last <paramref name="window"/> closes, or null if too few.</summary>
    public static decimal? Sma(IReadOnlyList<decimal> closesOldestToNewest, int window)
    {
        if (window <= 0 || closesOldestToNewest.Count < window)
        {
            return null;
        }

        decimal sum = 0m;
        for (var i = closesOldestToNewest.Count - window; i < closesOldestToNewest.Count; i++)
        {
            sum += closesOldestToNewest[i];
        }

        return sum / window;
    }

    public static RegimeTrend ClassifyTrend(decimal latest, decimal? sma, decimal band)
    {
        if (sma is null)
        {
            return RegimeTrend.Unknown;
        }

        var upper = sma.Value * (1m + band);
        var lower = sma.Value * (1m - band);
        if (latest > upper)
        {
            return RegimeTrend.Rising;
        }

        return latest < lower ? RegimeTrend.Falling : RegimeTrend.Flat;
    }

    // ── Rates axis ───────────────────────────────────────────────────────────

    /// <summary>Assesses the rates axis from the latest 10y and 2y yields (percentage points).</summary>
    public static RatesAssessment AssessRates(decimal dgs10, decimal dgs2, RegimeOptions options)
    {
        var spread = dgs10 - dgs2;
        var regime = ClassifyRatesBand(spread, options);
        var recession = spread < options.SpreadInvertedMax;
        return new RatesAssessment(regime, dgs10, dgs2, spread, recession, TiltFor(regime));
    }

    public static RatesRegime ClassifyRatesBand(decimal spread, RegimeOptions options)
    {
        if (spread < options.SpreadInvertedMax)
        {
            return RatesRegime.Inverted;
        }

        if (spread < options.SpreadFlatMax)
        {
            return RatesRegime.Flat;
        }

        return spread < options.SpreadNormalMax ? RatesRegime.Normal : RatesRegime.Steep;
    }

    public static string TiltFor(RatesRegime regime) => regime switch
    {
        RatesRegime.Inverted => TiltInverted,
        RatesRegime.Flat => TiltFlat,
        RatesRegime.Normal => TiltNormal,
        _ => TiltSteep,
    };
}

/// <summary>Volatility-axis assessment: band + raw drivers.</summary>
public sealed record VolatilityAssessment(VolatilityRegime Regime, decimal Level, decimal? Sma, RegimeTrend Trend);

/// <summary>Rates-axis assessment: band + raw drivers + recession flag + tilt hint.</summary>
public sealed record RatesAssessment(
    RatesRegime Regime, decimal Dgs10, decimal Dgs2, decimal Spread, bool RecessionWarning, string Tilt);
