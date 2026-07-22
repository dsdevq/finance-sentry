# Tasks: Thesis Break Monitor

**Feature**: `017-thesis-monitor` | **Branch**: `017-thesis-monitor`
**Input**: plan.md, spec.md, data-model.md, contracts/, research.md, quickstart.md
**Tests**: REQUIRED — spec mandates Test-First (SC-001 deterministic evaluator, constitution Testing Discipline gate).

All paths under repo root `/home/dsdevqq/projects/finance-sentry`. Backend module: `FinanceSentry.Modules.Research`.

**Build gate reminder**: after every `.cs` change run `dotnet build backend/` → zero warnings before the task is complete.

---

## Phase 1: Setup

- [X] T001 Confirm the two open data-migration inputs before M004: exact GRAB thesis id and DRAM `price_drawdown` threshold (0.30?). Query `research.theses` (`SELECT id, ticker, invalidation_triggers FROM research.theses;`) on the running Postgres and record the real ids/current triggers in `specs/017-thesis-monitor/data-model.md` (replace the `e7b9af2c-…` placeholder).
- [X] T002 [P] Verify test projects exist: `backend/tests/FinanceSentry.Modules.Research.Tests/` and `backend/tests/FinanceSentry.Mcp.Tests/`. If the Research test project is missing, create it (xUnit, reference `FinanceSentry.Modules.Research`) and add to the solution.

---

## Phase 2: Foundational (blocking prerequisites)

**These block ALL user stories — the trigger shape, vocabulary, and price-history source must exist first.**

- [X] T003 Add `ThesisPeriodType` enum (`Quarter`, `Annual`) in `backend/src/FinanceSentry.Modules.Research/Domain/InvestmentThesis.cs` (or a sibling `Domain/ThesisPeriodType.cs`).
- [X] T004 Extend the `ThesisInvalidationTrigger` record in `backend/src/FinanceSentry.Modules.Research/Domain/InvestmentThesis.cs`: add `string? ProxyTicker`, `int ConsecutivePeriods = 1`, `ThesisPeriodType PeriodType = ThesisPeriodType.Quarter`. Keep it a positional record; ensure System.Text.Json (Web defaults) round-trips missing keys to defaults.
- [X] T005 [P] Create the closed metric vocabulary `ThesisMetric` in `backend/src/FinanceSentry.Modules.Research/Domain/ThesisMonitor/ThesisMetric.cs` — the 12 keys (`gross_margin`, `operating_margin`, `net_margin`, `revenue_yoy`, `net_income_yoy`, `operating_income_yoy`, `eps_yoy`, `revenue`, `net_income`, `diluted_eps`, `price_drawdown`, `price_return`), with a `Contains(string)` guard and a `IsPriceMetric(string)` helper.
- [X] T006 [P] Create `TriggerVerdict` result types in `backend/src/FinanceSentry.Modules.Research/Domain/ThesisMonitor/TriggerVerdict.cs`: `Breached(Metric, decimal[] ObservedValues, string[] Periods, decimal Threshold, string Direction)`, `Held`, `NonEvaluable(string Reason)` (reasons: `no_fundamentals`, `insufficient_periods`, `divide_by_zero`, `no_price_history`, `unsupported_metric`).
- [X] T007 [P] Add `DailyClose(DateOnly Date, decimal Close)` DTO and the `GetDailyClosesAsync(string ticker, DateOnly since, CancellationToken ct)` method signature to `backend/src/FinanceSentry.Modules.Research/Application/Services/IMarketDataService.cs`.
- [X] T008 Implement `GetDailyClosesAsync` in `backend/src/FinanceSentry.Modules.Research/Application/Services/YahooMarketDataService.cs` — reuse the existing `/v8/finance/chart/{ticker}?interval=1d` call, widen `range` to cover `since`, and retain the full close series (currently discarded). Return empty list on fetch failure (caller treats as non-evaluable).
- [X] T009 Add `ThesisTriggerVocabulary` static guard in `backend/src/FinanceSentry.Modules.Research/Application/Validation/ThesisTriggerVocabulary.cs` and invoke it from `SaveThesisCommandHandler` (`backend/src/FinanceSentry.Modules.Research/Application/Commands/SaveThesisCommand.cs`) — reject any trigger whose `Metric` ∉ vocabulary (FR-012), throwing a domain validation exception.

