# Cross-Module Port Contract: `IMarketRegimeSource`

**Defined in**: `FinanceSentry.Core.Interfaces` (so it crosses module boundaries with no Radar dependency).
**Implemented in**: `FinanceSentry.Modules.Radar` (`MarketRegimeSource`, reads `IRegimeReadingRepository`), registered by `RadarModule`.
**Consumed by**: `FinanceSentry.Modules.Research` — `ScoreCandidateCommandHandler` injects the Core interface only.

## Interface

```csharp
public interface IMarketRegimeSource
{
    Task<MarketRegimeSnapshot?> GetLatestAsync(CancellationToken ct = default);
}

public sealed record MarketRegimeSnapshot(
    DateTimeOffset ComputedAt,
    bool VolatilityAvailable, string? VolatilityRegime, decimal? VixLevel, string? VixTrend,
    bool RatesAvailable, string? RatesRegime, decimal? Spread, bool RecessionWarning,
    string? GrowthValueTilt,
    DateTimeOffset? VolatilityLastChange, DateTimeOffset? RatesLastChange);
```

## Semantics

- Returns the newest persisted `regime_readings` row projected to strings (no Radar enums leak into Core).
- Returns `null` when no reading has ever been computed → callers treat as `no_regime_data`.
- `*LastChange` is the `ComputedAt` of the most recent reading where that axis' band differs from the reading before it (best-effort; may be null if history is too short).
- Pure read — no side effects, no fetch, no compute.

## Boundary rationale

Unlike the 039 IPS/Risk ports (which delegate to another module's query handler and thus need an API-layer adapter), this port's implementation reads the **owning module's own** data, so the concrete class lives in Radar and requires no `FinanceSentry.API/Integration` glue. Research depends only on the Core abstraction → Principle I (no module→module reference) holds.
