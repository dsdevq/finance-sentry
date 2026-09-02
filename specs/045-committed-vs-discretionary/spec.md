# Feature Specification: Split the Spending Trend into Committed vs Discretionary

**Feature Branch**: `045-committed-vs-discretionary`

**Created**: 2026-09-02

**Status**: US1 implemented; US2 blocked on the coverage gate (see [US2])

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

### [US2] Dashboard stacked spending chart (P2 — GATED, not implemented)

The dashboard spending chart renders the split as a stacked bar over `completeMonths()`, reusing the
existing `BarSeries` contract; the in-progress month stays off the chart, same as #537.

**This story is gated by a coverage check and is deliberately NOT implemented in this feature.**

Gate (issue #538 AC1): compute committed spend as a share of total outflow over the last three
complete months on the same transfer-excluded basis the dashboard uses. **If coverage is under 40%,
do not ship the chart.** A split that buckets three-quarters of spending as "discretionary" is worse
than no split — it labels a gap in the detector as a finding about the user's spending.

**Gate status (2026-09-02): FAILS.** The issue filer measured ~22% on production data (13 active
subscriptions ≈ $708/mo against ~$3.2k/mo of real outflow). US1's structural analysis corroborates
why coverage is low rather than mismeasured — see "Known coverage limits" below. Widening the
definition of "committed" is a separate ticket, not a silent workaround here.

---

## Known coverage limits (why ~22%, structurally)

Matching is on the detector's own normalized merchant key, so what the detector cannot key, the
split cannot claim:

- **Installment plans keyed synthetically.** `SubscriptionDetectionJob.DetectInstallments` stores
  `MerchantNameNormalized` as `installment:{merchant}:{roundedAmount}`. No transaction's normalized
  merchant key ever takes that form, so розстрочка repayments fall into discretionary. (Installments
  detected via the masked-PAN path in `DetectSubscriptions` *do* carry a real merchant key and do
  match.)
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
