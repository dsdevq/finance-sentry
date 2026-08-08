# Phase 0 Research: Market Regime Scanner

## R1 — Volatility (VIX) band thresholds

**Decision**: Default bands `Calm` (VIX < 15), `Normal` (15 ≤ VIX < 20), `Stressed` (20 ≤ VIX < 30), `Panic` (VIX ≥ 30). Trend by comparing the latest close to a 20-day SMA of VIX closes: `Rising` if last > SMA × (1 + band), `Falling` if last < SMA × (1 − band), else `Flat`; `Unknown` when fewer than the SMA window of bars exist.

**Rationale**: The VIX long-run average sits near ~19–20, so ~20 is the natural "normal" pivot. Sub-15 reflects low-volatility complacency (typical of calm bull markets); 20–30 is elevated stress (corrections, growth scares); 30+ is crisis territory — the index spiked well past 30 (often 40–80) in 2008 and March 2020 and in every major drawdown. These are widely-published, auditable conventions, not invented numbers. A trend band (default 2%) avoids flip-flopping `Rising`/`Falling` on noise around the SMA.

**Alternatives considered**: VIX term-structure (VIX vs VIX3M contango/backwardation) — richer but needs a second series and is more fragile to parse; deferred. Percentile-of-trailing-1y VIX — adaptive but non-deterministic across history and harder to explain than fixed evidence-based bands.

## R2 — Rates (yield-curve) band thresholds

**Decision**: 10y-2y spread (`DGS10 − DGS2`, percentage points). Bands `Inverted` (spread < 0), `Flat` (0 ≤ spread < 0.5), `Normal` (0.5 ≤ spread < 1.5), `Steep` (spread ≥ 1.5). `recessionWarning = spread < 0`. Growth-vs-value tilt hint: `Inverted` → "quality/defensive (recession-warning)"; `Flat` → "late-cycle, neutral"; `Normal` → "mid-cycle, balanced"; `Steep` → "early-cycle, cyclical/value-supportive".

**Rationale**: The 10y-2y spread is the canonical recession indicator — it has inverted ahead of every U.S. recession since the 1950s, typically 6–18 months before onset. A positive-but-flat curve (< ~50 bps) is characteristically late-cycle; a steep curve (> ~150 bps) is early-cycle/expansionary and historically coincides with cyclical/value leadership (steepening helps bank net interest margins). The tilt is a *documented hint*, never an instruction.

