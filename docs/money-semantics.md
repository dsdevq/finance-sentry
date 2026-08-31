# Money Semantics — how every number is calculated

Source of truth for Finance Sentry's money math. **Any PR that changes one of these
behaviours must update this document in the same diff.** File references point at the
implementing code; when they disagree, the code is the bug or this doc is stale — fix one.

Last verified: 2026-08-31 (PR #531).

---

## 1. Balance semantics

`BankAccount.CurrentBalance` means different things by account type:

| Account type | CurrentBalance means | Sign |
|---|---|---|
| `checking` / `savings` | Own funds | positive = money you have |
| `credit` | Amount owed | positive = debt |

`BankAccount.CreditLimit` holds the credit line when the provider exposes one, null otherwise.

### Per provider

- **Monobank** (`MonobankHttpClient`, `MonobankAdapter`): the client-info `balance` field
  *includes* the credit limit on credit-enabled cards (`balance = own funds + creditLimit`).
  `MonobankHttpClient.ToStoredBalance` converts: accounts with `creditLimit > 0` store
  `creditLimit − balance` (amount owed) and are typed `credit` regardless of product name
  (a black/platinum card with a credit line is a liability account); accounts without a
  limit store the raw balance. Product-name mapping (`yellow` → credit, etc.) applies only
  when there is no credit line.
- **TrueLayer accounts** (`TrueLayerHttpClient`, `/data/v1/accounts/{id}/balance`): store
  `current` as-is (own funds; can be negative for an overdraft). `available` is fetched but
  not persisted.
- **TrueLayer cards** (`/data/v1/cards/{id}/balance`): `current` is already the amount
  owed — stored as-is with type `credit`; `credit_limit` → `CreditLimit`. Cards are a
  separate endpoint family: they never appear under `/data/v1/accounts`, are discovered at
  connect/reconnect (`FinalizeTrueLayerConnectCommand`), and are routed by
  `ProductType == "card"` (`TrueLayerAdapter.CardProductType`).

### Failure behaviour

A failed balance fetch **keeps the prior stored balance** — never zeroes it. Both the
Monobank path (rate-limit) and the TrueLayer path (`ScheduledSyncService`) follow this;
zeroing recorded phantom net-worth drops.

## 2. Liability sign convention

`FinanceSentry.Core.Utils.AccountBalanceMath` is the single authority:

- `IsLiability(accountType)` — true only for `"credit"`.
- `SignedForNetTotal(accountType, amount)` — negates credit balances.

**Aggregates** (net worth, currency totals, banking sleeve, wealth institution/card-group
totals, liquidity projections) always sum `SignedForNetTotal(...)`. **Per-account display**
keeps the raw positive value ("you owe X"), matching how banks present credit cards.

## 3. Currency conversion

- Convert **once, at the reader/query boundary**, via `CurrencyConverter.ToUsd(amount,
  currency)` — never sum native amounts across accounts (UAH + EUR is not USD).
- Every DTO crossing an aggregation boundary carries a `…Usd` field; aggregations sum only
  that field.
- Rate table: process-wide, refreshed daily at midnight UTC by the FX job
  (`Program.cs`), seeded with hardcoded fallbacks until the first refresh.
- **Unknown currency falls back 1:1.** Use `CurrencyConverter.IsKnown` to flag a total as
  approximate rather than trusting the silent fallback.

## 4. Transaction lifecycle (pending / posted / dedup)

- Dedup hash: `HMAC-SHA256(accountId|amount|date|description)`
  (`TransactionDeduplicationService`). Pending rows hash on `TransactionDate`; posted rows
  on `PostedDate`.
- **Settle-in-place**: if a posted candidate hashes identically to a stored *pending* row
  (Monobank holds keep their date when they clear), the stored row is flipped to posted
  (`ScheduledSyncService.PersistAndReconcileAsync`).
- **PendingReconciler**: a pending row whose posted twin exists under a *different* hash
  (date moved — the TrueLayer case) is retired (soft-deleted, `IsActive = false`).
- Net effect: a real purchase exists as exactly one active row; it may be `IsPending` for a
  few days, then becomes posted either in place or via retire-and-replace.

## 5. Monthly inflow / outflow ("Spending this month", "Monthly Outflow")

`MoneyFlowStatisticsService.GetMonthlyFlowAsync`:

- **Fetch window**: rolling — `UtcNow.AddMonths(-6)`. **Bucketing**: calendar UTC month on
  `PostedDate ?? TransactionDate`. The current bucket is month-to-date; the oldest bucket
  is a partial month (truncated by the rolling fetch edge).
- **Included**: active transactions, **pending included** — a card hold is committed
  spending (excluding it made the month's outflow a fraction of reality).
- **Excluded**: internal transfers, two ways — (a) pair-matched via
  `TransferDetectionService` (cross-currency aware, posted rows only), (b) category-based
  `TRANSFER_IN` / `TRANSFER_OUT`. Note: credit-card repayments are transfers (moving money
  onto your own card), so they are rightly excluded — the *spending* is counted on the card
  itself.
- Outflow = sum of `debit` amounts, inflow = sum of `credit` amounts, per currency, plus
  USD-converted fields. Transactions on deactivated accounts resolve to currency
  `"UNKNOWN"` (converted 1:1).
- **Not cached** — recomputed per `/dashboard/aggregated` request. The dashboard polls
  every 5 minutes; the transaction-ledger stat card fetches once at page load.

## 6. Top spending categories

`MerchantCategoryStatisticsService`: same filters as monthly flow (active, pending
included, transfers excluded, debits only, USD-converted), but a **flat rolling 6-month
window** — not month-bucketed. Displayed as "Top Spending Categories (6M)".

## 7. Savings rate

Frontend-only (`dashboard.computed.ts`), derived from the monthly-flow buckets:
**completed calendar months with inflow only** — the in-progress month is deliberately
excluded (month-to-date reads absurdly negative before payday).

## 8. Net worth

- **Headline stat** ("Net Worth" card): computed **live** per request —
  banking (signed per §2, USD per §3) + crypto holdings + brokerage holdings
  (`DashboardQueryService`).
- **History chart**: daily `net_worth_snapshots` rows, one per (user, UTC date),
  **upserted** — refreshed by every successful account sync (`FirstSyncSnapshotTrigger`)
  and by the 01:00 UTC Hangfire backstop job (`NetWorthSnapshotJob`). The newest point
  therefore tracks the live position through the day. Headline and last chart point can
  still differ by minutes, not by a day.
- **Carry-forward**: a sleeve (banking/brokerage/crypto) whose feed hasn't synced within
  36h, or that drops to exactly $0 from a positive value, is treated as a failed sync — the
  previous day's value is carried forward and the sleeve is listed in `StaleSleeves`
  (`NetWorthSnapshotService`). The baseline is the latest snapshot *strictly before* the
  snapshot date, so a same-day refresh never carries forward from itself.
- **Backfill** (`NetWorthSnapshotBackfillService`, on boot): fills missed days with
  *current* balances — a downtime gap renders as a flat line, not real history.

## 9. Known approximations (accepted)

- Everything is UTC; no user-timezone normalization of transaction dates or month edges.
- Unknown currencies convert 1:1 (§3).
- A pending transaction and its posted twin can both be active between the twin's arrival
  and the account's next sync — a transient double-count window of one sync cycle.
- Backfilled snapshot days are not historical truth (§8).
- Transfer pair-detection only considers posted rows; a pending half of a transfer is
  excluded only if its category says so.
