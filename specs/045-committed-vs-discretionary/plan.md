# Plan: Committed vs Discretionary Spending Split (045)

## Architecture decisions

| Decision | Choice | Why |
|---|---|---|
| Where the split is computed | `MoneyFlowStatisticsService.GetMonthlyFlowAsync` | Issue #538 AC2 puts it in the monthly-flow reader; that is the only place where account currency, transfer exclusion and the month window are all already in scope |
| Cross-module read | New method on the existing `IActiveSubscriptionsReader` port (Core) | The port already exists and is already implemented in Subscriptions + registered in `SubscriptionsModule`; no new contract, no new adapter, no DI wiring |
| Port shape | `GetActiveCommitmentMerchantKeysAsync → IReadOnlySet<string>` | The matcher needs a set-membership test, nothing else. Returning `ActiveSubscriptionSummary` rows would drag amount/cadence/date fields the split never reads, and extending that record would ripple through Liquidity + ~10 test construction sites for no gain |
| Why a *new* method rather than reusing `GetActiveSubscriptionsAsync` | That method filters `Kind == Subscription`, dropping installments, and returns display names | Committed spend must consider every active `DetectedSubscription` regardless of kind, and must match on the *normalized* key. Widening the existing method would silently change Liquidity's cash-flow projection — out of scope |
| Match key | `MerchantNameNormalizer.NormalizeDetectionKey(merchantName, description)` on both sides | The stored `DetectedSubscription.MerchantNameNormalized` is produced by exactly this function during detection. Any other normalization would systematically under-match |
| Where the key function lives | Moved from `SubscriptionDetectionJob.NormalizeForDetection(TxRow)` into `MerchantNameNormalizer` | Two callers now need it; the job keeps a one-line `TxRow` overload delegating to it. Satisfies AC3's "reuse `MerchantNameNormalizer`" and keeps one definition of the key |
| Currency | Committed native sum per (month, currency) bucket → `CurrencyConverter.ToUsd` at the same boundary as `OutflowUsd` | backend-rules.md Currency/Money rule: convert once at the reader boundary where account currency is in scope. Subscriptions span UAH/EUR/USD so a native cross-account sum is wrong by construction |
| Discretionary derivation | `DiscretionaryOutflowUsd = OutflowUsd − CommittedOutflowUsd` | Guarantees the partition invariant exactly (no rounding drift between two independent conversions of complementary subsets) |
| New `MonthlyFlow` fields | `CommittedOutflowUsd`, `DiscretionaryOutflowUsd` only — no native twins | AC2 names exactly these two. Native committed/discretionary amounts have no consumer; adding them would be dead surface |
| Reader is a required dependency | Plain ctor param, not the optional `?…= null` shape `DashboardQueryService` uses for its crypto/brokerage readers | Those modules can genuinely be absent from a composition; Subscriptions is registered in every one, so an optional reader would be a null branch that can never be taken. Cost is updating the 5 existing `MoneyFlowStatisticsTests` call sites |

## Story-slice surfaces

### [US1] Backend split — files touched / created

- `FinanceSentry.Core/Interfaces/IActiveSubscriptionsReader.cs` — add `GetActiveCommitmentMerchantKeysAsync`
- `FinanceSentry.Modules.Subscriptions/Application/Services/ActiveSubscriptionsReader.cs` — implement it (all active rows, both kinds, `MerchantNameNormalized`, ordinal set)
- `FinanceSentry.Modules.BankSync/Application/Services/MerchantNameNormalizer.cs` — gains `NormalizeDetectionKey(merchantName, description)` + the mobile-top-up regex moved in
- `FinanceSentry.Modules.BankSync/Infrastructure/Jobs/SubscriptionDetectionJob.cs` — `NormalizeForDetection(TxRow)` delegates to the normalizer
- `FinanceSentry.Modules.BankSync/Application/Services/MoneyFlowStatisticsService.cs` — 2 new `MonthlyFlow` fields, optional reader dependency, per-bucket committed sum + conversion, XML doc of the match rule
- `tests/FinanceSentry.Tests.Unit/BankSync/Application/MoneyFlowStatisticsTests.cs` — existing 5 call sites updated; new split tests incl. the cross-currency case

Constraint discovered while planning: `MonthlyFlow` is a positional record consumed by
`DashboardQueryService` → `DashboardController` → the Angular `MonthlyFlow` interface. Appending
fields is additive for JSON consumers, so the frontend model can stay untouched until US2.

### [US2] Stacked chart — surface (NOT built; recorded for the next session)

- `frontend/src/app/modules/bank-sync/models/dashboard/dashboard.model.ts` — mirror the two fields
- `frontend/src/app/modules/bank-sync/store/dashboard/dashboard.computed.ts` — `incomeVsSpendingBars`
  gains a stacked committed/discretionary spending pair over `completeMonths()`
- `frontend/src/app/modules/bank-sync/pages/dashboard/dashboard.component.ts` — `[stacked]` binding
  on the spending `cmn-bar-chart` (precedent: the net-worth `cmn-area-chart` at line ~195)
- `dashboard.computed.spec.ts` + a Playwright spec (app-surface UI ⇒ browser gate applies)

**Do not start US2 until the coverage gate passes.** See spec.md [US2].
