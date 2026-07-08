# Feature Specification: Market Structure Scanner + Radar Signal Log

**Feature Branch**: `018-market-structure`
**Created**: 2026-07-07
**Status**: Draft
**Input**: The "eyes" of the Radar (see `specs/ROADMAP.md`): daily price-history ingestion for a configurable universe, deterministic market-structure computations (relative strength, sector rotation, breadth, unusual moves, extension), and the shared append-only **signal log** that all Radar scanners write to.

## Why this spec exists

On 2026-07-07 DRAM gapped down ~16% as part of a memory/semis crowded-trade unwind and a broader rotation into megacaps. Ledger explained the single-name catalyst but missed the rotation, because Finance Sentry exposes only spot quotes (`get_quotes`) and fundamentals — nothing that can see *where capital is flowing*. This feature adds that sight, and introduces the accumulation layer (`radar_signals`) so trend context ("memory RS has deteriorated for 3 weeks") exists *before* the loud day.

This service is **tier 1** (deterministic) per the Radar architecture: it computes and records; it does not interpret (Ledger) and does not deliver (Alerts raise, clients consume).

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Daily bars ingested for the universe (Priority: P1)

A scheduled job maintains daily OHLCV history for every ticker in the Radar universe (benchmark, sector ETFs, holdings, watchlist), so all structure metrics are computable locally without re-fetching.

**Independent Test**: Seed a universe of 3 tickers, run the ingestion job against the market-data source, assert ≥ 200 daily bars per ticker exist and a re-run inserts only missing days (idempotent).

**Acceptance Scenarios**:

1. **Given** an empty bar store, **When** ingestion runs, **Then** each universe ticker has daily bars back to the configured lookback (default 300 trading days).
2. **Given** bars exist through yesterday, **When** ingestion runs after today's close, **Then** exactly the new day per ticker is appended (no duplicates, unique on ticker+date).
3. **Given** the source fails for one ticker, **When** ingestion runs, **Then** remaining tickers still ingest and the failure is recorded in the run summary.
4. **Given** a ticker is added to holdings or watchlist, **When** the next ingestion runs, **Then** it is auto-included in the universe and backfilled.

### User Story 2 — Relative strength and sector rotation (Priority: P1)

The system computes, per ticker and per sector ETF, returns over standard windows and relative strength vs the benchmark, and ranks sectors so rotation (rank shifts) is visible.

**Independent Test**: Seed bars where ticker A outperforms SPY and B underperforms over 21 trading days; assert A's RS > 0 > B's RS and the ranking orders A above B.

**Acceptance Scenarios**:

1. **Given** sufficient bars, **When** structure is computed, **Then** every universe ticker has returns and RS vs benchmark for 5/21/63-day windows.
2. **Given** sector rankings for today and 5 trading days ago, **When** rotation is computed, **Then** each sector has a rank delta, and deltas ≥ the configured threshold produce a `rotation_shift` signal.
3. **Given** a held ticker whose sector drops into the bottom quartile, **Then** a `held_sector_laggard` signal is recorded.

### User Story 3 — Breadth, unusual moves, and extension (Priority: P2)

The system computes universe breadth (% above 20/50/200-day MA), flags single-day moves that are large vs the ticker's own volatility, and measures extension from moving averages (crowding proxy).

**Acceptance Scenarios**:

1. **Given** daily bars, **When** structure is computed, **Then** breadth percentages are recorded daily as a `breadth` signal.
2. **Given** a ticker moves ≥ 3σ vs its trailing 63-day daily volatility, **Then** an `unusual_move` signal is recorded with the z-score; a held ticker also raises an Alert (existing Alerts module) when |move| ≥ the alert bar.
3. **Given** a ticker closes ≥ the configured extension threshold above its 50-day MA, **Then** an `extended` signal is recorded (silent — log only).

> **Deferred to v2 — `trim_into_strength` composite** (held + extended + dominant book weight + sector rank falling). Cut from v1 per 2026-07-07 review: the practitioner judge notes extension *is* momentum (trimming on it systematically fights the evidence), and the architect judge notes an untuned three-condition composite will spam or stay silent. Revisit only after the calibration phase and historical validation quantify its precision. Until then, Ledger can narrate the underlying facts (extension, weight, sector rank — all individually available) in pre-earnings briefs without a deterministic alert.

