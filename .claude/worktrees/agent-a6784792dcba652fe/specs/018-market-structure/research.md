# Phase 0 Research: Market Structure Scanner + Radar Signal Log

Grounded in the existing codebase (2026-07-08) and the spec's `[DECISION]` notes.

## R1 — Module boundary: new `Modules.Radar`, not a Research extension

- **Decision**: Stand up a new `FinanceSentry.Modules.Radar` with its own `RadarDbContext`
  (schema `radar`, history table `__ef_migrations_history_radar`, `M001_InitialSchema`).
- **Rationale**: (a) Persisting daily bars is the explicit inverse of 017's "no new time-series"
  rule and is a distinct concern; (b) `radar_signals` is a shared platform table written by many
  scanners — it must not be owned by a single feature module. Module wiring is reflection-discovered
  (`IModuleRegistrar`/`IJobRegistrar`), so a new module adds itself with only: a `.csproj` reference
  from API + Mcp, one `MigrateContext<RadarDbContext>` line in `MigrationExtensions.cs`, and the MCP
  allowlist bump.
- **Alternatives**: Extend Research — rejected (couples every future scanner to Research; violates
  the scanner-agnostic-log decision).

## R2 — Cross-module signal writing: `IRadarSignalWriter` in Core

- **Decision**: Define `IRadarSignalWriter.AppendSignalAsync(RadarSignalRequest, ct)` in
  `FinanceSentry.Core/Interfaces/`, implement `RadarSignalWriter` in `Modules.Radar`. 017/019 (in
  Research) inject the Core interface to append signals with **no dependency on the Radar module**.
- **Rationale**: Exactly the proven `IAlertGeneratorService` pattern (Core interface, single-module
  impl, cross-module injection). Keeps Principle I intact and lets 017/019 emit into the shared log.
- **Alternatives**: Research owning the signal table — rejected (R1). A message bus — over-engineered
  for an in-process monolith.

## R3 — History source behind `IMarketHistorySource`

- **Decision**: New Core `IMarketHistorySource.GetDailyBarsAsync(ticker, since, ct) →
  IReadOnlyList<DailyBarData>` implemented by `YahooMarketHistorySource` over the existing
  `yahoo-finance` HttpClient (`/v8/finance/chart/{ticker}?interval=1d&range=…`). The chart response's
  `indicators.quote[0]` already carries `open/high/low/close/volume` and `indicators.adjclose[0].adjclose`
  — the current `YahooMarketDataService` reads only `close`, so 018 reads the full OHLCV+adjclose from
  the **same** endpoint, no new HTTP surface.
- **Rationale**: FR-003 (extend existing source, no paid API) + the spec's SPOF concern — a thin
  interface lets a Stooq fallback drop in without touching ingestion/consumers.
- **Alternatives**: Reuse `IMarketDataService.GetDailyClosesAsync` — insufficient (close only; need
  OHLCV+adjclose + volume for z-score/breadth/extension).

## R4 — Universe resolution & decoupling

- **Decision**: `RadarUniverseService` composes: seed members (SPY + 11 SPDR sectors:
  XLB XLC XLE XLF XLI XLK XLP XLRE XLU XLV XLY + industry seed SMH) ∪ equity holdings
  (`IBrokerageHoldingsReader`, filter `InstrumentType == "STK"`) ∪ watchlist (`IWatchlistReader`).
  Membership persisted in `radar_universe_members` (`Kind`, `Source=Seed|Auto`, `Active`), auto-synced
  each ingestion run; de-activated (not deleted) when a ticker leaves holdings/watchlist so its history
  is retained.
- **Rationale**: FR-001. `IBrokerageHoldingsReader` is already in Core. Add a **new Core
  `IWatchlistReader`** (impl in Research over `IWatchlistRepository`) rather than coupling Radar to a
  Research-internal repo — smallest addition that preserves module boundaries.
- **Alternatives**: Radar referencing `IWatchlistRepository` directly — rejected (module coupling).
  Crypto/cash excluded by asset-class filter (`ICryptoHoldingsReader`/`IBankingTotalsReader` not used
  for the equity universe).

## R5 — Metric definitions (the deterministic core)

