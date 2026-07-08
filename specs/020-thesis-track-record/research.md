# Research: Thesis Track Record

## R1 — How to hook lifecycle events without coupling to 017/019

**Decision**: Introduce `IThesisEventRecorder.RecordAsync(subjectType, subjectId, ticker, eventType, decisionNote?)` in `FinanceSentry.Modules.Research.Application.Services`. `SaveThesisCommand`'s existing handler calls it once, right after persisting a *new* `InvestmentThesis` (never on update — FR: "one `Created` per thesis"). 017's break/unbreak write path and 019's promote/reject/expire write path will call the same interface when they land; both live in the same module so this is an in-module dependency, not a cross-module one.

**Rationale**: Spec FR-002 requires "no duplicated business logic" in 017/019, and the constitution (Principle I) forbids modules coupling directly to concrete implementations of things outside their own domain — but 017/019/020 are all inside `FinanceSentry.Modules.Research`, so a shared interface within that module is the correct boundary, not a new Core interface. Matches how `INetWorthSnapshotService` was used for a genuinely cross-module case (015) — that pattern is reserved for calls that cross module boundaries, which this is not.

**Alternatives considered**: EF Core `SaveChanges` interceptor emitting domain events — rejected: harder to attach `DecisionNote` (caller-supplied, not derivable from entity state) and harder to test in isolation; a plain injected service is simpler and matches the existing `SaveThesisTool → ICommandHandler` style already in the codebase.

## R2 — Price source in v0

**Decision**: `ThesisEventRecorder` calls the existing `IMarketDataService.GetQuoteAsync` (or equivalent — confirm exact method name in `Application/Services/IMarketDataService.cs` at implement time) for both the subject ticker and a hardcoded benchmark ticker (`"SPY"`) at event time. On any exception or null quote, the event is still appended with `PricesPending = true`, `SubjectPrice = null`, `BenchmarkPrice = null`.

**Rationale**: 018 (persisted daily bars) doesn't exist yet; spec's Assumptions section explicitly says "v0 ships right after 017... Upgrades to persisted-bar pricing when 018 lands." Building against a service that doesn't exist would block this feature indefinitely.

**Alternatives considered**: Block v0 until 018 ships — rejected per the 2026-07-07 resequencing decision recorded in the spec itself ("the measurement clock must start immediately").

## R3 — Net-of-friction return calculation (FR-007b)

**Decision**: `ThesisPerformanceCalculator` accepts a `FrictionConfig` (per-trade cost estimate as a flat bps figure, short-term/long-term capital-gains rates, short/long boundary in days) supplied via `IConfiguration` (`appsettings.json` section `ThesisTrackRecord:Friction`), not hardcoded. Gross return is computed first (pure price math); net return subtracts round-trip cost bps and applies the appropriate tax rate based on holding period (`ClosedAt - CreatedAt`) to the *gain* portion only (no tax drag on losses).

**Rationale**: Spec explicitly calls out defaults as "placeholders, not advice" and jurisdiction-specific — configuration, not code, is the correct home. Keeps the calculator pure/unit-testable (SC-001) since the friction params are just more inputs.

**Alternatives considered**: Separate `INetOfFrictionCalculator` in Core for reuse by a future accounting module — rejected as premature; nothing else needs it yet (YAGNI), and it can be extracted later without breaking callers if it becomes cross-module.

## R4 — Hit rate & low-sample caveat (FR-006/FR-007)

**Decision**: Hit = `ExcessReturn > 0`, evaluated at the terminal event (`Broken`/`Closed`/`Rejected`/`Expired`) for terminal records, and at "latest quote" for still-active ones — reported as two separate rates (`terminalHitRate`, `activeHitRate`) rather than blended, so an aggregate response never silently mixes closed and open bets. `get_track_record` sets `lowSampleCaveat = true` when `closedCount < 30` (configurable constant `MinimumClosedSampleSize = 30`, matching the spec's stated default).

**Rationale**: Matches FR-006/FR-007 literally; keeping the two rates separate avoids a common analytics mistake (marking-to-market open positions inflates apparent hit rate).

## R5 — Weekly snapshot job scope

**Decision**: `ThesisTrackRecordSnapshotJob` runs weekly (Hangfire cron, e.g. `Cron.Weekly()`), and on each run: (1) finds all `ThesisEvent` rows with `PricesPending = true` and attempts to backfill prices via `IMarketDataService`; (2) for every thesis/candidate without a terminal event, appends a new `Snapshot` event with current prices — giving history plots a regular cadence independent of how often lifecycle events actually fire (spec User Story 3, Acceptance Scenario 3).

**Rationale**: Directly matches spec language; weekly (not monthly like 015's net-worth snapshot) because thesis-level moves are more time-sensitive than net-worth trend and the dataset is tiny (single-digit theses), so weekly has negligible cost.

## R6 — Not-evaluable / proxy ticker handling (Edge Cases)

**Decision**: `ThesisPerformanceCalculator` returns a `NotEvaluable` result variant (not an exception) when neither the thesis ticker nor its `proxyTicker` (017 concept — field doesn't exist on `InvestmentThesis` yet) is quotable. Aggregate queries (`GetTrackRecordQuery`) count and exclude these, reporting `excludedCount` per FR/edge-case requirement. Until 017 adds `ProxyTicker` to `InvestmentThesis`, the calculator's proxy-fallback branch is unreachable but present (documented in code comment) so 017 lands without touching this feature's calculator logic again.

**Rationale**: Keeps the "system never reports fake confidence" principle from the spec; avoids reopening this file when 017 ships the field.