### User Story 4 — Signal log + MCP surface (Priority: P1)

All computed events append to one shared `radar_signals` log; Ledger and the web UI read structure and signals via MCP.

**Independent Test**: Run the scanner, then call `list_signals` filtered by day and type, and verify the emitted signals are returned with payloads.

**Acceptance Scenarios**:

1. **Given** any scanner emission, **Then** the signal has: timestamp, scanner, type, severity (`info`|`notable`|`alerted`), subject (ticker/sector/universe), dedup key, and a JSON payload with the computed evidence.
2. **Given** the same condition on consecutive days, **Then** the dedup key prevents duplicate `notable+` signals within the configured silence window (`info` signals may repeat daily).
3. **Given** a call to `get_market_structure` (or `get_sector_rotation` / `get_relative_strength` / `get_market_breadth`), **Then** current computed values return without triggering ingestion.
4. **Given** a call to `get_radar_summary`, **Then** it returns today's notable signals + sector leaders/laggards + breadth in one payload (Ledger's first-call tool).

### Edge Cases

- Ticker with < 200 bars (recent IPO): compute only windows that fit; longer-window metrics are "not evaluable", never zero.
- Non-US/unsupported symbols in holdings (crypto, Monobank cash): excluded from the equity universe by asset-class filter.
- Market holidays / weekends: ingestion no-ops gracefully; computations use trading days, not calendar days.
- Split/adjusted prices: use the source's adjusted close for return computations.
- Source rate limits: ingestion batches with per-request throttle; a partial run records what remains and completes next run.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST maintain a configurable Radar universe = benchmark (SPY) + the 11 SPDR sector ETFs + configured industry ETFs (seed: SMH) + all equity holdings + all watchlist tickers, auto-syncing the latter two.
- **FR-002**: The system MUST ingest and persist daily OHLCV + adjusted close per universe ticker via a scheduled Hangfire job (default: daily post-US-close), idempotent per ticker+date, with a configurable lookback (default 300 trading days).
- **FR-003**: The system MUST extend the existing Research market-data source (`YahooMarketDataService`) for history; no new external provider and no paid APIs.
- **FR-004**: The system MUST compute deterministically per ticker: returns over 21/63/126/252 trading days (windows aligned with the momentum literature; the 5-day window from the first draft is dropped as noise per 2026-07-07 review); RS vs benchmark per window (`ticker return − benchmark return`); 20/50/200-day MAs; % distance from 50-day MA (extension); 63-day daily-return σ and today's z-score; volume vs 20-day average volume.
- **FR-005**: The system MUST rank sector ETFs by RS per window and compute rank deltas vs 21 trading days prior (rotation).
- **FR-006**: The system MUST compute universe breadth: % of universe tickers above their 20/50/200-day MAs.
- **FR-007**: The system MUST append signals to a shared `radar_signals` store with: `Timestamp, Scanner, SignalType, Severity, SubjectType, Subject, DedupKey, Payload(jsonb)`. The store is append-only; no scanner mutates another's signals.
- **FR-008**: Signal emission thresholds (rotation rank delta, z-score bars, extension %, breadth crossings) MUST be configuration values, not code constants.
- **FR-009**: `notable`-and-above signals MUST be deduped by `DedupKey` within a configurable silence window; `info` signals are recorded every run.
- **FR-010**: For **held** tickers only, `unusual_move` at/above the alert bar MUST raise a domain Alert via the existing Alerts module (new `AlertType.MarketStructure`); the scanner MUST NOT deliver to any external channel. *(The `trim_into_strength` composite is deferred to v2 — see User Story 3 note.)*
- **FR-015 (calibration phase)**: The scanner MUST support a **log-only mode** (signals recorded, zero Alerts) and MUST launch in it. Alert thresholds are set from the observed signal distributions after 2–4 weeks and from historical validation (FR-016); only then is alerting enabled. Mode is configuration.
- **FR-016 (historical validation)**: Signal definitions and thresholds MUST be validated by replaying the computation over ≥ 5 years of persisted daily bars before alerting is enabled — counting signal frequency and precision across at least the 2020 COVID crash, the 2022 growth unwind, and the 2026-07 memory rotation. This is a one-off analysis job over the same pure functions, not a separate backtesting framework.
- **FR-017 (freshness watchdog)**: If any universe ticker's latest bar is older than N trading days (config, default 2), or an ingestion run fails outright, the system MUST raise a domain Alert. Structure reads over stale data MUST carry a staleness flag in the payload — the system must never answer confidently from dead data.
- **FR-011**: The system MUST expose MCP tools: `get_market_structure(ticker)`, `get_relative_strength(tickers?)`, `get_sector_rotation()`, `get_market_breadth()`, `list_signals(since?, scanner?, type?, subject?)`, `get_radar_summary()`. Reads never trigger ingestion.
- **FR-012**: All computations MUST be pure functions over the persisted bar series — same bars in, same numbers out; no LLM, no randomness, no wall-clock dependence beyond "latest bar".
- **FR-013**: A run summary (tickers ingested, bars added, signals emitted by type, errors) MUST be recorded per scanner run.
- **FR-014**: The feature MUST NOT execute trades or account actions.

