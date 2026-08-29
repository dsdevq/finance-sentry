# Feature Specification: Market Regime Scanner

**Feature Branch**: `021-market-regime`
**Created**: 2026-08-08
**Status**: Implemented
**Input**: User description: "021 — Market Regime Scanner. The professional-grade context: two orthogonal, evidence-backed macro axes (equity volatility via VIX, interest-rate/curve via FRED 10y-2y) classified into deterministic regimes, logged as radar signals, exposed via MCP, and fed as *context* into the 019 opportunity scoring. Regime is never an action — stay-invested default holds."

## Overview

Every scanner shipped so far reads *micro* structure — one ticker, one sector, one book. None reads the *macro weather* the whole book sits in. A candidate scoring 90 on relative strength means something very different when the VIX is 12 and the curve is steep (early-cycle, risk-on) than when the VIX is 38 and the curve is inverted (crisis, recession-warning). Feature 021 supplies that missing context: a daily, deterministic read of two orthogonal macro axes and a way to feed them into the existing opportunity pipeline **without ever auto-actioning** — regime modulates *ranking and framing*, never buy/sell/cash decisions.

The two axes are deliberately the only two with real evidence behind them (the 2026-07-07 roadmap review cut sentiment indices — CNN Fear & Greed, crypto Fear & Greed — as folklore-grade):

- **Volatility regime** — the CBOE Volatility Index (`^VIX`), level + trend.
- **Rates regime** — the U.S. Treasury constant-maturity yield curve (FRED `DGS10`, `DGS2`), 10y-2y spread + inversion state.

It rides the exact pattern every scanner uses: a daily Hangfire job runs a deterministic classifier over config-driven thresholds, writes to the shared `radar_signals` log (daily `info` readings per axis, a `regime_change` `notable` signal when either axis crosses a band), and surfaces the current state through an MCP tool.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See the current market regime on both axes (Priority: P1)

As the operator (via the Ledger companion agent), I ask "what's the market regime right now?" and get back both orthogonal axes at once: the volatility regime (with the raw VIX level and its trend direction), the rates regime (with the raw 10y-2y spread and inversion state), and when each axis last changed band. The two axes are never flattened into a single "risk-on/risk-off" label — they are reported independently because they carry independent information (you can have a calm-but-inverted market, or a panicked-but-steep one).

**Why this priority**: This is the core deliverable — a truthful, deterministic macro read. Everything else (signals, 019 coupling) is downstream of having the classification exist and be queryable. It is independently valuable on its own: the operator gets professional-grade context for every discretionary decision even before any automated coupling.

**Independent Test**: Seed VIX bars and yield observations (fixtures), run the compute command, call `get_market_regime()`, and assert both axes classify to the expected bands with the raw readings and last-change dates present.

**Acceptance Scenarios**:

1. **Given** VIX closing at 12.5 with a falling 20-day SMA and a 10y-2y spread of +1.8%, **When** the regime compute runs and I call `get_market_regime()`, **Then** the volatility axis reports `Calm` / trend `Falling`, the rates axis reports `Steep`, both carry the raw level/spread, and neither is combined into one label.
2. **Given** VIX closing at 34 with a rising SMA and a 10y-2y spread of -0.3%, **When** compute runs, **Then** the volatility axis reports `Panic` / trend `Rising`, the rates axis reports `Inverted` with a recession-warning flag set, and the raw values are echoed back.
3. **Given** no regime has ever been computed, **When** I call `get_market_regime()`, **Then** the tool returns an explicit "no reading available" result rather than a fabricated or defaulted regime.

---

### User Story 2 - Regime readings and changes are logged as radar signals (Priority: P2)

As the operator, I want each day's regime read recorded on the shared signal log so history accrues and so a *change* of regime is a first-class, deduped `notable` event I (or Ledger) can react to — while an unchanged day is a quiet `info` reading that never spams.

**Why this priority**: The signal log is the shared substrate every downstream consumer (Ledger briefs, future dashboards, 020 measurement) reads. Without it, the regime is ephemeral. It depends on US1 producing a classification.

