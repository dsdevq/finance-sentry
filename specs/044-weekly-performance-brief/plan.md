# Plan: Weekly Performance Brief (044)

## Story slices

### Slice 1 (shipped) — Companion pipeline wiring + signal trends

Touches:
- `FinanceSentry.Modules.Companion/Domain/CompanionEventKind.cs` — add `PerformanceBrief`
- `FinanceSentry.Modules.Companion/Application/Services/MaterialityPolicy.cs` — map `"PerformanceBrief"` → `CompanionEventKind.PerformanceBrief`
- `FinanceSentry.Modules.Radar/Infrastructure/Jobs/BookPerformanceBriefJob.cs` — inject `IRadarSignalRepository`; append up to 4 Notable `allocation_drift` trend lines after the scoreboard
- `FinanceSentry.Tests.Unit/Radar/BookPerformanceBriefJobTests.cs` (new) — unit tests for message building and trend inclusion

### Slice 2 (this session) — US4: track-record delta + one policy-judged action

Touches:
- `FinanceSentry.Modules.Radar/Domain/Ports/ITrackRecordSource.cs` (new) — port + `TrackRecordDelta`
- `FinanceSentry.Integration/ResearchTrackRecordSource.cs` (new) — adapter over `GetTrackRecordQuery`
- `FinanceSentry.Integration/CrossModulePortRegistration.cs` — register the port
- `FinanceSentry.Modules.Radar/Application/Services/PerformanceBriefComposer.cs` (new) — message
  composition moved out of the job, plus the track-record line, the action line, the line budget
- `FinanceSentry.Modules.Radar/Infrastructure/Jobs/BookPerformanceBriefJob.cs` — orchestration only;
  widens the signal query from `allocation_drift` to all Notable `portfolio_scanner` signals
- `FinanceSentry.Tests.Unit/Radar/PerformanceBriefComposerTests.cs` — replaces
  `BookPerformanceBriefJobTests.cs`; the existing cases move with the code under test

Constraints found:
- `driftPct` in the `allocation_drift` payload is **percentage points (0–100)**, not a fraction —
  slice 1 formatted it with `:P1`, which rendered `8.3` as `830.0%`. Fixed here; the slice-1 tests
  hid it by feeding fractions.
- `GetTrackRecordQuery` returns blended (terminal + active) averages at the top level; the
  per-status slices are the only non-blended source, so the adapter combines `Closed` + `Broken`
  by count weight and never mixes them with `Active` (feature 020 R4).
- Only one signal query is issued: all Notable `portfolio_scanner` signals for the user, then
  partitioned by type in the composer.

### Slice 3 (this session) — US5: unit-consistent policy limits + observable total failure

Touches:
- `FinanceSentry.Integration/PortfolioScanDataReader.cs` — convert `MaxPositionWeightPct` /
  `MinCashBufferPct` from the stored fraction to percentage points at the port boundary
- `FinanceSentry.Modules.Radar/Infrastructure/Jobs/BookPerformanceBriefJob.cs` — collect per-user
  failures and throw when every active user failed
- `FinanceSentry.Tests.Integration/CrossModulePorts/PortfolioScanDataReaderTests.cs` (new)
- `FinanceSentry.Tests.Unit/Radar/BookPerformanceBriefJobTests.cs` (new — the name freed when
  slice 2 moved composition into `PerformanceBriefComposerTests`)
- `FinanceSentry.Modules.Radar.Tests/Portfolio/PortfolioScannerTests.cs` — pin the payload units

Constraints found:
- `RiskRuleSet.MaxPositionWeightPct` / `MinCashBufferPct` are **fractions in (0,1]** despite the
  `Pct` suffix (`SaveRiskRuleSetCommand.ValidateFractionalRange`, `numeric(9,6)`), while
  `PortfolioScanData` is contractually percentage points. `IPositionCapSource` documents the
  fraction and its consumer compares fractions, so the conversion belongs in the scan adapter only.
- `ConsecutiveFailureAlertFilter` is an `IApplyStateFilter` — it only ever sees job-level terminal
  states, so a job that catches everything internally can never accumulate a streak.

## Constraints / decisions

- Action-line priority is `allocation_drift` (the IPS bands proper) → `cash_buffer` → 
  `concentration_weight`. Rationale: drift is what the IPS actually states; the other two are the
  risk-rule boundary around it. `sync_health` deliberately produces no action — stale data is a
  data-quality caveat, not a portfolio move.
- The line budget counts the headline, so the body is capped at 11 lines; the action line and the
  track-record line are reserved before drift trend lines are allocated.
- `PerformanceBrief` is NOT in `CompanionEventKind` enum and NOT in `MaterialityPolicy` — both gaps must be filled for Telegram delivery to work.
- Brief is the `Alert.Title` (headline) + `Alert.Message` (body). The Companion dispatch sends both. Body must be ≤ ~12 lines.
- Signal query: `IRadarSignalRepository.ListAsync(new SignalFilter(Since: 30-day lookback, Scanner: "portfolio_scanner", SignalType: "allocation_drift", UserId: userId, Severity: "notable"))`. Take up to 4 lines sorted by `driftPct` descending.
- `DispositionFor` default policy (quiet→suppress, digest→hold) is correct for PerformanceBrief — no special override needed.
- US2 (cron failure alerting) rides the existing global `ConsecutiveFailureAlertFilter`, but the job
  has to actually fail for the filter to see anything: per-user isolation is kept, and the run
  throws only when **every** active user failed. A partial failure still delivers the briefs it can,
  which is the behaviour worth preserving over an all-or-nothing job.
