namespace FinanceSentry.Modules.Radar.Infrastructure.MarketData;

/// <summary>
/// Swappable source for the treasury yield curve (FRED in v1). Keyless-silent: when unconfigured
/// (<see cref="IsConfigured"/> false) it issues no request and callers skip the rates axis quietly
/// (FR-005). Returns null when configured but the data is unavailable that run.
/// </summary>
public interface IYieldCurveSource
{
    bool IsConfigured { get; }

    Task<YieldCurveReading?> GetLatestAsync(CancellationToken ct = default);
}

/// <summary>Latest 10y + 2y constant-maturity yields (percentage points) and the observation date.</summary>
public sealed record YieldCurveReading(decimal Dgs10, decimal Dgs2, DateOnly AsOf);
