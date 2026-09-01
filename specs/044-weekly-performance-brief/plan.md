# Plan: Weekly Performance Brief (044)

## Story slices

### Slice 1 (this session) — Companion pipeline wiring + signal trends

Touches:
- `FinanceSentry.Modules.Companion/Domain/CompanionEventKind.cs` — add `PerformanceBrief`
- `FinanceSentry.Modules.Companion/Application/Services/MaterialityPolicy.cs` — map `"PerformanceBrief"` → `CompanionEventKind.PerformanceBrief`
- `FinanceSentry.Modules.Radar/Infrastructure/Jobs/BookPerformanceBriefJob.cs` — inject `IRadarSignalRepository`; append up to 4 Notable `allocation_drift` trend lines after the scoreboard
- `FinanceSentry.Tests.Unit/Radar/BookPerformanceBriefJobTests.cs` (new) — unit tests for message building and trend inclusion

## Constraints / decisions

- `PerformanceBrief` is NOT in `CompanionEventKind` enum and NOT in `MaterialityPolicy` — both gaps must be filled for Telegram delivery to work.
- Brief is the `Alert.Title` (headline) + `Alert.Message` (body). The Companion dispatch sends both. Body must be ≤ ~12 lines.
- Signal query: `IRadarSignalRepository.ListAsync(new SignalFilter(Since: 30-day lookback, Scanner: "portfolio_scanner", SignalType: "allocation_drift", UserId: userId, Severity: "notable"))`. Take up to 4 lines sorted by `driftPct` descending.
- `DispositionFor` default policy (quiet→suppress, digest→hold) is correct for PerformanceBrief — no special override needed.
- US2 (cron failure alerting) is satisfied by the existing global `ConsecutiveFailureAlertFilter` — no new code.
