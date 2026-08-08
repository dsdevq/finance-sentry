namespace FinanceSentry.Modules.Radar.Domain.Regime;

/// <summary>
/// One persisted market-regime compute (feature 021), row in <c>radar.regime_readings</c>. The
/// newest row is "current". Each axis carries an availability flag so an unavailable axis is
/// represented by <c>false + null</c> rather than a fabricated band (FR-011/FR-017). The two axes
/// are stored independently and never merged into a single label (FR-010).
/// </summary>
public sealed class RegimeReading
{
    public Guid Id { get; init; }

    public DateTimeOffset ComputedAt { get; set; } = DateTimeOffset.UtcNow;

    // ── Volatility axis ──────────────────────────────────────────────────────
    public bool VolatilityAvailable { get; set; }

    public VolatilityRegime? VolatilityRegime { get; set; }

    public decimal? VixLevel { get; set; }

    public decimal? VixSma { get; set; }

    public RegimeTrend? VixTrend { get; set; }

    // ── Rates axis ───────────────────────────────────────────────────────────
    public bool RatesAvailable { get; set; }

    public RatesRegime? RatesRegime { get; set; }

    public decimal? Dgs10 { get; set; }

    public decimal? Dgs2 { get; set; }

    public decimal? Spread { get; set; }

    public bool RecessionWarning { get; set; }

    public string? GrowthValueTilt { get; set; }
}