**Independent Test**: Run compute twice against the same fixture (no band change) and assert two `info` readings per axis but zero `regime_change` signals; then run against a fixture that crosses a band and assert exactly one `regime_change` `notable` signal for the axis that moved.

**Acceptance Scenarios**:

1. **Given** yesterday's volatility regime was `Normal` and today's is still `Normal`, **When** compute runs, **Then** a daily `info` reading is appended for each axis and **no** `regime_change` signal is emitted.
2. **Given** yesterday's rates regime was `Flat` and today's crosses into `Inverted`, **When** compute runs, **Then** exactly one `regime_change` `notable` signal is appended for the rates axis (carrying `from`/`to` bands and the raw spread) and the volatility axis — unchanged — emits only its daily `info` reading.
3. **Given** the volatility axis crosses a band and compute is (erroneously) run twice the same day, **When** the second run executes, **Then** the `regime_change` signal is suppressed by the silence-window dedup and is not duplicated.

---

### User Story 3 - Regime context adjusts opportunity scoring (never actions) (Priority: P3)

As the operator, I want the opportunity scanner (019) to *account for* the macro regime when it scores a candidate — a risk-off / high-VIX regime should haircut speculative, over-extended, high-beta candidates; an inverted curve should tilt the read toward quality/value — so a chase-machine can't rank a frothy momentum name top-of-list during a crisis. Crucially, this is **context only**: the regime never raises cash, never sells, never blocks a promotion. It modulates the *presented/ranked* structure score and is fully explained; the canonical raw score is preserved untouched.

**Why this priority**: This is the "fully wired" payoff — regime earning its keep inside the existing pipeline. It depends on both US1 (classification) and the 019 scoring path already shipping. It is the lowest priority because the classification + signals are valuable standalone, and this coupling must be added conservatively (deterministic, documented, reversible) to respect the stay-invested default.

**Independent Test**: Score the same candidate fixture under a `Calm`/`Steep` regime and under a `Panic`/`Inverted` regime; assert the raw structure score is identical in both, the regime-adjusted score is lower under `Panic`/`Inverted` for an *extended/crowded* candidate, unchanged for a *non-speculative* candidate, and the adjustment carries a human-readable rationale in both cases. Assert no cash/sell/promotion behaviour changes.

**Acceptance Scenarios**:

1. **Given** an over-extended, high-volume (Extended crowding) candidate and a `Panic` volatility regime, **When** it is scored, **Then** the raw structure score is preserved and the regime-adjusted structure score is haircut by the configured Panic amount, with a rationale naming the regime and the speculativeness driver.
2. **Given** a non-speculative (Early/Neutral crowding) candidate and the same `Panic` regime, **When** it is scored, **Then** the haircut is smaller-or-zero per the configured rules and the rationale explains why.
3. **Given** an `Inverted` rates regime, **When** an extended candidate is scored, **Then** an additional configured inversion haircut is applied on top of any volatility haircut, and the rationale names the recession-warning tilt.
4. **Given** no regime reading is available (e.g. FRED keyless and VIX fetch failed), **When** a candidate is scored, **Then** the regime-adjusted score equals the raw score, the rationale is `no_regime_data`, and scoring proceeds normally (no failure, no action).

---

### Edge Cases

- **VIX fetch fails / empty** (Yahoo outage): the volatility axis is not classified; compute records the failure, emits no volatility `info`/`change` signal for that day, and does not fabricate a level. The rates axis still computes independently.
- **FRED keyless (no `FRED_API_KEY`)**: the rates source is silent (exactly like the Finnhub free-API pattern) — no fetch, no rates classification, no error. The volatility axis still computes. `get_market_regime()` reports the rates axis as unavailable.
- **FRED returns `.` placeholders** (its documented "no observation" marker on holidays/weekends): those observations are skipped; the latest *valid* DGS10/DGS2 pair is used for the spread.
- **Insufficient VIX history for the SMA** (fewer than the configured trend window of bars): the level classifies but trend reports `Unknown` rather than a guessed direction.
- **Value sitting exactly on a band boundary**: bands use half-open intervals with documented inclusive/exclusive edges so a boundary value classifies deterministically to exactly one band.
- **First-ever run** (no prior reading to compare): daily `info` readings are emitted; a `regime_change` is emitted only if a prior reading exists to cross from (a first read is not a "change").
- **Regime data unavailable to 019**: scoring must degrade gracefully — raw score preserved, no adjustment, explicit `no_regime_data` rationale, never an exception.

