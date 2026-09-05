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
- BankSync job can inject `ISubscriptionHygieneSummaryReader` without a project reference to Subscriptions — DI resolves at runtime via the composition root
