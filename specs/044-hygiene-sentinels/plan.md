# 044 — Hygiene Sentinels: Implementation Plan

## Architecture

Each detector is a Hangfire job in `FinanceSentry.Modules.BankSync/Infrastructure/Jobs/`. They consume:
- `IAlertGeneratorService` (Core) — alert emission + dedup
- BankSync's own `BankSyncDbContext` (transactions / accounts) for US2, US3, US4
- A new Core port `ISubscriptionHygieneSummaryReader` (US1) — bridging to Subscriptions module

Thresholds are read from `IConfiguration` under the `HygieneSentinels:*` keys. Each job is registered in `BankSyncModule.JobRegistrar` as a daily recurring job.

New alert types (`PriceHike`, `DuplicateCharge`, `CategorySpike`, `FxSpread`) are added to `AlertType`, `IAlertGeneratorService`, `AlertGeneratorService`, `CompanionEventKind`, and `MaterialityPolicy` in one PR each.

## Cross-module boundary (US1)

`PriceHikeDetectionJob` lives in BankSync which only references Core. To access `DetectedSubscription` data without a module-to-module reference:
- New port in Core: `ISubscriptionHygieneSummaryReader` → `SubscriptionHygieneSummary` record
- Adapter in Subscriptions module: `SubscriptionHygieneSummaryReader` queries `SubscriptionsDbContext.DetectedSubscriptions` directly (efficient single cross-user query, bypasses per-user repo method)
- Registered in `SubscriptionsModule` alongside existing `IActiveSubscriptionsReader`

This follows the same pattern as `IActiveSubscriptionsReader` / `ActiveSubscriptionsReader`.

## The hike baseline (US1)

Reading recurrence from `detected_subscriptions` rather than re-deriving it means the sentinel
can only see what detection chose to record — and detection recorded no evidence a price had
ever changed. `SubscriptionDetectionJob` keeps only the amount cluster holding the most recent
charge, and the cluster tolerance (15%) equals the sentinel threshold (15%), so the two gates
excluded each other: a hike large enough to fire split the new price into a cluster of one,
which failed the three-occurrence gate and dropped the subscription off the list entirely; a
hike small enough to stay clustered was diluted by its own charge in the average it was being
compared against (a 15% step over two prior charges measures 9.5%). No charge series could
produce an alert.

Decision: detection reports the displaced cluster's price as `PreviousAmount` while the new
price is still new — that is, until the current cluster has `MinOccurrences` charges of its
own — and the sentinel measures against `SubscriptionHygieneSummary.HikeBaseline`
(`PreviousAmount ?? AverageAmount`). The displaced cluster also counts toward the occurrence
gate, which is what keeps a repriced subscription on the list at all.

Why not simply lower the threshold: it would only ever catch hikes small enough to survive
clustering, leaving the large ones — the ones worth alerting on — permanently invisible.

A displaced cluster is a repricing, and not two different things, only when all of:
- the merchant billed exactly two prices — a third cluster is an outlier or a third plan, and
  taking the nearest of several would let one stray charge shadow the price really replaced;
- those two form a clean chronological step — concurrent plans interleave in time;
- the step is within `MaxPriceStepRatio` (2×, so Claude Pro €22 → Max €110 stays a plan switch);
- the old price has at least `MinBaselineCharges` (2) charges — a prorated or promotional first
  month is one charge with zero variance, and would otherwise alert on the merchant's own
  onboarding;
- the old price was itself stable under the CV gate.

Each guard fails closed: no baseline, and the row behaves exactly as it did before this change.

Tying the baseline's lifetime to `MinOccurrences` rather than to a time window bounds
re-alerting to one further monthly cycle past the 30-day alert silence — the baseline clears on
the third charge at the new price.

Known limits, both consequences of reading detection's output rather than the raw series:
- `AmountClusterTolerance` sets the floor on what a hike can be. A step under 15% never leaves
  its cluster, so it has no baseline and is still measured against a diluted average; lowering
  `HygieneSentinels:PriceHikeThreshold` below 15% buys less than it appears to.
- An annual subscription cannot produce a baseline: two prior charges at the old price do not
  fit the 13-month detection lookback. Annual repricing stays invisible to this sentinel.
- `OccurrenceCount` counts the displaced charges too, so it rises while a step is live and
  returns to the current-cluster count once the price settles.

## [US1] Price hike — files touched
- NEW `backend/src/FinanceSentry.Core/Interfaces/ISubscriptionHygieneSummaryReader.cs`
- NEW `backend/src/FinanceSentry.Modules.Subscriptions/Application/Services/SubscriptionHygieneSummaryReader.cs`
- EDIT `backend/src/FinanceSentry.Modules.Subscriptions/SubscriptionsModule.cs` — register adapter
- EDIT `backend/src/FinanceSentry.Modules.Alerts/Domain/AlertType.cs` — add `PriceHike`
- EDIT `backend/src/FinanceSentry.Core/Interfaces/IAlertGeneratorService.cs` — add method
- EDIT `backend/src/FinanceSentry.Modules.Alerts/Application/Services/AlertGeneratorService.cs` — implement
- EDIT `backend/src/FinanceSentry.Modules.Companion/Domain/CompanionEventKind.cs` — add `PriceHike`
- EDIT `backend/src/FinanceSentry.Modules.Companion/Application/Services/MaterialityPolicy.cs` — map it
- NEW `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Jobs/PriceHikeDetectionJob.cs`
- EDIT `backend/src/FinanceSentry.Modules.BankSync/BankSyncModule.cs` — register + schedule daily
- NEW `backend/tests/FinanceSentry.Tests.Unit/BankSync/Infrastructure/PriceHikeDetectionJobTests.cs`