**Alternatives considered**: 10y-3m spread (the Fed's preferred model input) — equally defensible; 10y-2y chosen because it's the most widely-quoted and both legs come from one FRED series family with identical formatting. 3-month may be added later behind config.

## R3 — FRED external API contract & keyless-silent pattern

**Decision**: New `FredYieldCurveSource : IYieldCurveSource` using a named `IHttpClientFactory` client (`regime-fred`). Endpoint `GET https://api.stlouisfed.org/fred/series/observations?series_id=DGS10&api_key=<key>&file_type=json&sort_order=desc&limit=<N>`. Parse `observations[]`, each `{date, value}`; **skip `value == "."`** (FRED's documented "no observation" placeholder on holidays/weekends), take the latest valid observation per series, pair DGS10 with DGS2 by "latest valid each". Keyed from `FRED_API_KEY` → `Regime__Fred__ApiKey` config binding; `IsConfigured => Enabled && key non-blank`; blank key ⇒ no fetch, no error, rates axis reports unavailable (exact mirror of `FinnhubRecommendationTrendsService`).

**Rationale**: Matches the shipped Finnhub free-API keyless-silent precedent (feature 037): documented REST+JSON, config-bound key in `.env.sops`, silent when keyless. FRED requires the key as a query param (not a header) — that's the only shape difference from Finnhub. `sort_order=desc&limit` bounds the payload to the few most-recent points so a `.`-heavy tail (long weekend) still yields a valid pair.

**Alternatives considered**: Treasury.gov / other yield feeds — less stable, no free structured JSON with a stable contract. Storing yields as `daily_bars` rows — unnecessary; only the computed spread + latest raw legs need persisting in the reading.

**Contract sample**: see `contracts/fred-series-observations.md`.

## R4 — VIX fetch via the existing market-history source

**Decision**: Reuse `IMarketHistorySource.GetDailyBarsAsync("^VIX", since)` (Yahoo-backed, already pinned with a browser User-Agent). Read the last ~40 calendar days to guarantee ≥20 trading closes for the SMA. Do **not** persist VIX into `daily_bars` (it is not a universe member and ingestion/breadth must not accidentally include it) — the closes are consumed transiently and only the computed reading is stored.

**Rationale**: No second vendor, no new contract; the source already returns empty-on-failure so a Yahoo outage degrades to "volatility axis unavailable today" cleanly. Yahoo returns `^VIX` on the same chart endpoint.

**Alternatives considered**: FRED `VIXCLS` series (VIX is also on FRED) — viable and would unify both axes on FRED, but couples the volatility axis to the FRED key (which is blank by default in dev), defeating the "VIX works out-of-the-box via Yahoo" goal. Keep VIX on Yahoo, rates on FRED.

## R5 — Module placement (Radar vs new module)

**Decision**: Implement inside the **Radar module** (schema `radar`). Add `regime_readings` table via Radar migration **M002**. Reuse `RadarDbContext`, `IRadarSignalWriter`, the shared `radar_signals` log, `IMarketHistorySource`, and RadarModule's Hangfire/HttpClient/config wiring.

**Rationale**: The roadmap explicitly says "Later scanners (021 regime, portfolio, events/news) plug into the same signal log as small independent features — no new architecture needed." Two scalar daily series do not justify a whole new DbContext + migration-history table + signal plumbing. Keeping it in Radar reuses the calibrate-before-alert, retention, and dedup machinery already built.

**Alternatives considered**: A standalone `Modules.MarketRegime` with its own DbContext — cleaner isolation on paper, but duplicates the signal-log integration and doubles migration-history bookkeeping for a two-number feature. Rejected as over-engineering.

## R6 — Cross-module coupling into 019 (Core port)

**Decision**: Define `IMarketRegimeSource` + `MarketRegimeSnapshot` in `FinanceSentry.Core.Interfaces`. Implement `MarketRegimeSource` **in Radar** (reads `IRegimeReadingRepository`), registered by `RadarModule`. The Research `ScoreCandidateCommandHandler` injects `IMarketRegimeSource` (Core type) — it never references the Radar assembly. This is the same decoupling shape as `IRadarSignalWriter` (Core-defined, Radar-implemented, Research-consumed).

**Rationale**: Preserves Principle I (no module→module reference). Unlike the 039 IPS/Risk ports (which delegate to *another module's* query handler and therefore need an API-layer adapter), the regime port's impl reads Radar's **own** data, so the concrete class lives in Radar and needs no `FinanceSentry.API/Integration` glue. Simpler and still boundary-clean.

**Alternatives considered**: An API-layer adapter delegating to `GetMarketRegimeQuery` (039 style) — unnecessary indirection here because the data owner *is* the implementer. Publishing regime onto `radar_signals` and having Research read the log — works but forces Research to reconstruct classification from signal payloads; a typed port is cleaner and testable.

## R7 — 019 scoring adjustment design (context, never action)

**Decision**: Add a pure `RegimeScoreAdjuster.Adjust(rawStructureScore, crowding, snapshot, options) → (adjustedScore, points, rationale[])`. Attach a `RegimeContext` record to `CandidateScorecard` (volatility band, rates band, recession flag, raw drivers, `RawStructureScore`, `AdjustedStructureScore`, `AdjustmentPoints`, `Rationale`, `AsOf`). **The persisted `CandidateScore.StructureScore` stays the raw value** (governed by `FormulaVersion`); the regime adjustment is a returned, time-varying context — never overwrites the canonical score.

Adjustment rules (all magnitudes are named config constants on `OpportunityOptions`, defaults below):
- **Volatility haircut** (subtractive, only in risk-off bands):
  - `Panic`: Extended crowding → −`RegimePanicExtendedHaircut` (default 15); Neutral → −half (7); Early → 0.
  - `Stressed`: Extended → −`RegimeStressedExtendedHaircut` (default 8); Neutral → −half (4); Early → 0.
  - `Calm`/`Normal`: 0.
- **Inversion haircut** (additional, when rates band `Inverted`): Extended → −`RegimeInvertedExtendedHaircut` (default 5); Neutral → −half (2); Early → 0.
- Result clamped to `[0,100]`. If `rawStructureScore is null` OR snapshot unavailable OR both axes unavailable → adjusted = raw, rationale `no_regime_data`, points 0.

**Rationale**: Speculativeness proxied by `CrowdingClass` (Extended = over-extended/high-volume = the froth you least want to chase in a crisis; Early = the opposite). Haircuts are small and bounded so regime *reranks* without erasing a genuinely strong structural read — respecting the roadmap's "no false precision / stay-invested" guardrails. Fully explainable (raw + adjusted + rationale both surfaced). Reversible (config → 0 disables). Never touches cash/sell/promote (FR-021).

**Alternatives considered**: Multiplicative scaling — harder to reason about and can zero-out strong names; rejected for the transparent additive haircut. A separate composite "regime score" — violates the roadmap's "no composite single number" stance; rejected. Boosting value/quality candidates in inversion — we have no value sub-score in 019 v1, so we express the tilt as a haircut on speculative names + a documented hint, not a fabricated value boost.

## R8 — Migration safety (known past failure)

**Decision**: Generate M002 with the EF CLI (`dotnet ef migrations add M002_RegimeReadings`) inside the sdk:10.0 container against the `RadarDbContextFactory`, producing the `.cs` + `.Designer.cs` (with `[DbContext]`/`[Migration]` attributes) + an updated `RadarDbContextModelSnapshot.cs`. Verify all three artifacts exist and the snapshot includes `regime_readings` before committing. If the CLI tool is unavailable in the container, hand-author all three with the attributes present and validate by a real `database update` against a throwaway Postgres.

**Rationale**: The M007 incident (a hand-written migration lacking the Designer + attributes was silently never applied) is the explicit failure this feature must not repeat. EF-generated artifacts carry the attributes by construction; a snapshot diff is the proof the model change is captured.
