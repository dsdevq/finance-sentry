# Feature Specification: IPS ↔ Risk Rules Boundary Cleanup

**Feature Branch**: `039-ips-risk-boundary`
**Created**: 2026-08-07
**Status**: Implemented
**Input**: Two modules store overlapping, independently-editable copies of the same financial-policy concepts (target allocation, position cap). Give each concept exactly one home so the policy the agent reads is unambiguous — a prerequisite for ever delegating money to it.

## Context

The user's financial policy is currently split across two records that **both** claim ownership of the same two concepts:

- **Target asset-class allocation** lives in *both* the Investment Policy Statement (IPS) and the Risk Rule Set — each with its own copy and its own drift/rebalance band.
- **Maximum single-position cap** lives in *both* records too.

Both copies are actively read by different parts of the system, and either can be edited without the other knowing. Today that only risks confusing advice. The moment any portion of money is delegated to the agent, it becomes dangerous: the agent could consult two different, conflicting limits and act on the wrong one.

The agreed principle draws a clean line:

> **The IPS holds *intent* — "what I want."  The Risk Rule Set holds *enforced limits* — "what must never happen."**

Applying it: **target allocation is intent → it belongs only in the IPS**; **a single-position cap is a hard limit → it belongs only in the Risk Rule Set**. Every reader is repointed to the one remaining home, and the duplicate copies are removed. Behaviour must not change — this is a structural cleanup, not a feature.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - One place to set each policy value (Priority: P1)