**Checkpoint**: trigger shape, vocabulary, price source, and save-guard all exist and build clean.

---

## Phase 3: User Story 1 — Automatic thesis-break detection (P1) 🎯 MVP

**Goal**: Scheduled deterministic evaluation of every active thesis's triggers; mark broken + raise one alert.
**Independent test**: Seed a thesis with `gross_margin < 0.35` proxy MU across 2 quarters below threshold; run monitor; assert broken with cited reason and exactly one `ThesisBroken` alert.

### Tests (write first — must fail before impl)

- [X] T010 [P] [US1] Evaluator unit tests in `backend/tests/FinanceSentry.Modules.Research.Tests/ThesisMonitor/ThesisBreakEvaluatorTests.cs`: consecutive-period breach (holds all N periods → breach; holds N-1 → held), YoY using same fiscal period prior year, proxy-ticker substitution (evaluates proxy not thesis ticker), OR semantics (any trigger breaches → break; `BrokenReason` names first breaching trigger), no-triggers → skipped.
- [X] T011 [P] [US1] Non-evaluable unit tests in the same test dir: missing fundamentals → `NonEvaluable(no_fundamentals)`, insufficient periods → `insufficient_periods`, Revenue=0 denominator → `divide_by_zero`, unsupported metric → `unsupported_metric` — none produce a breach (SC-002).

### Implementation

- [X] T012 [US1] Implement the pure deterministic `ThesisBreakEvaluator` in `backend/src/FinanceSentry.Modules.Research/Domain/ThesisMonitor/ThesisBreakEvaluator.cs`: given a trigger + `IReadOnlyList<FundamentalFact>` (target ticker) and/or `IReadOnlyList<DailyClose>`, compute the metric over the most recent `ConsecutivePeriods` periods and return a `TriggerVerdict`. Fundamentals metrics select by `PeriodType`/fiscal period; price metrics compute drawdown/return over trading days. No EF, no HTTP, no LLM.
- [X] T013 [US1] Add `AlertType.ThesisBroken` const to `backend/src/FinanceSentry.Modules.Alerts/Domain/AlertType.cs` and a silence-window `TimeSpan` for it in `AlertGeneratorService`.
- [X] T014 [US1] Add `GenerateThesisBreakAlertAsync(Guid userId, Guid thesisId, string ticker, string reason, CancellationToken)` to `backend/src/FinanceSentry.Core/Interfaces/IAlertGeneratorService.cs` and implement in `backend/src/FinanceSentry.Modules.Alerts/Application/Services/AlertGeneratorService.cs` following `GenerateLowBalanceAlertAsync` (FindActive → HasRecent silence window → AddAsync; `ReferenceId`=thesisId, `ReferenceLabel`=ticker, Severity=Warning).
- [X] T015 [US1] Add `RunThesisMonitorCommand` + handler in `backend/src/FinanceSentry.Modules.Research/Application/Commands/RunThesisMonitorCommand.cs` (Core.Cqrs `ICommand<ThesisMonitorRunSummary>`): load active theses for the user via `IThesisRepository`, fetch fundamentals (`ISecEdgarService`, ≥8 periods) and price closes per target/proxy ticker, run `ThesisBreakEvaluator`, on unbroken→broken set `BrokenAt`/`BrokenReason` + call `GenerateThesisBreakAlertAsync`, persist via `UpsertAsync`, accumulate `ThesisMonitorRunSummary` counts. One thesis/ticker failure is caught, counted in `errors`, run continues (FR-014).
- [X] T016 [US1] Add `ThesisMonitorRunSummary` result record in `backend/src/FinanceSentry.Modules.Research/Application/Commands/` (or `Domain/ThesisMonitor/`): `ThesesEvaluated, TriggersEvaluated, BreaksRaised, BreaksCleared, Skipped, Errors` (FR-016).
- [X] T017 [US1] Create `ThesisMonitorJob` in `backend/src/FinanceSentry.Modules.Research/Infrastructure/Jobs/ThesisMonitorJob.cs` (sealed, `[AutomaticRetry(Attempts = 2)]`, ILogger, primary ctor) with `ExecuteAsync(CancellationToken)` that resolves the command handler and runs it for all users; log the summary via Serilog.
- [X] T018 [US1] Register in `backend/src/FinanceSentry.Modules.Research/ResearchModule.cs`: `services.AddScoped<ThesisMonitorJob>()` and add `mgr.AddOrUpdate<ThesisMonitorJob>("thesis-monitor", j => j.ExecuteAsync(CancellationToken.None), Cron.Daily())` to the existing `JobRegistrar`.
- [X] T019 [US1] Create migration `M004_ThesisTriggerV2` in `backend/src/FinanceSentry.Modules.Research/Migrations/` (`dotnet ef migrations add M004_ThesisTriggerV2 --context ResearchDbContext`): reshape `invalidation_triggers` jsonb and backfill the DRAM/GRAB triggers to the exact target state (per data-model.md, using ids confirmed in T001). Verify `dotnet ef database update` applies cleanly.