- **Decision**: Pure static functions over the persisted adjusted-close (and volume) series:
  - Returns over 21/63/126/252 trading days (5-day dropped as noise per 2026-07-07 review).
  - RS vs benchmark = `tickerReturn(window) − benchmarkReturn(window)` per window.
  - MAs 20/50/200 (simple, on adjusted close); extension = `(close − MA50) / MA50`.
  - Volatility = stdev of daily log/simple returns over 63 days; today z-score = `todayReturn / σ`.
  - Volume ratio = `todayVolume / avg20Volume`.
  - Sector rotation = rank the sector ETFs by RS(window); `rankDelta = rank(today) − rank(21d ago)`.
  - Breadth = % of universe tickers with `close > MA{20,50,200}`.
- **Rationale**: FR-004/005/006/012; windows aligned to momentum literature. All inputs are the
  persisted bars → identical bars in, identical numbers out (SC-001).
- **Edge handling**: `< N` bars → that window is **not evaluable** (null), never zero (Edge Cases);
  zero volume → volume ratio not evaluable; missing days handled by trading-day indexing (no calendar
  arithmetic).

## R6 — Signal log shape, dedup, and severity

- **Decision**: `radar_signals` columns per FR-007 + `PayloadVersion` (int) + nullable `UserId`
  (set for held-ticker/holder-scoped signals; null for global ones like breadth/rotation). Append-only.
  `RadarSignalWriter` dedups **`notable`+** by `DedupKey` within a configurable silence window
  (`HasRecent`-style check against the store); `info` signals recorded every run.
- **Rationale**: FR-007/009. `PayloadVersion` lets payload shapes evolve per signal type without
  breaking readers. Retention: `info` pruned after a configured horizon (default 2y) by a prune step;
  `notable`+ kept indefinitely.
- **Alternatives**: Writer-side no dedup (like 017's alert path) — rejected here because the signal
  log itself is the dedup authority (there is no downstream Alerts dedup for `info`/`notable` signals).

## R7 — Calibration mode & alerting gate (FR-015/016)

- **Decision**: `ScannerMode` config (`LogOnly` default | `Alerting`). In `LogOnly`, compute emits
  signals only (zero Alerts). Held-ticker `unusual_move` at/above the alert bar raises
  `AlertType.MarketStructure` **only** when mode is `Alerting`. Thresholds (FR-008) are config values.
  `RunHistoricalValidationCommand` replays the same pure functions over ≥5y of persisted bars to count
  frequency/precision across the 2020 crash, 2022 unwind, 2026-07 memory rotation before flipping mode.
- **Rationale**: FR-015/016, SC-002 — no alert spam; thresholds earned from data, not guessed. The
  practitioner/architect judges cut the `trim_into_strength` composite to v2 for the same reason —
  018 v1 emits only individually-defensible signals.
- **Alternatives**: Ship alerting on day 1 — rejected (untuned thresholds spam or stay silent).

## R8 — Freshness watchdog (FR-017)

- **Decision**: `RadarFreshnessWatchdogJob` (or a check at compute time) raises an
  `AlertType.MarketStructure` freshness alert when any universe ticker's latest bar is older than N
  trading days (config default 2) or an ingestion run failed outright. Every structure read carries a
  `stale: bool` flag in its payload/DTO when computed over stale data.
- **Rationale**: FR-017 — never answer confidently from dead data.

## R9 — Alerts integration

- **Decision**: Add `AlertType.MarketStructure` const; add `GenerateMarketStructureAlertAsync(...)`
  (+ a freshness variant/reason) to `IAlertGeneratorService` (Core) implemented in
  `AlertGeneratorService` via the established `FindActive → HasRecent(silence) → AddAsync` shape;
  `ReferenceId` = the held holding/thesis subject id or a deterministic per-ticker guid,
  `ReferenceLabel` = ticker. Held-only, `Alerting` mode only.
- **Rationale**: FR-010/017 — reuse the loud tier; no channel delivery in-module (SC-005).

## R10 — Scheduling

- **Decision**: `RadarIngestionJob` daily post-US-close, then `RadarComputeJob` (chained or separate
  recurring job slightly later), plus `RadarFreshnessWatchdogJob`. Registered via the module's
  `JobRegistrar` (`IRecurringJobManager.AddOrUpdate`, `Cron.Daily(hour)`), auto-discovered.
- **Rationale**: FR-002; mirrors `NetWorthSnapshotJob`/`ThesisMonitorJob`. Weekend/holiday runs no-op
  gracefully (no new trading day → idempotent insert of zero rows).
