# Feature Specification: Risk Rules

**Feature Branch**: `022-risk-rules`
**Created**: 2026-07-07
**Status**: Draft
**Input**: The practitioner layer identified as the biggest omission by the 2026-07-07 independent review: written, machine-checked position policy. "For a concentrated book, sizing rules are worth more than every scanner combined." Small feature, big leverage: deterministic checks of the live book against explicit rules, violations surfaced as signals/alerts, and a hard gate on 019's promote flow.

## Why this spec exists

The book today: ~$15k with one position at ~46%. The Radar *watches* that concentration; nothing *forces a plan* about it. Classic retail failure modes — oversizing a conviction, averaging down on a broken thesis, letting a winner become the whole book — are all policy failures, not information failures. This feature turns the IPS from a narrative document into enforceable rules. Deterministic tier 1; the rules are Denys's (set once, changed deliberately), the system only checks them.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Policy violations are detected and surfaced (Priority: P1)

A scheduled check evaluates the live book against the configured risk rules and raises signals/alerts for violations — including pre-existing ones, which get a named remediation status rather than silent tolerance.

**Independent Test**: Configure `maxPositionWeight = 0.25`; with a seeded book holding one position at 46%, run the check; assert a `policy_violation` signal + one Alert naming the rule, the observed value, and the limit.

**Acceptance Scenarios**:

1. **Given** a position exceeds `maxPositionWeight`, **When** the check runs, **Then** a violation is recorded with rule, observed weight, limit, and excess amount in currency.
2. **Given** a violation already alerted within the silence window, **Then** no duplicate alert (existing Alerts dedup).
3. **Given** a pre-existing violation Denys has acknowledged with a remediation note ("trim DRAM on strength to ≤30% by Q4"), **Then** the violation is reported as `Acknowledged` with the note, and re-alerts only if it worsens by a configured step.
4. **Given** the book is fully compliant, **Then** a daily `info` signal records "compliant" — silence toward Denys.

### User Story 2 — Promotion-time gate (Priority: P1)

019's `promote_candidate` (and Ledger, before recommending) checks a proposed position against the rules; a would-be violation refuses with the violated rule named, overridable only explicitly.

**Acceptance Scenarios**:

1. **Given** a proposed position size that would exceed `maxPositionWeight` or drop cash below `minCashBuffer`, **When** `check_risk_rules` is called with the proposal, **Then** it returns `Refused` with the rule, the observed/limit values, and the maximum compliant size.
2. **Given** a compliant proposal, **Then** it returns `Allowed` with headroom facts.
3. **Given** an explicit override flag, **Then** the action proceeds and the override itself is recorded as a signal (overrides must be visible in the track record).

### User Story 3 — Adds to broken theses are flagged (Priority: P2)

Averaging down on a thesis that 017 has marked broken is the classic way small books die. The system cannot block a brokerage trade, but it MUST notice one.

**Acceptance Scenarios**:

1. **Given** a thesis is broken and the synced position quantity subsequently increases, **When** the check runs, **Then** an `add_to_broken_thesis` signal (`notable`) + Alert is raised.
2. **Given** the position increase precedes the break, **Then** no flag.

### Edge Cases

