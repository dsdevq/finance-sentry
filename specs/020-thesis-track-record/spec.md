# Feature Specification: Thesis Track Record

**Feature Branch**: `020-thesis-track-record`
**Created**: 2026-07-07
**Status**: Implemented
**Input**: The "honesty layer" of the Radar (see `specs/ROADMAP.md`): price-stamped lifecycle logging for every thesis and opportunity candidate, and performance measurement vs benchmark — so the system's (and Denys's) edge is measured, not assumed.

## Why this spec exists

The program goal is earning money. Without measurement, edge and luck are indistinguishable: a system that feels smart can quietly lose to SPY. This feature stamps market prices onto every thesis/candidate lifecycle event and computes returns vs benchmark, hit rates, and counterfactuals ("the candidates I rejected — how did they do?"). It is deliberately cheap: an event log, a snapshot job, and read queries. Deterministic tier 1; interpretation of *why* the record looks the way it does stays with Ledger.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Every lifecycle event is price-stamped (Priority: P1)

When a thesis is created, broken, un-broken, or closed — and when a candidate is created, promoted, rejected, or expired — the event is recorded with the subject's price and the benchmark's price at that time.

**Independent Test**: Create a thesis via the existing save path; assert a `Created` event exists with ticker price and SPY price. Mark it broken via the 017 monitor; assert a `Broken` event with fresh prices.

**Acceptance Scenarios**:

1. **Given** a thesis is saved for the first time, **Then** a `Created` event is stored with `(ticker price, benchmark price, timestamp)`.
2. **Given** 017 marks a thesis broken (or un-broken), **Then** a `Broken` (`Unbroken`) event is stored with prices at evaluation time.
3. **Given** a candidate is promoted/rejected/expired (019), **Then** the corresponding event is stored with prices — including for rejected candidates (counterfactual base).
4. **Given** the price source is unavailable at event time, **Then** the event is stored with prices marked pending and backfilled by the next snapshot job — an event is never lost to a quote failure.

### User Story 2 — Performance vs benchmark (Priority: P1)

For any thesis or candidate, the system reports return since creation (and between any two lifecycle events) alongside the benchmark's return over the same span, plus current open P&L context for held theses.

**Independent Test**: Seed a `Created` event at price 100 (SPY 500) and daily bars ending at 120 (SPY 510); assert return +20%, benchmark +2%, excess +18%.

**Acceptance Scenarios**:

1. **Given** an active thesis, **When** performance is queried, **Then** it returns absolute return, benchmark return, and excess return since `Created`, using the latest daily bar (018).
2. **Given** a broken/closed thesis, **Then** the same is computed create→break (create→close).
3. **Given** a rejected candidate, **Then** the counterfactual return since rejection is computable — "what the rejects did."

### User Story 3 — Aggregate track record (Priority: P2)

One call answers: how many theses/candidates, hit rate (excess return > 0 at close/break or currently), average excess return, and the user-vs-scan comparison (019 sources) — the "is this system actually earning" view.

**Acceptance Scenarios**:

1. **Given** a mix of active/broken/promoted/rejected records, **When** `get_track_record` is called, **Then** it returns counts, hit rate, average/median excess return, best/worst, split by source (`User` vs `Scan`) and by status.
2. **Given** fewer than a configured minimum of closed records, **Then** the summary carries a low-sample caveat flag (no fake confidence).
3. **Given** a weekly snapshot job, **Then** every active thesis/candidate gets a periodic valuation snapshot so history plots don't depend on event density.

### Edge Cases