## Requirements *(mandatory)*

### Functional Requirements

#### Volatility axis (VIX)

- **FR-001**: System MUST fetch `^VIX` daily bars via the existing swappable market-history source (the same Yahoo-backed `IMarketHistorySource` the Radar module already uses), reading persisted-or-fetched closes without introducing a second price vendor.
- **FR-002**: System MUST compute a volatility **level band** from the latest VIX close using config-driven thresholds, and a **trend direction** (Rising / Falling / Flat / Unknown) by comparing the latest close to its own simple moving average over a config-driven window.
- **FR-003**: The volatility bands MUST be, by default and documented as evidence-based: `Calm` (VIX < 15), `Normal` (15 ≤ VIX < 20), `Stressed` (20 ≤ VIX < 30), `Panic` (VIX ≥ 30). Thresholds MUST be overridable from configuration.

#### Rates axis (yield curve)

- **FR-004**: System MUST introduce a **FRED** data source (St. Louis Fed `fred/series/observations`) as a new free-API integration accessed via `IHttpClientFactory`, plain REST + JSON, no heavy new NuGet dependency.
- **FR-005**: The FRED source MUST be keyless-silent: keyed from `FRED_API_KEY` bound to configuration (mirroring the Finnhub `AnalystSources__Finnhub__ApiKey` pattern); when the key is blank the source performs no fetch, raises no error, and the rates axis reports as unavailable.
- **FR-006**: System MUST pull FRED series `DGS10` and `DGS2` (10-year and 2-year constant-maturity treasury yields), skip FRED's `.` no-observation placeholders, take the latest valid pair, and compute the **10y-2y spread** (percentage points).
- **FR-007**: System MUST classify a rates **regime band** from the spread using config-driven thresholds, and set a **recession-warning** flag when the curve is inverted. Default, documented bands: `Inverted` (spread < 0), `Flat` (0 ≤ spread < 0.5%), `Normal` (0.5% ≤ spread < 1.5%), `Steep` (spread ≥ 1.5%). Thresholds MUST be overridable from configuration.
- **FR-008**: System MUST derive a documented **growth-vs-value tilt** hint from the rates band (e.g. `Steep`/`Normal` → cyclical/value-supportive early-cycle; `Inverted` → quality/defensive recession-warning) exposed as context, never as an action.

#### Classification discipline

- **FR-009**: Regime classification on both axes MUST be pure and deterministic — same inputs always yield the same bands — with all thresholds bound from a single configuration section (no magic numbers in code, FR-008 parity with the Radar/Opportunity scanners).
- **FR-010**: The two axes MUST remain orthogonal and independently reported — the system MUST NOT collapse them into a single combined regime label anywhere (persistence, signals, MCP, or scoring context).

#### Persistence & signals