- Rules not configured → check reports "no rules on file" and raises a one-time setup nudge; nothing is inferred or defaulted silently.
- Multi-currency book → weights computed on the existing Wealth module's base-currency valuation.
- Crypto/brokerage sleeves → `maxPositionWeight` applies per asset; a per-sleeve cap (`maxSleeveWeight`) is a separate optional rule.
- Position sync is stale (BrokerageSync failure) → check runs on last-known book but carries the staleness flag; no violation auto-clears on stale data.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST store a versioned `RiskRuleSet` per user: `maxPositionWeight`, `maxSleeveWeight?`, `minCashBufferPct?`, `maxLossPerThesisPct` (from entry, informs the 017 `price_drawdown` trigger default), `maxNewPositionPct` (sizing cap for any single new bet), `turnoverBudget?` (max discretionary trades per quarter), and `allocationTargets?` (target weights per sleeve/asset with a drift band, e.g. ±5pp). All optional individually; changes append a new version (audit trail).
- **FR-001b (turnover guardrail — added 2026-07-08 gap-check)**: The system MUST count discretionary trades per rolling quarter (from synced position changes) and flag when the count reaches the `turnoverBudget`; `check_risk_rules` proposals beyond the budget return `Refused(turnover)`. Evidence basis: trading frequency, not idea quality, is the single largest measured drag on small-book returns (≈7pp/yr spread between most- and least-active retail quintiles).
- **FR-001c (allocation drift — added 2026-07-08 gap-check)**: When `allocationTargets` are configured, the scheduled check MUST report per-sleeve drift and flag breaches of the drift band as `allocation_drift` signals; any rebalancing suggestion surfaced to clients MUST carry the estimated tax/cost friction (020's model) alongside the drift fact.
- **FR-001d (correlation & stress facts — added 2026-07-08 gap-check)**: The compliance report MUST include portfolio-level context facts when 018 bars are available: pairwise 63-day return correlations among top holdings, and a simple stress line ("−30% on top position ⇒ −X% of book"). Facts only — no VaR machinery.
- **FR-002**: A scheduled check (Hangfire, daily after sync) MUST evaluate the live book (existing Wealth valuation) against the active rule set, writing `radar_signals` and raising Alerts (new `AlertType.PolicyViolation`) via the existing dedup.
- **FR-003**: The system MUST support acknowledged violations with a remediation note; acknowledged violations re-alert only on worsening past a configured step.
- **FR-004**: The system MUST expose `check_risk_rules(proposal?)` via MCP: with no argument, current compliance report; with a proposed `(ticker, amount)`, an `Allowed | Refused` verdict with cited rule values and maximum compliant size.
- **FR-005**: The system MUST expose `get_risk_rules` / `save_risk_rules` via MCP; saving validates ranges (weights in (0,1], percentages sane) and appends a version.
- **FR-006**: The system MUST detect quantity increases on positions whose thesis is broken (017 state) and flag them (`add_to_broken_thesis`).
- **FR-007**: Overrides of a `Refused` verdict MUST be recorded as signals — never silent.
- **FR-008**: All checks are deterministic facts against configuration; no LLM, no advice generation. The feature MUST NOT execute or block actual trades (it has no execution surface) and MUST NOT deliver to external channels.

### Key Entities *(data changes)*

- **RiskRuleSet** *(new, versioned)* — per FR-001 plus `CreatedAt`, `Version`.
- **PolicyViolationAck** *(new)* — `RuleKey, Subject, AcknowledgedAt, RemediationNote, WorseningStep`.
- **AlertType.PolicyViolation** *(new const)* — existing Alerts module.
- Writes to **RadarSignal** (018). If 022 ships before 018, signals go to the Alerts module only and the signal-log wiring follows 018 — the check logic is independent of the log.

### Success Criteria *(mandatory)*

- **SC-001**: All rule checks are pure functions over (book valuation, rule set, proposal) with unit tests including the acknowledged-violation and stale-book paths.
- **SC-002**: The seeded real-world case — one position at 46% vs a 25% cap — produces exactly one alert, then an acknowledged remediation state that survives re-runs without spam.
- **SC-003**: 019's promotion path demonstrably refuses an oversized bet end-to-end and names the rule (contract test with 019 when it ships).
- **SC-004**: Every override is visible in signals and, via 020's event trail, in the track record.

## Assumptions & Dependencies

- Book valuation and position quantities come from the existing Wealth/BrokerageSync/CryptoSync modules; no new market-data dependency.
- 017 provides broken-thesis state (FR-006); Alerts module (`012`) is the emission target.
- Rule *values* are Denys's decisions, set via MCP/UI — the system never invents limits.
- Constitution gates apply.

## Notes / Decisions

- **[DECISION]** Rules are enforceable facts, not narrative: the IPS document stays for strategy prose; `RiskRuleSet` is the machine-checked subset.
- **[DECISION]** The system flags and refuses within its own flows (promotion) but never touches brokerage execution — detection of outside actions (FR-006) is the compensating control.
- **[DECISION]** Acknowledged-violation flow exists because the book *starts* in violation (DRAM ~46%); the system must manage remediation, not nag daily about a known state.
- **[OUT OF SCOPE]** Portfolio optimization, VaR/volatility-based sizing models, margin rules, options. Start with weight/cash/loss caps — the rules that actually kill small books.
- **[MCP CONTRACT]** Four new tools (`check_risk_rules`, `get_risk_rules`, `save_risk_rules`, `acknowledge_risk_violation` — the FR-003 ack flow needs a write surface). Update the MCP tool-count contract test. *(Amended 2026-07-08 from three.)*
- **[DECISION 2026-07-08]** A thin REST `RiskController` ships alongside MCP (the future web UI reads the same compliance report); plan.md's "MCP-only" note is superseded.