### Key Entities *(data changes)*

- **DailyBar** *(new)* — `Ticker, Date, Open, High, Low, Close, AdjClose, Volume`; unique `(Ticker, Date)`. New `ResearchDbContext` table (or sibling context per module convention).
- **RadarSignal** *(new — the platform table)* — as FR-007, plus `PayloadVersion` (int) so payload shapes can evolve per signal type without breaking readers. Indexed on `(Timestamp)`, `(Scanner, SignalType)`, `(Subject)`. Retention: `info` signals are pruned after a configured horizon (default 2 years); `notable`+ kept indefinitely. Other scanners (017 follow-up, 019, future portfolio/event scanners) write here too.
- **RadarUniverseMember** *(new)* — `Ticker, Kind (Benchmark|Sector|Industry|Holding|Watchlist), Source (Seed|Auto), Active`.
- **AlertType.MarketStructure** *(new const)* — existing Alerts module.

### Success Criteria *(mandatory)*

- **SC-001**: All metric computations are unit-tested pure functions, including edge cases (short history, zero volume, missing days) — identical inputs always give identical outputs.
- **SC-002**: Replaying seeded historical episodes — at minimum the 2020 COVID crash, the 2022 growth unwind, and the 2026-07 memory rotation — produces the expected signals in each (deteriorating RS before the break, `unusual_move` on the loud day, `rotation_shift` on the sector move), with measured signal frequency low enough not to spam. Generalized beyond the single DRAM episode per 2026-07-07 review.
- **SC-003**: Daily scheduled run (ingest + compute + emit) completes in < 5 minutes for a 100-ticker universe.
- **SC-004**: `get_radar_summary` answers in one call: sector leaders/laggards with rank deltas, breadth, today's notable signals.
- **SC-005**: Zero paid API dependencies; zero channel-delivery dependencies in the module.

## Assumptions & Dependencies

- Yahoo chart/history endpoint (`/v8/finance/chart`, no crumb auth needed — the existing service already calls it with `range=5d`; this feature extends the range) is the v1 source; daily granularity is sufficient (no intraday in v1). History access goes behind a thin `IMarketHistorySource` interface so a fallback provider (e.g. Stooq) can be added without touching consumers — Yahoo must not be a hard-wired single point of failure.
- Holdings tickers come from the existing Wealth/Brokerage aggregation; watchlist from the Research module.
- Alerts module (`012`) is the loud tier; Hangfire scheduling pattern per existing jobs.
- Constitution gates apply (zero-warning build, xUnit on the computation core, CQRS for queries, MCP via `WithToolsFromAssembly`, migrations per module context).

## Notes / Decisions

- **[DECISION]** The signal log lives in this feature because market structure is its first and heaviest writer; the table is deliberately scanner-agnostic so 017/019/future scanners reuse it without schema change.
- **[DECISION]** Crowding is proxied (extension from MA + volume ratio), not measured from flow data — flow/positioning feeds are deferred (see ROADMAP).
- **[DECISION]** Persisting bars is the explicit opposite of 017's "no new time-series" rule and is justified: structure math needs history, and local bars remove per-run dependence on the external source.
- **[OUT OF SCOPE]** Intraday data, estimate revisions, options/fund flows, non-US exchanges, charting UI (MCP/REST only in v1).
- **[MCP CONTRACT]** Six new tools (FR-011). Update the MCP tool-count contract test.
