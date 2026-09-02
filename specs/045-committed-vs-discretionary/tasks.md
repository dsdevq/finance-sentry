# Tasks: Committed vs Discretionary Spending Split (045)

## [US1] Committed/discretionary split computed backend-side

- [x] Add `GetActiveCommitmentMerchantKeysAsync` to `IActiveSubscriptionsReader` (Core) with the match rule documented
- [x] Implement it in `ActiveSubscriptionsReader` — every active row (both kinds), `MerchantNameNormalized`, ordinal set
- [x] Move the detection-key derivation into `MerchantNameNormalizer.NormalizeDetectionKey(merchantName, description)`; `SubscriptionDetectionJob.NormalizeForDetection(TxRow)` delegates to it
- [x] Add `CommittedOutflowUsd` / `DiscretionaryOutflowUsd` to the `MonthlyFlow` record
- [x] Inject `IActiveSubscriptionsReader` into `MoneyFlowStatisticsService`; compute the per-bucket committed sum and convert with `CurrencyConverter.ToUsd`; document the match rule in the interface XML doc
- [x] Update the 5 existing `MoneyFlowStatisticsTests` construction sites for the new ctor param
- [x] Document the split in `docs/money-semantics.md` §5a (mandatory for any PR changing money math)
- [x] Unit tests (`MoneyFlowStatisticsTests`): match/no-match, partition invariant, transfer exclusion, detector-key matching incl. the mobile-top-up and description-fallback paths, cross-currency case that would fail under a native sum, no-active-commitments case
- [x] Unit tests (`ActiveSubscriptionsReaderTests`): active-only repository query, installments included, `GetActiveSubscriptionsAsync` unchanged for Liquidity
- [x] Unit tests for `MerchantNameNormalizer.NormalizeDetectionKey` (mobile top-up, merchant-name/description precedence, both-missing)
- [x] `dotnet build backend/FinanceSentry.sln -c Release` — 0 errors, only the 3 pre-existing Radar CS1587 warnings
- [x] `dotnet test backend/FinanceSentry.sln --filter "Category!=Integration"` — all projects green (555 in `Tests.Unit`, 19 in the touched classes)
- [x] Commit spec artifacts + code

## [US1b] Installment repayments count as committed

- [x] Extract installment recognition from `SubscriptionDetectionJob` into `Application/Services/InstallmentPlanRecognizer` (markers, MCC, prefixes, `ExtractMerchant`, `RoundPlanAmount`, `PlanKey`)
- [x] Job delegates its routing, grouping and stored-key construction to the recognizer; no wrappers left behind
- [x] Add `CommitmentKeyResolver.Resolve(merchantName, description, amount, mcc)` mirroring the job's routing
- [x] `MoneyFlowStatisticsService` matches via the resolver; match-rule XML doc rewritten
- [x] Repoint the 5 `SubscriptionDetectionAlgorithmTests` call sites to `InstallmentPlanRecognizer`
- [x] `CommitmentKeyResolverTests`: plan-key derivation, MCC path, payoff, unrecoverable merchant, amount jitter, concurrent plans, and the drift guard against the real `DetectInstallments`
- [x] `MoneyFlowStatisticsTests`: installment committed, second plan amount not claimed, cross-currency installments, completed plan discretionary
- [x] Update `docs/money-semantics.md` §5a (match rule + known limits)
- [x] `dotnet build backend/FinanceSentry.sln -c Release` — 0 errors, only the 3 pre-existing Radar CS1587 warnings
- [x] `dotnet test backend/FinanceSentry.sln --filter "Category!=Integration"` — green (569 in `Tests.Unit`, +14)
- [x] Commit spec artifacts + code

## [US2] Dashboard stacked spending chart — BLOCKED by the coverage gate

Gate: committed share of outflow over the last 3 complete months must be ≥ 40% (issue #538 AC1).
The filer's ~22% predates US1b, which made installment plans matchable for the first time — the
number must be re-measured before the gate can be called either way. Re-measuring needs
production data and posting it needs GitHub access; no sandbox on this branch has had either.
**Do not start the chart tasks until the gate is re-run and passes.**

- [ ] Run the coverage check on production data via `GET /api/v1/dashboard/aggregated` and post the number as a comment on issue #538
- [ ] Mirror `committedOutflowUsd` / `discretionaryOutflowUsd` on the frontend `MonthlyFlow` interface
- [ ] Split the spending series in `dashboard.computed.ts` over `completeMonths()`
- [ ] Bind `[stacked]` on the spending `cmn-bar-chart`
- [ ] `dashboard.computed.spec.ts` coverage for the split series
- [ ] Playwright spec over the dashboard (app-surface UI ⇒ browser gate)
