# 044 — Hygiene Sentinels: Tasks

## [US1] Price hike detection

- [x] Create `ISubscriptionHygieneSummaryReader` port in Core
- [x] Implement `SubscriptionHygieneSummaryReader` in Subscriptions module + register
- [x] Add `PriceHike` to `AlertType`, `IAlertGeneratorService`, `AlertGeneratorService`
- [x] Add `PriceHike` to `CompanionEventKind` + `MaterialityPolicy`
- [x] Implement `PriceHikeDetectionJob` + register + schedule in BankSyncModule
- [x] Write unit tests for `PriceHikeDetectionJob`

## [US2] Duplicate charge detection

- [x] Add `DuplicateCharge` to `AlertType`, `IAlertGeneratorService`, `AlertGeneratorService`
- [x] Add `DuplicateCharge` to `CompanionEventKind` + `MaterialityPolicy`
- [x] Implement `DuplicateChargeDetectionJob` + register + schedule in BankSyncModule
- [x] Write unit tests for `DuplicateChargeDetectionJob`

## [US3] Category spike detection

- [x] Add `CategorySpike` to `AlertType`, `IAlertGeneratorService`, `AlertGeneratorService`
- [x] Add `CategorySpike` to `CompanionEventKind` + `MaterialityPolicy`
- [x] Implement `CategorySpikeDetectionJob` + register + schedule in BankSyncModule
- [x] Write unit tests for `CategorySpikeDetectionJob`

## [US4] FX spread detection

- [x] Add `FxSpread` to `AlertType`, `IAlertGeneratorService`, `AlertGeneratorService`
- [x] Add `FxSpread` to `CompanionEventKind` + `MaterialityPolicy`
- [x] Implement `FxSpreadDetectionJob` + register + schedule in BankSyncModule
- [x] Write unit tests for `FxSpreadDetectionJob`