### Baseline follow-up (the hike a merchant actually charges)
- EDIT `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Jobs/SubscriptionDetectionJob.cs` — `SplitAtPriceStep` / `PriceSeries`
- EDIT `backend/src/FinanceSentry.Core/Interfaces/ISubscriptionDetectionResultService.cs` — `DetectedSubscriptionData.PreviousAmount`
- EDIT `backend/src/FinanceSentry.Modules.Subscriptions/Domain/DetectedSubscription.cs` + `SubscriptionsDbContext.cs` — persist it
- NEW `backend/src/FinanceSentry.Modules.Subscriptions/Migrations/20260906000000_M006_AddPreviousAmount.cs` (+ Designer, snapshot)
- EDIT `ISubscriptionHygieneSummaryReader.cs` — `PreviousAmount` + `HikeBaseline`; reader projects it
- NEW `backend/tests/FinanceSentry.Tests.Unit/BankSync/Infrastructure/PriceHikeSentinelPipelineTests.cs` — detect → persist → read → alert

## [US2] Duplicate charge — files touched
- EDIT `backend/src/FinanceSentry.Modules.Alerts/Domain/AlertType.cs` — add `DuplicateCharge`
- EDIT `backend/src/FinanceSentry.Core/Interfaces/IAlertGeneratorService.cs` — add method
- EDIT `backend/src/FinanceSentry.Modules.Alerts/Application/Services/AlertGeneratorService.cs` — implement
- EDIT `backend/src/FinanceSentry.Modules.Companion/Domain/CompanionEventKind.cs` — add `DuplicateCharge`
- EDIT `backend/src/FinanceSentry.Modules.Companion/Application/Services/MaterialityPolicy.cs` — map it
- NEW `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Jobs/DuplicateChargeDetectionJob.cs`
- EDIT `backend/src/FinanceSentry.Modules.BankSync/BankSyncModule.cs` — register + schedule daily
- NEW `backend/tests/FinanceSentry.Tests.Unit/BankSync/Infrastructure/DuplicateChargeDetectionJobTests.cs`

## [US3] Category spike — files touched
- EDIT `backend/src/FinanceSentry.Modules.Alerts/Domain/AlertType.cs` — add `CategorySpike`
- EDIT `backend/src/FinanceSentry.Core/Interfaces/IAlertGeneratorService.cs` — add method
- EDIT `backend/src/FinanceSentry.Modules.Alerts/Application/Services/AlertGeneratorService.cs` — implement
- EDIT `backend/src/FinanceSentry.Modules.Companion/Domain/CompanionEventKind.cs` — add `CategorySpike`
- EDIT `backend/src/FinanceSentry.Modules.Companion/Application/Services/MaterialityPolicy.cs` — map it
- NEW `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Jobs/CategorySpikeDetectionJob.cs`
- EDIT `backend/src/FinanceSentry.Modules.BankSync/BankSyncModule.cs` — register + schedule daily
- NEW `backend/tests/FinanceSentry.Tests.Unit/BankSync/Infrastructure/CategorySpikeDetectionJobTests.cs`

## [US4] FX spread — files touched
- EDIT `backend/src/FinanceSentry.Modules.Alerts/Domain/AlertType.cs` — add `FxSpread`
- EDIT `backend/src/FinanceSentry.Core/Interfaces/IAlertGeneratorService.cs` — add method
- EDIT `backend/src/FinanceSentry.Modules.Alerts/Application/Services/AlertGeneratorService.cs` — implement
- EDIT `backend/src/FinanceSentry.Modules.Companion/Domain/CompanionEventKind.cs` — add `FxSpread`
- EDIT `backend/src/FinanceSentry.Modules.Companion/Application/Services/MaterialityPolicy.cs` — map it
- NEW `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Jobs/FxSpreadDetectionJob.cs`
- EDIT `backend/src/FinanceSentry.Modules.BankSync/BankSyncModule.cs` — register + schedule daily
- NEW `backend/tests/FinanceSentry.Tests.Unit/BankSync/Infrastructure/FxSpreadDetectionJobTests.cs`

## Constraints
- DetectedSubscription.UserId is `string`; BankAccount.UserId is `Guid` — convert at the adapter boundary with `Guid.Parse(s.UserId)`
- All amounts compared cross-currency must go through `CurrencyConverter.ToUsd` before comparison
- Spend is selected by direction, never by sign: adapters persist a positive `Transaction.Amount` with `TransactionType` = `"debit"`/`"credit"`, and every persist path runs `Transaction.ValidateInvariants`, which rejects a negative amount. The 044 sentinels that filter for outflows (`DuplicateChargeDetectionJob`, `CategorySpikeDetectionJob`) use `(t.Amount < 0 || t.TransactionType == "debit")` and exclude `IsPending` — pending and posted rows coexist, so counting both doubles a month's spend. The `Amount < 0` arm is defensive; no ingest path can produce such a row. Pre-existing `UnusualSpendDetectionJob` still filters on `t.Amount < 0` alone and is therefore inert — out of scope here, tracked as follow-up
- BankSync job can inject `ISubscriptionHygieneSummaryReader` without a project reference to Subscriptions — DI resolves at runtime via the composition root