- **FR-011**: Each compute run MUST persist a **latest regime reading** row (both axes' bands, raw VIX level + SMA + trend, raw DGS10/DGS2 + spread + recession flag, computed-at timestamp) so the current state and history are queryable without replaying the signal log.
- **FR-012**: Each compute run MUST append a daily `info` regime reading to the shared `radar_signals` log for each axis that classified.
- **FR-013**: When an axis crosses into a different band versus the most recent prior reading, the run MUST append exactly one `regime_change` `notable` signal for that axis, carrying `from`/`to` bands and the raw driver value; an unchanged axis MUST NOT emit a change signal.
- **FR-014**: `regime_change` signals MUST be deduped by the existing silence-window mechanism so a same-day re-run does not duplicate a change.
- **FR-015**: The regime feature MUST run as a daily Hangfire recurring job scheduled after the US market close / FRED daily update, using the shared signal-writer and shared log (no new signal infrastructure).

#### MCP surface

- **FR-016**: System MUST expose an MCP tool `get_market_regime()` that returns **both axes**: current band per axis, the latest raw readings (VIX level/SMA/trend; DGS10/DGS2/spread/recession flag), the growth-vs-value tilt hint, and the last-change date per axis. It MUST follow the existing tool conventions (`[McpServerTool(Name = "get_market_regime")]`, DI-resolved query handler, registered by assembly scan).
- **FR-017**: When an axis has never been read or its source is unavailable, `get_market_regime()` MUST report that axis as explicitly unavailable rather than returning a fabricated band.

#### 019 coupling (context, never action)

- **FR-018**: The opportunity scoring path (`ScoreCandidateCommand` and, by extension, the scheduled `OpportunityScanJob`) MUST read the latest regime via a **read-only cross-module port** (defined in Core, implemented in the regime's module) — the Research module MUST NOT reference the regime module directly, preserving modular-monolith boundaries.
- **FR-019**: Regime MUST modulate a candidate's **regime-adjusted structure score** by a deterministic, config-driven adjustment while **preserving the raw structure score untouched** for formula-version integrity and explainability. The adjustment and its rationale MUST be attached to the returned scorecard.
- **FR-020**: The adjustment rules MUST be: (a) a **volatility haircut** in `Stressed`/`Panic` regimes scaled by candidate speculativeness (crowding class — Extended most penalized), (b) an **additional inversion haircut** for speculative candidates when the rates axis is `Inverted`, (c) clamped to the 0–100 range, (d) documented with named config constants and default magnitudes.
- **FR-021**: Regime MUST NEVER auto-action: it MUST NOT trigger cash-raising, selling, promotion blocking, or any book change. It only reranks/reframes scoring. The stay-invested default is inviolable.
- **FR-022**: When no regime reading is available, the regime-adjusted score MUST equal the raw score with a `no_regime_data` rationale and scoring MUST proceed without error.

#### Quality gates

- **FR-023**: All new C# MUST build with zero `dotnet build` warnings; the migration adding any new table MUST be generated/authored with a complete EF Designer + `ModelSnapshot` update and `[DbContext]`/`[Migration]` attributes so it is actually applied (a hand-written attribute-less migration is a known past failure and is forbidden).
- **FR-024**: Unit tests MUST cover: band-boundary classification on both axes (including exact-boundary values), trend direction, inversion + recession flag, the FRED source (keyless no-op + `.`-placeholder parse + latest-pair selection), the regime-change-vs-no-change signal emission, and the 019 scoring adjustment (haircut applied vs not applied vs no-data).

### Key Entities *(include if feature involves data)*

- **RegimeReading**: The persisted latest/historical snapshot of one compute run. Attributes: computed-at timestamp; volatility band, raw VIX level, VIX SMA, trend direction; rates band, raw DGS10, raw DGS2, 10y-2y spread, recession-warning flag; growth-vs-value tilt hint. One row per successful compute; the newest is "current".
- **RegimeSignal (radar_signals rows)**: Not a new entity — reuses the shared log. `info` daily readings (one per axis) and `regime_change` `notable` events (per axis that crossed), under a new `market_regime` scanner key with `regime_volatility` / `regime_rates` / `regime_change` signal types.
- **MarketRegimeSnapshot (cross-module DTO / port result)**: The read-only shape the Core port `IMarketRegimeSource` returns to the Research module — both axes' current bands, raw drivers, recession flag, and last-change dates — carrying no EF or module-internal types.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Given any VIX close and any 10y-2y spread, the classifier assigns each axis to exactly one band deterministically (same input → same band on every run), verified across the full boundary set in unit tests.
- **SC-002**: `get_market_regime()` returns both axes with raw readings and last-change dates in a single call, and never returns a fabricated band for an unavailable axis.
- **SC-003**: A day with no band change produces zero `regime_change` signals; a day that crosses a band on one axis produces exactly one `regime_change` signal on that axis and none on the other.
- **SC-004**: For a fixed candidate, switching the regime from `Calm`/`Steep` to `Panic`/`Inverted` lowers the regime-adjusted structure score for an Extended-crowding candidate while leaving the raw score unchanged, and produces no cash/sell/promotion side effect (stay-invested default holds).
- **SC-005**: With `FRED_API_KEY` blank, the system runs end-to-end with the rates axis silently unavailable and no errors, exactly as the Finnhub keyless pattern behaves.
- **SC-006**: The whole backend builds with zero warnings and the new migration is confirmed applied (present in `__ef_migrations_history_radar` on a real up-migrate, with a matching model snapshot).

## Assumptions

- The regime feature is implemented **inside the existing Radar module** (schema `radar`), reusing `RadarDbContext`, the shared `radar_signals` log + `IRadarSignalWriter`, the `IMarketHistorySource` for VIX, and the module's config/job wiring. Rationale: the roadmap states later scanners "plug into the same signal log as small independent features — no new architecture needed"; a separate DbContext/module would duplicate migration-history and signal plumbing for two scalar time series. The new table is added via the **next Radar migration (M002)** after the existing M001.
- The 019 opportunity scoring path already ships (feature 019) with `ScoreCandidateCommand`, `CandidateScorecard`, `StructureScorer`, and `CrowdingClassifier`; this feature extends the scorecard with regime context rather than rewriting the scorers.
- FRED's free API is a documented plain REST + JSON endpoint; the free key is obtained at fred.stlouisfed.org and stored in `.env.sops` as `FRED_API_KEY` (blank in dev by default → rates axis silent). No paid tier is required.
- VIX via Yahoo `^VIX` is the same client already pinned with a browser User-Agent; no new vendor contract.
- Backend-only. No Angular/frontend work. The only external surface is the MCP tool.
- Evidence basis for defaults: VIX ~20 is its long-run average; sub-15 reflects complacency/low-vol, 20–30 elevated stress, 30+ crisis (2008/2020/major corrections spike well past 30). The 10y-2y spread inverting (< 0) has preceded every U.S. recession since the 1950s by ~6–18 months; a steep curve (> ~1.5%) is characteristically early-cycle/expansionary. These are widely-published conventions, documented here so the thresholds are auditable, not invented.

## Notes

- [DECISION] Two orthogonal axes, never one label: volatility and rates carry independent information and are reported/persisted/scored separately (FR-010). Collapsing them into a single "risk-on/off" score would destroy exactly the nuance that makes the read professional-grade.
- [DECISION] Regime lives in the Radar module: reuses the shared signal log, market-history source, and DbContext; adds Radar migration M002 (`regime_readings` table). Avoids a redundant DbContext/module for two scalar series. Cross-module reads by 019 go through a Core port (`IMarketRegimeSource`) so Research never references Radar directly (Principle I).
- [DECISION] Raw structure score is preserved; regime produces a *separate* regime-adjusted score + rationale on the scorecard. The persisted canonical structure score (governed by `FormulaVersion`) is not overwritten by a time-varying macro input — that keeps historical scorecards honest and the adjustment fully explainable and reversible.
- [DECISION] Regime is context, never action (FR-021): no cash-raising, no selling, no promotion block. Enforces the roadmap's stay-invested default. The coupling only reranks/reframes.
- [OUT OF SCOPE] Sentiment indices (CNN Fear & Greed, crypto Fear & Greed): deliberately rejected in the 2026-07-07 roadmap review as folklore-grade; not built.
- [OUT OF SCOPE] Any frontend/Angular UI: this feature's only surface is the MCP tool and the signal log.
- [OUT OF SCOPE] Alerting/Telegram on regime change: `regime_change` is a `notable` signal on the log; wiring it to the Alerts→Companion→Telegram path is a later, separate concern (the scanner ships log-only, consistent with 018's calibrate-before-alert discipline).
- [DEFERRED] Additional macro axes (credit spreads, dollar index, breadth-thrust): only the two evidence-backed axes ship now; more can plug into the same log later.