The user (or the agent on the user's behalf) sets their target asset-class mix in exactly one place, and their maximum single-position limit in exactly one place. There is no second copy that can silently disagree.

**Why this priority**: This is the whole point — a single, authoritative source per concept. Without it, every downstream capability (advice, monitoring, and eventually delegated action) is built on a value that might be contradicted elsewhere.

**Independent Test**: Set the target allocation; confirm it is stored in the intent record and that no allocation copy remains in the limits record. Set the position cap; confirm it is stored in the limits record and no cap copy remains in the intent record. Confirm there is no way to write a second, competing value for either concept.

**Acceptance Scenarios**:

1. **Given** the cleanup is complete, **When** the user's intent record is read, **Then** it contains the target allocation and does **not** contain a single-position cap.
2. **Given** the cleanup is complete, **When** the user's limits record is read, **Then** it contains the single-position cap and does **not** contain a target-allocation copy.
3. **Given** the user updates their target allocation, **When** the update is saved, **Then** exactly one stored value changes and no second allocation copy exists to fall out of sync.

---

### User Story 2 - Nothing behaves differently after the move (Priority: P1)

Allocation-drift monitoring, position-cap enforcement, and opportunity-candidate scoring produce the **same results** after the cleanup as before — same numbers, same verdicts — just sourced from the single home instead of the duplicate.

**Why this priority**: A cleanup that changes behaviour is a regression, not a cleanup. The value here is *structural* correctness with *zero* functional drift; any change in a verdict or a score would erode trust in the exact records we're trying to make trustworthy.

**Independent Test**: For a portfolio and policy where the two copies currently agree, capture the drift verdicts, the enforcement outcome, and the candidate scores before the change; run the same evaluations after; confirm identical results.

**Acceptance Scenarios**:

1. **Given** a portfolio evaluated for allocation drift, **When** the drift check runs after the cleanup (reading the target from its single home), **Then** it returns the same drift verdicts it returned before.
2. **Given** a position that breaches the cap, **When** enforcement runs after the cleanup (reading the cap from its single home), **Then** it flags the same breach it flagged before.
3. **Given** an opportunity candidate scored against the position cap, **When** scoring runs after the cleanup, **Then** it produces the same score it produced before.

---

### User Story 3 - No existing policy value is lost or reset (Priority: P1)

When the duplicate copies are removed, the user's existing values survive. Where a concept lived in two places with different values, a defined reconciliation rule decides the survivor; where one side was empty, the populated side is kept; nothing is fabricated.

**Why this priority**: This is real user data — the policy the user (and agent) rely on. Silently dropping or resetting a limit or a target during a structural change would be a serious, hard-to-notice error.

**Independent Test**: Seed the two records with (a) matching values, (b) differing values, and (c) one-side-empty; run the migration; confirm the surviving single value matches the documented reconciliation rule in every case and that no value is invented where both sides were empty.

**Acceptance Scenarios**:

1. **Given** both records hold the same target allocation, **When** the migration runs, **Then** the single surviving allocation equals that value.
2. **Given** the two records hold **different** single-position caps, **When** the migration runs, **Then** the surviving cap is the stricter (lower) of the two, per the documented rule.
3. **Given** the two records hold **different** target allocations, **When** the migration runs, **Then** the surviving allocation is the intent-record (IPS) value, per the documented rule.
4. **Given** one side is empty and the other populated, **When** the migration runs, **Then** the populated value survives.
5. **Given** both sides are empty, **When** the migration runs, **Then** no value is fabricated (the concept remains unset).

---

### User Story 4 - Agent and API contracts reflect the single home (Priority: P2)

The tools and endpoints the agent and clients use to read/write policy no longer expose the moved fields in the wrong record: the intent-save/read contract drops the position cap; the limits-save/read contract drops the target allocation. The contract change is explicitly flagged so the agent's own configuration (its prompts/persona) can be updated to match.

**Why this priority**: If the contracts still advertise the removed field in the old place, the agent will keep trying to set it there and be silently ignored — reintroducing exactly the ambiguity we removed. It is P2 only because it follows naturally once P1–P3 land; it must ship in the same change.

**Independent Test**: Inspect the intent and limits read/write contracts; confirm each moved field appears only under its single home; confirm attempting to set a moved field under its old contract is rejected or absent rather than silently accepted; confirm the change is documented for the agent-config owner.

**Acceptance Scenarios**:

1. **Given** the updated contracts, **When** the intent record is saved, **Then** the payload has no position-cap field.
2. **Given** the updated contracts, **When** the limits record is saved, **Then** the payload has no target-allocation field.
3. **Given** the cleanup ships, **When** the change log is reviewed, **Then** the moved fields and their new homes are called out for the agent-configuration owner.

### Edge Cases

- A concept currently set in only one of the two records (the common real case) — the populated side simply becomes the single home; no reconciliation needed.
- The two records disagree — the documented reconciliation rule (stricter cap wins; intent-record allocation wins) decides deterministically; the discarded value is recorded in the migration log for auditability, not silently dropped.
- A reader that previously fell back to a default when its local copy was absent — after the move it must apply the same default when the single home is unset, so absent-value behaviour is unchanged.
- The single home is empty at read time — downstream evaluation degrades exactly as it did before when the (old) source was empty (no new errors introduced).
- Re-running the migration — it must be idempotent: once fields are consolidated, a second run makes no further change.

## Requirements *(mandatory)*

### Functional Requirements

**Single source of truth**

- **FR-001**: Target asset-class allocation (with its rebalance/drift band) MUST be stored in exactly one record — the intent (IPS) record — and MUST NOT be duplicated in the limits (Risk) record.
- **FR-002**: The maximum single-position cap MUST be stored in exactly one record — the limits (Risk) record — and MUST NOT be duplicated in the intent (IPS) record.
- **FR-003**: After the cleanup there MUST be no supported path to write a second, competing copy of either concept.

**Behaviour preservation**

- **FR-004**: Allocation-drift evaluation MUST read the target allocation from its single home and MUST produce the same verdicts it produced before the move for equivalent inputs.
- **FR-005**: Position-cap enforcement MUST read the cap from its single home and MUST produce the same breach outcomes it produced before for equivalent inputs.
- **FR-006**: Opportunity-candidate scoring MUST read the position cap from its single home and MUST produce the same scores it produced before for equivalent inputs.
- **FR-007**: Absent-value and default-fallback behaviour for every repointed reader MUST be unchanged relative to before the move.

**Data migration**

- **FR-008**: Before any duplicate field is removed, existing user values MUST be reconciled into the single home so no value is lost or reset.
- **FR-009**: When the two copies of a concept differ, the migration MUST apply a documented, deterministic reconciliation rule: the **stricter (lower) cap** wins for the position cap; the **intent-record value** wins for target allocation.
- **FR-010**: When only one side holds a value, that value MUST survive; when neither side holds a value, the migration MUST NOT fabricate one.
- **FR-011**: Any value discarded by reconciliation MUST be recorded (audit/log), not silently dropped.
- **FR-012**: The migration MUST be idempotent — re-running it after consolidation makes no further change.

**Contracts**

- **FR-013**: The read/write contracts for the intent record MUST no longer expose the position cap, and the read/write contracts for the limits record MUST no longer expose the target allocation.
- **FR-014**: The contract change MUST be documented for the agent-configuration owner so the agent's prompts/persona can be updated (the agent-side update itself is out of scope).

**Scope guard**

- **FR-015**: This feature MUST NOT introduce any new user-facing capability, new module, or behavioural change beyond consolidating the two concepts and repointing their readers.

### Key Entities *(include if feature involves data)*

- **Intent record (IPS)**: The user's strategy/"what I want." After cleanup it is the sole home of target asset-class allocation (with rebalance band) and retains goals, horizon, risk tolerance/capacity, contributions, sell discipline, cooling-off, exclusions, and review cadence. It no longer carries a single-position cap.
- **Limits record (Risk Rule Set)**: The user's enforced "what must never happen." After cleanup it is the sole home of the single-position cap and retains max sleeve weight, min cash buffer, max loss per thesis, max new position, turnover budget, and violation/verdict tracking. It no longer carries a target-allocation copy.
- **Reconciliation outcome (migration-time)**: For each user and each moved concept, the surviving value and the rule that chose it (and any discarded value) — recorded for auditability, not a persisted product entity.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Each of the two concepts (target allocation, position cap) is stored in exactly one record — verifiable by inspecting both records and finding zero duplicate copies.
- **SC-002**: For every portfolio/policy where the two copies previously agreed, allocation-drift verdicts, position-cap enforcement outcomes, and candidate scores are byte-for-byte identical before and after the cleanup (zero behavioural regressions).
- **SC-003**: 100% of existing user policy values are preserved through the migration according to the documented reconciliation rule; zero values are lost, reset, or fabricated.
- **SC-004**: Re-running the migration produces no further changes (idempotent).
- **SC-005**: The intent and limits contracts each expose every moved field under exactly one home; attempting to set a field under its old home has no silent effect.
- **SC-006**: The moved fields and their new homes are documented for the agent-configuration owner in the change record.

## Assumptions

- The existing intent (IPS) and limits (Risk Rule Set) records are the two records involved; no third record holds these concepts.
- In current production data the concepts are, in practice, set in at most one of the two records for the single active user, making reconciliation conflicts unlikely — but the rule must still be implemented for correctness and future users.
- The rebalance band that accompanies the target allocation moves with it to the single (intent) home; a separately-named drift band on the limits side is treated as the same concept and consolidated, not kept as a distinct value.
- Repointed readers may take a read-only cross-module dependency on the other record; the exact read mechanism is a planning/design decision, not a spec-level constraint.
- The agent-side configuration update (prompts/persona) is performed separately by the agent-config owner once this change flags the contract difference.

## Notes

- [DECISION] IPS = intent, Risk = enforced limits: target allocation → IPS (single source); single-position cap → Risk (single source). Rationale: a desired mix is intent; a hard "never exceed" line is an enforced limit, and enforced limits are the surface future delegation guardrails will read.
- [DECISION] Reconciliation rule on conflict: stricter (lower) cap wins; intent-record (IPS) value wins for target allocation. Rationale: never loosen a safety limit during a migration; treat the intent record as authoritative for intent.
- [DECISION] Zero behavioural drift is the success bar: this is a structural cleanup; identical verdicts/scores before and after are required (SC-002), not "improved" behaviour.
- [OUT OF SCOPE] Agent persona/prompt updates: flagged here (FR-014) but performed as agent-config on OpenClaw, not in this repository.
- [OUT OF SCOPE] Delegation guardrails and goal-progress synthesis: separate future features that this cleanup unblocks by giving them a single, unambiguous source to read.
- [OUT OF SCOPE] Any new frontend/UI.
