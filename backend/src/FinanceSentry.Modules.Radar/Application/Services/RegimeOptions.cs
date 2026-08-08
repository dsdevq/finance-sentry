namespace FinanceSentry.Modules.Radar.Application.Services;

/// <summary>
/// All market-regime thresholds and toggles bound from configuration (section <c>Regime</c>).
/// No magic numbers in the classifier or the compute command (FR-009). Defaults are the
/// evidence-based conventions documented in the spec/research (VIX ~20 long-run average;
/// 10y-2y inversion as the canonical recession indicator).
/// </summary>
public sealed class RegimeOptions
{
    public const string SectionName = "Regime";

    public FredOptions Fred { get; set; } = new();

    // ── Volatility axis (VIX via the existing Yahoo market-history source) ────
    /// <summary>Ticker fetched for the volatility axis.</summary>
    public string VixTicker { get; set; } = "^VIX";

    /// <summary>Calendar days of VIX history pulled (enough to yield the SMA window of trading closes).</summary>
    public int VixLookbackDays { get; set; } = 40;

    /// <summary>Trading closes in the VIX simple moving average used for the trend read.</summary>
    public int VixSmaWindow { get; set; } = 20;

    /// <summary>± fraction around the SMA within which the trend is Flat (avoids flip-flop on noise).</summary>
    public decimal VixTrendBand { get; set; } = 0.02m;

    /// <summary>VIX strictly below this ⇒ Calm.</summary>
    public decimal VixCalmMax { get; set; } = 15m;

    /// <summary>VIX strictly below this (and ≥ Calm bound) ⇒ Normal.</summary>
    public decimal VixNormalMax { get; set; } = 20m;

    /// <summary>VIX strictly below this ⇒ Stressed; at/above ⇒ Panic.</summary>
    public decimal VixStressedMax { get; set; } = 30m;

    // ── Rates axis (FRED 10y-2y spread) ──────────────────────────────────────
    /// <summary>Spread strictly below this ⇒ Inverted (and recession warning). Default 0.</summary>
    public decimal SpreadInvertedMax { get; set; } = 0m;

    /// <summary>Spread strictly below this (and ≥ inverted bound) ⇒ Flat (percentage points).</summary>
    public decimal SpreadFlatMax { get; set; } = 0.5m;

    /// <summary>Spread strictly below this ⇒ Normal; at/above ⇒ Steep (percentage points).</summary>
    public decimal SpreadNormalMax { get; set; } = 1.5m;

    // ── Scheduling ───────────────────────────────────────────────────────────
    /// <summary>Hour (UTC) the daily regime compute runs — after Radar's 23:00 structure compute.</summary>
    public int ComputeHourUtc { get; set; } = 23;

    public sealed class FredOptions
    {
        /// <summary>Master toggle for the rates source (config demotion without a code change).</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>From <c>FRED_API_KEY</c>; blank ⇒ the rates axis is silently unavailable (FR-005).</summary>
        public string ApiKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = "https://api.stlouisfed.org/fred/";

        /// <summary>Most-recent observations fetched per series (bounds a "." holiday tail).</summary>
        public int ObservationLimit { get; set; } = 8;

        /// <summary>FRED series id for the 10-year constant-maturity treasury yield.</summary>
        public string TenYearSeriesId { get; set; } = "DGS10";

        /// <summary>FRED series id for the 2-year constant-maturity treasury yield.</summary>
        public string TwoYearSeriesId { get; set; } = "DGS2";
    }
}
