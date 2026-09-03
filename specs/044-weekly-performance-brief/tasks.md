# Tasks: Weekly Performance Brief (044)

**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)
**GitHub Issue**: #414

## [US1] Companion pipeline wiring + signal trends

- [x] Add `PerformanceBrief` to `CompanionEventKind` enum
- [x] Map `"PerformanceBrief"` → `CompanionEventKind.PerformanceBrief` in `MaterialityPolicy.ClassifyAlert`
- [x] Inject `IRadarSignalRepository` into `BookPerformanceBriefJob`; query recent Notable `allocation_drift` signals; append trend lines to message body
- [x] Write unit tests (`FinanceSentry.Tests.Unit/Radar/BookPerformanceBriefJobTests.cs`) — 10 tests
- [x] `dotnet build FinanceSentry.sln` → zero warnings
- [x] `dotnet test --filter "Category!=Integration"` → all pass (551 unit, 102 MCP, 120 integration)
- [x] Commit spec artifacts + code

## [US2] Cron failure alerting

- [x] Satisfied by existing global `ConsecutiveFailureAlertFilter` — no new code needed
- [x] Lower `Observability:JobFailureAlertThreshold` from 3 → 2 in `appsettings.json` so the brief job alerts after 2 consecutive failures (matches AC)

## [US3] Regression coverage for materiality + dedup paths

- [x] Add `[InlineData("PerformanceBrief", CompanionEventKind.PerformanceBrief)]` to `MaterialityPolicyTests.Known_alert_types_map_to_kinds` — confirms Telegram routing is under test
- [x] Add `GeneratePerformanceBrief_NoRecent_AddsInfoAlert` to `AlertGeneratorServiceTests` — confirms fresh-alert path calls `AddAsync`
- [x] Add `GeneratePerformanceBrief_WithinSixDaySuppressWindow_SkipsCreation` to `AlertGeneratorServiceTests` — confirms 6-day suppression window blocks duplicate delivery
- [x] `dotnet build FinanceSentry.sln -c Release` → 0 warnings, 0 errors
- [x] `dotnet test --filter "Category!=Integration"` → 553 unit / 102 MCP / 120 integration all pass

## [US4] Track-record delta + one policy-judged suggested action

- [x] Add `ITrackRecordSource` port (+ `TrackRecordDelta`) to `Modules.Radar/Domain/Ports`
- [x] Add `ResearchTrackRecordSource` adapter in `FinanceSentry.Integration` over `GetTrackRecordQuery`; combine the `Closed` + `Broken` slices by count weight so terminal and active records are never blended
- [x] Register the port in `CrossModulePortRegistration.AddCrossModulePorts`
- [x] Extract message composition out of `BookPerformanceBriefJob` into `PerformanceBriefComposer`
- [x] Add the track-record line ("Calls: …") with the low-sample caveat
- [x] Add at most one action line, priority allocation drift → cash floor → position cap; silent when nothing breaches policy
- [x] Fix the drift-percentage formatting bug (`driftPct` is percentage points, `:P1` rendered `8.3` as `830.0%`)
- [x] Re-budget the layout so headline + body stays ≤12 lines with every section present
- [x] Query all Notable `portfolio_scanner` signals (not just `allocation_drift`) in the job
- [x] Port `BookPerformanceBriefJobTests` → `PerformanceBriefComposerTests` (+11 new cases, 21 total)
- [x] Add `ResearchTrackRecordSourceTests` (3 cases) under `Tests.Integration/CrossModulePorts`
- [x] `dotnet build FinanceSentry.sln -c Release` → 0 warnings, 0 errors
- [x] `dotnet test --filter "Category!=Integration"` → 563 unit / 123 integration / 102 MCP all pass
