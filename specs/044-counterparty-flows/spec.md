# Feature Specification: Counterparty Flows (044)

**Feature Branch**: `goal/fs-429-counterparty-flows-2026-08-31`

**GitHub Issue**: #429

**Created**: 2026-09-02

**Status**: In Progress

## Context

The family clearing-house card (white card 3840) routes ~₴18k/mo rent from
Mom (Людмила Сичова) and ~₴10–15k/mo support payments to her. Both sides are
currently classified as TRANSFER, so rent is invisible income and support
payments are invisible spending. The savings rate reads ~40% against an honest
~25–30%.

**Denys's decision on record**: family support IS expenses (not neutral).

---

## User Scenarios & Testing

### User Story 1 — Counterparty matching engine (Priority: P1)

Counterparty definitions (name + description patterns) exist in the database.
When the classification service processes a batch of transactions, it correctly
identifies which ones belong to a counterparty.

**Why this priority**: Foundation for all flow reclassification.

**Independent Test**: Seed two counterparties with rules; assert the service
matches exactly the expected transaction IDs and misses unrelated ones.

**Acceptance Scenarios**:

1. **Given** counterparty "Людмила Сичова" with description pattern "Людмила Сичова",
   **When** a credit transaction whose Description contains "Людмила Сичова" is processed,
   **Then** it is returned in `MatchedTransactionIds`.

2. **Given** counterparty "Єлизавета Морозова" with pattern "Ліза",
   **When** a debit transaction whose Description contains "Ліза ❤️" is processed,
   **Then** it is matched (case-insensitive substring).

3. **Given** a transaction whose Description contains none of the known patterns,
   **When** classification runs,
   **Then** it is not in `MatchedTransactionIds`.

---

### User Story 2 — Monthly netting + flow reclassification (Priority: P1)

For each month, counterparty transactions are netted: the credit surplus becomes
INCOME, the debit surplus becomes FAMILY_SUPPORT expense, and offsetting gross
movement stays TRANSFER (excluded from flow).

**Why this priority**: Directly fixes the savings-rate lie and the top-categories
distortion.

**Independent Test**: Feed the service a month with ₴18k credit and ₴13k debit
from a single counterparty; assert netIncomeUsd > 0, netExpenseUsd = 0 (credits
dominate). Flip the amounts; assert the reverse.

**Acceptance Scenarios**:

1. **Given** a month with 18000 UAH credit and 13000 UAH debit from Mom,
   **When** netting is applied,
   **Then** netIncomeUsd ≈ ToUsd(5000, "UAH") and netExpenseUsd = 0.

2. **Given** a month with 0 UAH credit and 10000 UAH debit to Mom,
   **When** netting is applied,
   **Then** netIncomeUsd = 0 and netExpenseUsd ≈ ToUsd(10000, "UAH").

3. **Given** two months in the window with different netting outcomes,
   **When** `GetMonthlyFlowAsync` is called,
   **Then** each month's inflow/outflow reflects the counterparty reclassification.

4. **Given** the same data re-run over a fixed month,
   **When** classification is called again,
   **Then** the result is identical (deterministic).

---

### User Story 3 — Dashboard four-bucket split (Priority: P2)

The dashboard renders: spent / supported family / invested / kept.

**Why this priority**: Frontend visualisation of the reclassification. Requires US1+US2.

**Independent Test**: Load the dashboard; confirm four labelled buckets appear
and the "supported family" bucket matches the backend's FAMILY_SUPPORT total.

**Acceptance Scenarios**:

1. **Given** a month with family support expense,
   **When** the dashboard loads,
   **Then** "Supported family" shows the net expense (not 0, not mixed with regular spend).

2. **Given** no counterparty transactions in the window,
   **When** the dashboard loads,
   **Then** "Supported family" shows 0 / is hidden.

3. **Given** a month with money routed to an investment venue,
   **When** the dashboard loads,
   **Then** "Invested" shows that amount, "Spent" does not include it, and "Kept" is the
   surplus that remains after it (`inflow − outflow − invested`).

---

### Edge Cases

- What if a counterparty has credits = debits in a month? Net both to 0 — all is TRANSFER.
- What if a counterparty matches no transactions in the window? Omit from results.
- What if a transaction matches multiple counterparties? First-match wins (deterministic).
- Unknown account currency in `CurrencyConverter.ToUsd`? Falls back to 1:1 per existing convention.

---

## Requirements

### Functional Requirements

- **FR-001**: System MUST store counterparty definitions with one or more match rules per counterparty.
- **FR-002**: A match rule MUST support two match types: `description_contains` and `merchant_name_contains` (case-insensitive substring).
- **FR-003**: System MUST ship default counterparties (Людмила Сичова, Єлизавета Морозова) applying to all users (UserId = Guid.Empty sentinel).
- **FR-004**: Monthly netting MUST be applied per counterparty per calendar month.
- **FR-005**: Net credit balance MUST feed into monthlyFlow inflow (INCOME); net debit balance MUST feed into outflow (FAMILY_SUPPORT).
- **FR-006**: `GetMonthlyFlowAsync` and `GetTopCategoriesAsync` MUST read the same classification — no separate re-derivation.
- **FR-007**: FAMILY_SUPPORT MUST appear as a distinct category in top-categories output when net expense > 0.
- **FR-008**: Counterparty-matched transactions MUST be excluded from the normal transfer-detection pass to avoid double-exclusion.
- **FR-009**: Classification output MUST be deterministic for a fixed input set.
- **FR-010**: The classification MUST be computed ONCE per request and passed to the money-flow
  and top-categories readers; neither may derive its own.
- **FR-011**: A counterparty's FlowRole MUST decide where its netted movement lands.
  `family_support` net expense counts as outflow (and so lowers the savings rate);
  `investment` net expense MUST NOT count as outflow or as spend — it is reported separately
  and deducted from the kept surplus.

### Key Entities

- **Counterparty**: display name, UserId (Guid.Empty = system default), FlowRole
  ("family_support" | "investment").
- **CounterpartyRule**: foreign key to Counterparty, MatchType ("description_contains" | "merchant_name_contains"), Pattern string.

---

## Success Criteria

- **SC-001**: `dotnet test --filter Category!=Integration` passes with zero failures.
- **SC-002**: `dotnet build FinanceSentry.sln` produces zero warnings.
- **SC-003**: Unit tests cover: match/no-match, netting (credits dominate, debits dominate, equal), multi-month, multi-counterparty, no-match month.
- **SC-004**: The reported savings rate shifts from transfer-blind ~40% toward the honest ~25–30% on Denys's real data.

---

## Assumptions

- This is a single-user system; system-default counterparties (UserId = Guid.Empty) apply to all users.
- Dashboard four-bucket split (US3) is delivered in follow-on slices; the first slice ships the backend engine only.
- Investment routing is recognised by the same counterparty engine (an `investment`-role system
  counterparty), not by a second detection mechanism.
- No management API for counterparties in this slice — seeded via data migration.
- The white-card 3840 transactions arrive from Monobank as type "credit" / "debit" with counterparty name or description text.
