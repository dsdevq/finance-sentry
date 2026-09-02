# Feature Specification: Split the Spending Trend into Committed vs Discretionary

**Feature Branch**: `045-committed-vs-discretionary`

**Created**: 2026-09-02

**Status**: US1 + US1b implemented; US2 blocked on the coverage gate (see [US2])

**GitHub Issue**: #538

## Context

The dashboard already charts `incomeVsSpendingBars` and `savingsRateBars` (#537). Those two charts
plot the same relationship twice: savings rate is *defined* as `1 − outflow/inflow`, so their
correlation is an identity, not a finding. The informative version splits outflow into **committed**
and **discretionary** so a bad month has a cause.

**Definition (decided 2026-08-31):** committed = outflow matching an active `DetectedSubscription`;
everything else is discretionary. The Subscriptions module is the source of truth — no new category
taxonomy is invented, and `FinanceSentry.Core/Interfaces/IActiveSubscriptionsReader.cs` is the
existing cross-module port to read it through (Liquidity already consumes it).

Baseline over Apr–Aug 2026, transfers excluded the way `MoneyFlowStatisticsService` excludes them
(`!CategoryKeys.IsTransfer` plus `ITransferDetectionService` pair detection): inflow $4.9k–$6.0k/mo,
outflow $2.6k–$3.4k/mo, savings rate 31–52%.

---

## User Scenarios

### [US1] Committed/discretionary split computed backend-side (P1)

`MoneyFlowStatisticsService` classifies every outflow transaction in each month/currency bucket as
committed (its detection key matches an active detected subscription) or discretionary, and exposes
both as USD figures on the existing `MonthlyFlow` row.

This slice is also the **instrument for the US2 coverage gate**: once shipped, committed share of
outflow is read straight off `GET /api/v1/dashboard/aggregated` — no bespoke diagnostic script.

**Acceptance Scenarios**:

1. **Given** an outflow transaction whose normalized merchant key matches an active detected
   subscription, **When** monthly flow is computed, **Then** its amount lands in
   `CommittedOutflowUsd` and not in `DiscretionaryOutflowUsd`.
2. **Given** an outflow transaction with no matching active subscription, **When** monthly flow is
   computed, **Then** its amount lands in `DiscretionaryOutflowUsd`.
3. **Given** a subscription whose status is `dismissed`, `potentially_cancelled` or `completed`,
   **When** monthly flow is computed, **Then** its charges are discretionary — only `active` counts.
4. **Given** committed spending on a UAH account and on a EUR account in the same month, **When**
   monthly flow is computed, **Then** each row's `CommittedOutflowUsd` is converted with
   `CurrencyConverter.ToUsd` at the reader boundary, so summing the USD fields across rows is
   correct where summing native amounts would not be.
5. **Given** any month/currency bucket, **When** monthly flow is computed, **Then**
   `CommittedOutflowUsd + DiscretionaryOutflowUsd == OutflowUsd` (the split partitions outflow; it
   never adds or drops spend).
6. **Given** an internal transfer pair or a `TRANSFER_IN`/`TRANSFER_OUT` category, **When** monthly
   flow is computed, **Then** it is excluded from all three outflow figures alike — the split
   inherits the existing transfer exclusion rather than re-deriving it.

### [US1b] Installment repayments count as committed (P1)

US1 shipped a matcher that keyed every transaction as a *merchant*. The detector, however,
stores installment (розстрочка) plans under a synthetic `installment:{merchant}:{roundedAmount}`
key that no merchant key can ever take, so every розстрочка repayment — the most committed spend
a user has — was booked as discretionary. That is a defect in US1's match rule, not a widening of
the agreed definition: these rows already are active `DetectedSubscription`s.

`CommitmentKeyResolver` now mirrors `SubscriptionDetectionJob`'s own routing — installment
repayments resolve to their plan key, everything else to its merchant key — so both kinds of
stored commitment are reachable.

**Acceptance Scenarios**:

1. **Given** an outflow whose description marks it a розстрочка repayment and whose
   (merchant, rounded amount) matches an active installment plan, **When** monthly flow is
   computed, **Then** its amount lands in `CommittedOutflowUsd`.
2. **Given** two concurrent plans at one shop and an active row for only one of them, **When**
   monthly flow is computed, **Then** only the repayment at that plan's amount is committed —
   plan identity includes the rounded amount, so merchant-level matching may not claim both.
3. **Given** cent-level jitter between two months of the same plan (₴6,499.84 / ₴6,499.85),
   **When** each is keyed, **Then** both resolve to one plan key.
4. **Given** installment repayments on a UAH and a EUR account, **When** monthly flow is
   computed, **Then** each is converted with `CurrencyConverter.ToUsd` in its own bucket.
5. **Given** a plan the detector marked completed, **When** monthly flow is computed, **Then**
   its repayments are discretionary — the "active only" rule is unchanged.
6. **Given** any batch of repayments, **When** `DetectInstallments` stores their plans, **Then**
   every stored key is reproducible by `CommitmentKeyResolver.Resolve` from the transaction
   alone (the drift guard).

### [US2] Dashboard stacked spending chart (P2 — GATED, not implemented)

The dashboard spending chart renders the split as a stacked bar over `completeMonths()`, reusing the
existing `BarSeries` contract; the in-progress month stays off the chart, same as #537.

**This story is gated by a coverage check and is deliberately NOT implemented in this feature.**

Gate (issue #538 AC1): compute committed spend as a share of total outflow over the last three
complete months on the same transfer-excluded basis the dashboard uses. **If coverage is under 40%,
do not ship the chart.** A split that buckets three-quarters of spending as "discretionary" is worse
than no split — it labels a gap in the detector as a finding about the user's spending.

**Gate status (2026-09-02): UNMEASURED SINCE US1b.** The issue filer measured ~22% on production
data (13 active subscriptions ≈ $708/mo against ~$3.2k/mo of real outflow) — but that was against
US1's matcher, which could not match installment plans at all (US1b). The gate must be re-run
before it can be called either way, and re-running it needs production data: read
`committedOutflowUsd` / `outflowUsd` off `GET /api/v1/dashboard/aggregated` for the last three
complete months. No agent sandbox on this branch has had database or GitHub access, so the
measurement and the issue comment are still outstanding. Widening the *definition* of "committed"
beyond `DetectedSubscription` remains a separate ticket.

---

## Known coverage limits (why ~22%, structurally)

Matching is on the detector's own normalized merchant key, so what the detector cannot key, the
split cannot claim:

- ~~**Installment plans keyed synthetically.**~~ **Fixed in US1b** — `CommitmentKeyResolver`
  now derives the same `installment:{merchant}:{roundedAmount}` key from a transaction that
  `DetectInstallments` stores, so розстрочка repayments are committed. This was the one limit
  on the list that was a defect rather than a property of the definition; the rest stand.
- **Full early payoffs are not plan-keyed.** A "Повне погашення" completes its plan, and a
  completed plan is not `active`, so the payoff itself reads as discretionary — a lumpy one-off
  in the wrong bucket. Consistent with the agreed "only active counts" rule; noted as a wart.
- **Mortgage/loan repayments are transfers.** A recurring transfer to a masked card number is
  excluded from outflow altogether by the transfer filter, so it is neither committed nor
  discretionary — it is not in the denominator either.
- **Ordinary outflow has no recurring signature.** Groceries, fuel, restaurants and one-off
  purchases are the bulk of spend and by construction never become a `DetectedSubscription`.

These are properties of the agreed definition, not defects introduced here. They are recorded so the
follow-up ticket that widens "committed" starts from evidence.

---

## Out of scope

- Extending or reusing `Liquidity/Application/Services/CashFlowProjectionService` — it answers a
  different question (30-day per-account shortfall from upcoming charges).
- Changing `IActiveSubscriptionsReader.GetActiveSubscriptionsAsync` semantics or Liquidity's
  behaviour.
- Widening the definition of "committed" beyond `DetectedSubscription` (rent categories, a manual
  commitment list, MCC-based rules).
- Any new category taxonomy.

---

## Verification

- `dotnet build backend/FinanceSentry.sln` — zero new warnings
- `dotnet test backend/FinanceSentry.sln --filter "Category!=Integration"`
- No frontend change in this feature, so no ESLint / `ng test` / Playwright surface is touched.
