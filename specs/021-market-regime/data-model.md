# Phase 1 Data Model: Market Regime Scanner

## Enums (Radar.Domain.Regime)

```
VolatilityRegime : Calm | Normal | Stressed | Panic
RatesRegime      : Steep | Normal | Flat | Inverted
RegimeTrend      : Rising | Falling | Flat | Unknown
```

All persisted as `string` (EF `.HasConversion<string>()`) for readability and schema stability.

## Entity: RegimeReading (table `radar.regime_readings`)

One row per successful compute run; the newest row is "current". Added via Radar migration **M002**.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` (PK) | `gen_random_uuid()` default |
| `ComputedAt` | `DateTimeOffset` | run timestamp (UTC); indexed desc for "latest" |
| `VolatilityAvailable` | `bool` | false when VIX fetch failed that day |
| `VolatilityRegime` | `string?` (enum) | null when unavailable |
| `VixLevel` | `decimal?` | `numeric(10,4)` — latest VIX close |
| `VixSma` | `decimal?` | `numeric(10,4)` — SMA over trend window |
| `VixTrend` | `string?` (enum) | Rising/Falling/Flat/Unknown |
| `RatesAvailable` | `bool` | false when FRED keyless/unreachable |
| `RatesRegime` | `string?` (enum) | null when unavailable |
| `Dgs10` | `decimal?` | `numeric(10,4)` — latest valid 10y yield (%) |
| `Dgs2` | `decimal?` | `numeric(10,4)` — latest valid 2y yield (%) |
| `Spread` | `decimal?` | `numeric(10,4)` — `Dgs10 − Dgs2` (pp) |
| `RecessionWarning` | `bool` | true iff rates available and spread < inversion threshold |
| `GrowthValueTilt` | `string?` | documented hint string from rates band |

**Indexes**: `idx_regime_readings_computed_at` on `ComputedAt DESC`.

**Validation / invariants**:
- At least one axis available on a persisted row (a run where both fail persists nothing and logs a warning).
- `Spread` present iff both `Dgs10` and `Dgs2` present.
- `RecessionWarning` only true when `RatesAvailable`.
- Enums never defaulted to a band when the axis is unavailable — the `*Available` flag + null is the honest representation.

**"Latest"/"prior" reads** (for change detection + the port + the query): repository returns the newest row (`LatestAsync`) and the newest row strictly before a given timestamp (`PriorAsync`) to compare bands for `regime_change`.

## Signal shapes (shared `radar_signals`, scanner `market_regime`)

New constants in `RadarConstants.cs`:
- `RadarScanners.MarketRegime = "market_regime"`
- `RadarSignalTypes.RegimeVolatility = "regime_volatility"` (info, daily)
- `RadarSignalTypes.RegimeRates = "regime_rates"` (info, daily)
- `RadarSignalTypes.RegimeChange = "regime_change"` (notable, on band cross)

| Signal | Severity | Subject / SubjectType | DedupKey | Payload |
|---|---|---|---|---|
| `regime_volatility` | Info | `volatility` / `Universe` | `market_regime:regime_volatility:volatility:<yyyy-MM-dd>` | `{ regime, vixLevel, vixSma, trend }` |
| `regime_rates` | Info | `rates` / `Universe` | `market_regime:regime_rates:rates:<yyyy-MM-dd>` | `{ regime, dgs10, dgs2, spread, recessionWarning, tilt }` |
| `regime_change` (volatility) | Notable | `volatility` / `Universe` | `market_regime:regime_change:volatility:<from>-<to>` | `{ axis:"volatility", from, to, vixLevel }` |
| `regime_change` (rates) | Notable | `rates` / `Universe` | `market_regime:regime_change:rates:<from>-<to>` | `{ axis:"rates", from, to, spread, recessionWarning }` |

`info` readings dedup within a day; `regime_change` deduped by the silence window (a same-day re-run to the same `<from>-<to>` is suppressed).

## Cross-module port DTO (Core.Interfaces)

```
IMarketRegimeSource
  Task<MarketRegimeSnapshot?> GetLatestAsync(CancellationToken ct = default)

MarketRegimeSnapshot(
  DateTimeOffset ComputedAt,
  bool VolatilityAvailable, string? VolatilityRegime, decimal? VixLevel, string? VixTrend,
  bool RatesAvailable, string? RatesRegime, decimal? Spread, bool RecessionWarning,
  string? GrowthValueTilt,
  DateTimeOffset? VolatilityLastChange, DateTimeOffset? RatesLastChange)
```

Returns `null` when no reading exists. Strings (not Radar enums) cross the boundary so Core carries no Radar types.

## Scorecard extension (Research.Domain.Scoring)

`CandidateScorecard` gains `RegimeContext? Regime`:

```
RegimeContext(
  string? VolatilityRegime, string? RatesRegime, bool RecessionWarning,
  decimal? VixLevel, decimal? Spread,
  int? RawStructureScore, int? AdjustedStructureScore, int AdjustmentPoints,
  IReadOnlyList<string> Rationale, DateTimeOffset? AsOf)
```

`StructureScore` on the scorecard remains the **raw** value; `RegimeContext.AdjustedStructureScore` carries the regime-modulated value. `AdjustmentPoints` is the signed delta (≤ 0). `Rationale` lists e.g. `["volatility:Panic", "crowding:Extended", "haircut:-15", "rates:Inverted", "haircut:-5"]` or `["no_regime_data"]`.

## Config (section `Regime` on `RegimeOptions`) — env `Regime__*`

| Key | Default | Meaning |
|---|---|---|
| `Fred:Enabled` | `true` | rates source master toggle |
| `Fred:ApiKey` | `""` (`FRED_API_KEY`) | blank ⇒ rates axis silent |
| `Fred:BaseUrl` | `https://api.stlouisfed.org/fred/` | |
| `Fred:ObservationLimit` | `8` | most-recent points fetched per series |
| `VixTicker` | `^VIX` | |
| `VixLookbackDays` | `40` | calendar days pulled for the SMA |
| `VixSmaWindow` | `20` | trading closes in the SMA |
| `VixTrendBand` | `0.02` | ± fraction around SMA for Rising/Falling |
| `VixCalmMax` | `15` | < ⇒ Calm |
| `VixNormalMax` | `20` | < ⇒ Normal |
| `VixStressedMax` | `30` | < ⇒ Stressed; ≥ ⇒ Panic |
| `SpreadInvertedMax` | `0` | < ⇒ Inverted (and recession warning) |
| `SpreadFlatMax` | `0.5` | < ⇒ Flat |
| `SpreadNormalMax` | `1.5` | < ⇒ Normal; ≥ ⇒ Steep |
| `ComputeHourUtc` | `23` | daily job hour (after Radar compute) |

`OpportunityOptions` gains: `RegimePanicExtendedHaircut=15`, `RegimeStressedExtendedHaircut=8`, `RegimeInvertedExtendedHaircut=5` (Neutral crowding = half each, Early = 0).
