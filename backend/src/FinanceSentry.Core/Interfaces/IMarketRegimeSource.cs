namespace FinanceSentry.Core.Interfaces;

/// <summary>
/// Read-only cross-module access to the latest market-regime read (feature 021). Defined in Core so
/// consumers (the 019 opportunity scorer) never reference the Radar module directly; implemented
/// once in Modules.Radar over its persisted readings. Regime is <b>context only</b> — a consumer may
/// modulate ranking/framing with it, but it MUST NOT drive any buy/sell/cash action.
/// </summary>
public interface IMarketRegimeSource
{
    /// <summary>The newest regime reading, or null when none has ever been computed.</summary>
    Task<MarketRegimeSnapshot?> GetLatestAsync(CancellationToken ct = default);
}

/// <summary>
/// Cross-boundary projection of the latest regime — both orthogonal axes as strings (never merged),
/// raw drivers, the recession flag, the growth-vs-value tilt hint, and per-axis last-change dates.
/// Carries no Radar-internal types so Core stays dependency-free.
/// </summary>
public sealed record MarketRegimeSnapshot(
    DateTimeOffset ComputedAt,
    bool VolatilityAvailable,
    string? VolatilityRegime,
    decimal? VixLevel,
    string? VixTrend,
    bool RatesAvailable,
    string? RatesRegime,
    decimal? Spread,
    bool RecessionWarning,
    string? GrowthValueTilt,
    DateTimeOffset? VolatilityLastChange,
    DateTimeOffset? RatesLastChange);
