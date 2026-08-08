namespace FinanceSentry.Modules.Radar.Domain.Regime;

/// <summary>
/// Equity-volatility regime band derived from the VIX level (feature 021). Orthogonal to
/// <see cref="RatesRegime"/> — the two axes are never collapsed into one label.
/// </summary>
public enum VolatilityRegime
{
    Calm,
    Normal,
    Stressed,
    Panic,
}

/// <summary>Rates/yield-curve regime band derived from the 10y-2y spread.</summary>
public enum RatesRegime
{
    Steep,
    Normal,
    Flat,
    Inverted,
}

/// <summary>Direction of the VIX relative to its own moving average. <c>Unknown</c> when history is too short.</summary>
public enum RegimeTrend
{
    Rising,
    Falling,
    Flat,
    Unknown,
}