**Checkpoint**: US1 independently testable — scheduled/handler run marks a breached thesis broken with one alert.

---

## Phase 4: User Story 2 — On-demand evaluation & read via MCP (P1)

**Goal**: Trigger the monitor and read current breaks through MCP.
**Independent test**: Call `run_thesis_monitor` then `list_thesis_breaks`; the second reflects what the first marked broken.

### Tests (write first)

- [X] T020 [P] [US2] Add parity `[Fact]`s for `run_thesis_monitor` and `list_thesis_breaks` in `backend/tests/FinanceSentry.Mcp.Tests/IntegrationTests/ToolParityTests.cs` (invoke against seeded in-memory DB; assert run summary shape and that a broken thesis appears in the list; empty-list-not-error case).

### Implementation

- [X] T021 [US2] Add `ListThesisBreaksQuery` + handler in `backend/src/FinanceSentry.Modules.Research/Application/Queries/ListThesisBreaksQuery.cs` (Core.Cqrs `IQuery<IReadOnlyList<ThesisBreakView>>`): return every broken thesis for the user with metric, observed value(s), period(s), threshold, direction, reason (FR-015, SC-005). Add the `ThesisBreakView` record.
- [X] T022 [P] [US2] Create `RunThesisMonitorTool` in `backend/src/FinanceSentry.Mcp/Tools/RunThesisMonitorTool.cs` — `[McpServerToolType]`, `[McpServerTool(Name = "run_thesis_monitor")]`, primary-ctor DI of the command handler + `IIdentityResolver`, resolve `userId ?? identity.GetUserId()`, `[Description]` on method + `userId` param. Mirror `SaveThesisTool`.
- [X] T023 [P] [US2] Create `ListThesisBreaksTool` in `backend/src/FinanceSentry.Mcp/Tools/ListThesisBreaksTool.cs` — `[McpServerTool(Name = "list_thesis_breaks")]`, same DI/identity pattern, returns `ThesisBreakView[]`.

**Checkpoint**: both MCP tools invocable; parity tests green.

---

## Phase 5: User Story 3 — Idempotent state & auto-clear (P2)

**Goal**: One alert per transition; broken state auto-clears when the condition resolves in fresh data.
**Independent test**: Broken thesis whose condition still holds → no new alert; condition cleared in newer data → un-broken + alert resolved; breaches again → fresh break + alert.

### Tests (write first)

- [X] T024 [P] [US3] Handler-level tests in `backend/tests/FinanceSentry.Modules.Research.Tests/ThesisMonitor/RunThesisMonitorHandlerTests.cs`: (a) re-run on still-broken → zero new alerts, `BrokenAt` unchanged (SC-003); (b) cleared condition → `BrokenAt`/`BrokenReason` nulled, resolve called, `BreaksCleared`++; (c) re-breach after clear → fresh break + alert.

### Implementation