- Ticker with no bars in 018 (delisted, foreign) → performance "not evaluable"; aggregate excludes it and reports the exclusion count.
- Thesis on a basket/theme with a `proxyTicker` (017) → performance uses the thesis ticker if quotable, else the proxy, and says which was used.
- Events recorded outside market hours use the latest available close — consistently for subject and benchmark.
- Duplicate event suppression: one `Created` per thesis; monitor re-runs don't duplicate `Broken` events (aligned with 017 idempotency).

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST record an append-only `ThesisEvent` log: `SubjectType (Thesis|Candidate), SubjectId, EventType (Created|Broken|Unbroken|Closed|Promoted|Rejected|Expired|Snapshot), Timestamp, SubjectPrice?, BenchmarkPrice?, PricesPending(bool)`.
- **FR-002**: Event capture MUST hook the existing write paths (thesis save, 017 break/unbreak, 019 lifecycle) via domain events or repository decorators — no duplicated business logic in those features.
- **FR-003**: Prices MUST come from persisted daily bars (018) or the existing quote service; a quote failure never blocks the originating operation (FR: record pending, backfill later).
- **FR-004**: A scheduled job (Hangfire, weekly by default) MUST snapshot every active thesis/candidate with current prices and backfill any pending event prices.
- **FR-005**: The system MUST compute deterministically: absolute return, benchmark return, and excess return between any two events (and event→latest), using adjusted closes.
- **FR-006**: Hit rate MUST be defined as excess return > 0 measured at terminal event (break/close/expiry) for terminal records and at latest bar for active ones, reported separately.
- **FR-007**: Aggregates MUST split by candidate source (`User` vs `Scan`) and by subject status, and MUST carry a low-sample caveat below a configured minimum count (default 30 closed records — below that, hit-rate statistics are noise and the summary MUST say so).
- **FR-007b (costs & taxes)**: Performance MUST be reportable **net of frictions**: a configured per-trade cost estimate and a configured tax model (short-term vs long-term capital-gains rates, holding period from the event trail) applied to terminal returns. Gross and net figures are both shown — at small book sizes, short-term tax on rotation trades can erase the entire edge, and the honesty layer must not hide that. *(Added per 2026-07-07 review.)*
- **FR-008**: The system MUST expose MCP tools: `get_track_record()`, `get_thesis_performance(id | ticker)`, `list_thesis_events(subjectId?)`.
- **FR-008b (decision journal — added 2026-07-08 gap-check)**: Every lifecycle event MUST accept an optional `DecisionNote` (free text: the reasoning at decision time, captured contemporaneously via the promoting/creating client). Outcome data without pre-decision reasoning cannot separate good decisions from lucky ones (outcome bias); the journal is the post-mortem's data foundation.
- **FR-008c (post-mortem packet — added 2026-07-08 gap-check)**: The system MUST expose `get_postmortem_packet(period)` returning, for a review period: every terminal event with its decision notes, entry/exit prices, excess return, the counterfactuals, and override log — the raw material for a scheduled (semi-annual) process review conducted by Denys + Ledger. The system compiles; it does not judge.
- **FR-009**: Nothing in this feature deletes or mutates past events; corrections append.
- **FR-010**: The feature MUST NOT execute trades and MUST NOT deliver to external channels.

### Key Entities *(data changes)*

- **ThesisEvent** *(new, append-only)* — as FR-001, indexed `(SubjectType, SubjectId, Timestamp)`.
- No changes to `InvestmentThesis`, 017, or 019 schemas — capture is additive via hooks.

### Success Criteria *(mandatory)*

- **SC-001**: Return math is pure and unit-tested (splits/adjusted closes, pending-price backfill, not-evaluable paths); identical inputs → identical outputs.
- **SC-002**: Creating and breaking a thesis in a live stack produces a complete, correctly priced event trail with zero manual steps.
- **SC-003**: `get_track_record` answers the "is this earning money" question in one call: hit rate, average excess return vs SPY, user-vs-scan split, sample-size caveat.
- **SC-004**: Rejected candidates' counterfactual performance is queryable — the system can prove or disprove "I should have taken that one."
- **SC-005**: Event capture adds no measurable latency (>50ms) or failure coupling to thesis/candidate write paths.

## Assumptions & Dependencies

- **Ships as v0 right after 017** (events + quote-service prices) per the 2026-07-07 review resequencing — the measurement clock must start immediately. Upgrades to persisted-bar pricing when 018 lands. Depends on 017/019 write paths for hook points (019 hooks activate when 019 ships).
- Tax/cost parameters (FR-007b) are configuration supplied by Denys (jurisdiction-specific); defaults are placeholders, not advice.
- Existing Hangfire and MCP patterns; constitution gates apply.

## Notes / Decisions

- **[DECISION]** Track record measures *theses and candidates*, not brokerage fills — it answers "does the decision system work," not accounting P&L (the Wealth module owns actual balances).
- **[DECISION]** Excess return vs SPY is the primary metric — beating zero is not the bar; beating the index is.
- **[DECISION]** Rejected candidates are first-class: counterfactuals are how the 019 scan and Denys's intuition get honestly compared.
- **[OUT OF SCOPE]** Historical backtesting before feature launch, tax/fee modeling, position sizing analytics, any UI beyond MCP/REST in v1.
- **[MCP CONTRACT]** Three new tools (FR-008). Update the MCP tool-count contract test.
