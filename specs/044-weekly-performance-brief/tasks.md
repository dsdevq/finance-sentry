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
- [ ] Commit spec artifacts + code

## [US2] Cron failure alerting

- [x] Satisfied by existing global `ConsecutiveFailureAlertFilter` — no new code needed