- [X] T025 [US3] Add `ResolveThesisBreakAlertAsync(Guid userId, Guid thesisId, CancellationToken)` to `IAlertGeneratorService` + impl in `AlertGeneratorService` (find active `ThesisBroken` alert by `ReferenceId` → mark resolved), mirroring existing resolve methods.
- [X] T026 [US3] Extend `RunThesisMonitorCommand` handler transition logic (T015): on broken→cleared clear `BrokenAt`/`BrokenReason`, call `ResolveThesisBreakAlertAsync`, increment `BreaksCleared`; on broken→still-broken no-op (rely on generator active-alert dedup); ensure no oscillation spam (FR-011, US3).

**Checkpoint**: idempotency + auto-clear verified by handler tests.

---

## Phase 6: User Story 4 — Non-evaluable never false-breaks (P2)

**Goal**: Missing/insufficient/div-by-zero/no-price data is skipped and recorded, never a break.
**Independent test**: Trigger on a no-EDGAR ticker → skipped, not broken; `revenue_yoy` with one quarter → skipped; Revenue=0 period → non-evaluable not divide.

> Core logic already implemented in T012 + T011. This phase confirms end-to-end integration through the handler and run summary.

- [X] T027 [P] [US4] Handler integration tests in `backend/tests/FinanceSentry.Modules.Research.Tests/ThesisMonitor/`: no-fundamentals ticker → thesis not broken, counted in `Skipped`; ETF/basket with no proxy → all triggers non-evaluable, never broken; fetch failure for one ticker → that thesis in `Errors`, run continues for others (FR-014).
- [X] T028 [US4] Ensure the handler records non-evaluable outcomes into `Skipped` and per-ticker exceptions into `Errors` in the summary (wire-through from T012 verdicts) — adjust T015 handler if not already covered.

---

## Phase 7: Polish & Cross-Cutting

- [X] T029 [P] Update the MCP tool-count/contract expectation if any hard count exists, and confirm `ToolAttributeContractTests` still passes with the two new tools.
- [X] T030 [P] Run `dotnet build backend/` → zero warnings; run `dotnet test backend/tests/FinanceSentry.Modules.Research.Tests backend/tests/FinanceSentry.Mcp.Tests` → all green.
- [X] T031 Execute quickstart.md golden path against the live stack (docker compose up postgres+api, apply M004, `run_thesis_monitor` → `list_thesis_breaks`) and confirm SC-006 (DRAM/GRAB evaluate end-to-end against live EDGAR without error).
- [X] T032 [P] Verify SC-007: grep the Research module for any messaging/Telegram/email dependency — there must be none (module raises domain alerts only).

---

## Dependencies & Execution Order

- **Setup (T001–T002)** → **Foundational (T003–T009)** block everything.
- **US1 (T010–T019)** is the MVP; depends on Foundational. T012 (evaluator) is the spine — T015 handler depends on it, T013/T014 (alert) parallel to it.
- **US2 (T020–T023)** depends on US1's command/query existing (T015, plus T021 query).
- **US3 (T024–T026)** extends US1's handler (T015) + adds resolve (T025).
- **US4 (T027–T028)** validates evaluator/handler already built; mostly tests.
- **Polish (T029–T032)** last.

### Parallel opportunities

- Foundational: T005, T006, T007 in parallel (distinct files); T008 after T007; T009 after T004/T005.
- US1 tests: T010, T011 in parallel before T012.
- US1 impl: T013+T014 (Alerts) parallel to T012 (evaluator).
- US2 tools: T022, T023 in parallel after T021.

## MVP scope

**Phase 1 + 2 + Phase 3 (US1)** = deterministic scheduled break detection with one alert — the whole value of the feature. US2 adds the MCP surface (also P1, ship together). US3/US4 (P2) harden idempotency and non-evaluable safety.

## Implementation strategy

Implement with **Sonnet** per the approved recipe (this feature: plan/review big model, implement Sonnet). Go phase by phase, Test-First within each story, `dotnet build` zero-warnings after every `.cs`, commit per logical task per constitution branching discipline.
