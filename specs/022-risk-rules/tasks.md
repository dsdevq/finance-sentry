# Tasks: Risk Rules

**Input**: Design documents from `specs/022-risk-rules/` (spec.md, plan.md, research.md, data-model.md, quickstart.md)
**Prerequisites**: plan.md, spec.md, research.md, data-model.md

**Tests**: xUnit unit tests for all pure evaluation logic (MANDATORY — this is the core deliverable per SC-001), REST contract tests per new endpoint (MANDATORY), MCP tool contract test update (MANDATORY — `ToolNameContractTests` fails otherwise). No frontend, no E2E — this feature ships no UI in v1.

**Organization**: Tasks are grouped by user story (US1 = policy detection, US2 = promotion gate, US3 = broken-thesis flag) so each is independently implementable and testable. Setup and Foundational phases block all stories.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 / US2 / US3, or blank for Setup/Foundational/Polish

---

## Phase 1: Setup

**Purpose**: Scaffold the new module and register it — nothing functional yet.

- [ ] T001 Create `FinanceSentry.Modules.Risk` project (new `.csproj`, net9.0, referencing `FinanceSentry.Core`) at `backend/src/FinanceSentry.Modules.Risk/FinanceSentry.Modules.Risk.csproj`; add to `backend/FinanceSentry.sln`
- [ ] T002 Create empty folder skeleton per plan.md Project Structure: `API/Controllers/`, `API/Responses/`, `Application/Commands/`, `Application/Queries/`, `Application/Services/`, `Domain/`, `Domain/Exceptions/`, `Domain/Repositories/`, `Infrastructure/Jobs/`, `Infrastructure/Persistence/Repositories/`, `Migrations/` under `backend/src/FinanceSentry.Modules.Risk/`
- [ ] T003 [P] Reference `FinanceSentry.Modules.Risk` from `backend/src/FinanceSentry.API/FinanceSentry.API.csproj` and from `backend/src/FinanceSentry.Mcp/FinanceSentry.Mcp.csproj`
- [ ] T004 [P] Reference `FinanceSentry.Modules.Research` (for `IThesisRepository`) from `FinanceSentry.Modules.Risk.csproj` — read-only cross-module dependency per research.md R3

**Checkpoint**: Solution builds with the empty Risk module referenced everywhere it will be needed.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, persistence, module registration, and the Core/Alerts extension every user story depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T005 [P] `RiskRuleSet` entity in `backend/src/FinanceSentry.Modules.Risk/Domain/RiskRuleSet.cs` per data-model.md (versioned, `IsCurrent`, all fields optional)
- [ ] T006 [P] `PolicyViolationAck` entity in `backend/src/FinanceSentry.Modules.Risk/Domain/PolicyViolationAck.cs`
- [ ] T007 [P] `HoldingSnapshot` entity in `backend/src/FinanceSentry.Modules.Risk/Domain/HoldingSnapshot.cs`
- [ ] T008 [P] Transient records `BookSnapshot`, `BookPosition`, `PolicyViolation`, `RiskVerdict`, `ComplianceReport` in `backend/src/FinanceSentry.Modules.Risk/Domain/BookSnapshot.cs`, `PolicyViolation.cs`, `RiskVerdict.cs` (per data-model.md "In-memory / transient types")
- [ ] T009 [P] `RiskRuleSetNotFoundException` in `backend/src/FinanceSentry.Modules.Risk/Domain/Exceptions/RiskRuleSetNotFoundException.cs`
- [ ] T010 [P] Repository interfaces `IRiskRuleSetRepository`, `IPolicyViolationAckRepository`, `IHoldingSnapshotRepository` in `backend/src/FinanceSentry.Modules.Risk/Domain/Repositories/`
- [ ] T011 `RiskDbContext` (schema `risk`) in `backend/src/FinanceSentry.Modules.Risk/Infrastructure/Persistence/RiskDbContext.cs` mapping the three tables from data-model.md; `RiskDbContextFactory` for design-time migrations (mirrors `WealthDbContextFactory`)
- [ ] T012 Generate migration `M001_InitialSchema` (risk_rule_sets, policy_violation_acks, holding_snapshots + indexes) via `dotnet ef migrations add M001_InitialSchema --project backend/src/FinanceSentry.Modules.Risk --startup-project backend/src/FinanceSentry.API`
- [ ] T013 [P] Repository implementations `RiskRuleSetRepository`, `PolicyViolationAckRepository`, `HoldingSnapshotRepository` in `backend/src/FinanceSentry.Modules.Risk/Infrastructure/Persistence/Repositories/` (depends on T011)
- [ ] T014 `RiskModule.cs` at `backend/src/FinanceSentry.Modules.Risk/RiskModule.cs` — DI registration mirroring `WealthModule.cs`/`AlertsModule.cs` (DbContext, repositories, services, controllers)
- [ ] T015 Register `RiskModule` in `backend/src/FinanceSentry.API/Program.cs`
- [ ] T016 Extend `IAlertGeneratorService` (Core) with `GeneratePolicyViolationAlertAsync(...)` / `ResolvePolicyViolationAlertAsync(...)` in `backend/src/FinanceSentry.Core/Interfaces/IAlertGeneratorService.cs`
- [ ] T017 Add `AlertType.PolicyViolation` const in `backend/src/FinanceSentry.Modules.Alerts/Domain/AlertType.cs`
- [ ] T018 Implement the two new `IAlertGeneratorService` methods in `backend/src/FinanceSentry.Modules.Alerts/Application/Services/AlertGeneratorService.cs` (dedup pattern matching `GenerateLowBalanceAlertAsync`'s existing silence-window approach)
- [ ] T019 [P] `IBookSnapshotReader`/`BookSnapshotReader` in `backend/src/FinanceSentry.Modules.Risk/Application/Services/` — aggregates `ICryptoHoldingsReader`, `IBrokerageHoldingsReader`, `IBankingAccountsReader` into a `BookSnapshot`, per-source try/catch → `IsStale`/`StaleSources` (mirrors `GetAllocationDriftQueryHandler`'s degrade pattern)
- [ ] T020 [P] Unit tests for `BookSnapshotReader` in `backend/tests/FinanceSentry.Tests/Risk/BookSnapshotReaderTests.cs` — all-sources-ok, one-source-fails-marks-stale, all-sources-fail

**Checkpoint**: Module builds, migrates, is registered, and can read a `BookSnapshot`. Alerts can emit `PolicyViolation`. No user-facing behavior yet — this is the shared floor for US1–US3.

---

## Phase 3: User Story 1 — Policy violations are detected and surfaced (Priority: P1) 🎯 MVP

**Goal**: A scheduled daily check evaluates the live book against the configured `RiskRuleSet` and raises violations as Alerts, with an acknowledgement flow so pre-existing violations (the 46% DRAM case) don't spam.

**Independent Test**: Configure `maxPositionWeightPct = 0.25`; seed a book with one position at 46%; run the check; assert a `PolicyViolation` signal + one Alert naming the rule, observed value, and limit (per quickstart.md steps 1–4).

### Tests for User Story 1 (write first, must fail before implementation)

- [ ] T021 [P] [US1] Unit tests for `RiskEvaluationService.Evaluate(...)` in `backend/tests/FinanceSentry.Tests/Risk/RiskEvaluationServiceTests.cs` — cases: compliant book → empty violations + `info`; single position over `maxPositionWeightPct` → one `PolicyViolation` with correct `ExcessUsd`/`ExcessPct`; `maxSleeveWeightPct` breach; `minCashBufferPct` breach; multiple simultaneous violations; no rule set configured → "no rules on file" result, not an inferred default
- [ ] T022 [P] [US1] Unit tests for the acknowledgement/worsening-step path in `backend/tests/FinanceSentry.Tests/Risk/RiskEvaluationServiceTests.cs` (or a sibling `PolicyViolationAckTests.cs`) — seeded 46%-vs-25% scenario produces exactly one alert-worthy violation; after ack, re-run reports `Acknowledged` and does not re-alert; worsening past `WorseningStepPct` flips `Status` to `Worsened` and is alert-worthy again (SC-002)
- [ ] T023 [P] [US1] Unit tests for the stale-book path in `backend/tests/FinanceSentry.Tests/Risk/RiskEvaluationServiceTests.cs` — a stale source flags the report `IsStale = true` and existing violations do NOT silently auto-clear
- [ ] T024 [P] [US1] Unit tests for `SaveRiskRuleSetCommand` validation + versioning in `backend/tests/FinanceSentry.Tests/Risk/SaveRiskRuleSetCommandTests.cs` — weight out of `(0,1]` rejected, valid save appends new version and flips `IsCurrent`, all-fields-optional save accepted
- [ ] T025 [P] [US1] REST contract tests for `RiskController` in `backend/tests/FinanceSentry.Tests/Risk/RiskRulesContractTests.cs` — `GET /risk/rules` (200 + null-safe empty state), `PUT /risk/rules` (200/400 on invalid range), `GET /risk/compliance` (200, schema matches `ComplianceReportDto`), `POST /risk/violations/{id}/acknowledge` (200/404)

### Implementation for User Story 1

- [ ] T026 [US1] `IRiskEvaluationService`/`RiskEvaluationService` in `backend/src/FinanceSentry.Modules.Risk/Application/Services/IRiskEvaluationService.cs` + `RiskEvaluationService.cs` — pure function `(BookSnapshot, RiskRuleSet?, IReadOnlyList<PolicyViolationAck>) -> ComplianceReport`; implements FR-001 rule checks (position weight, sleeve weight, cash buffer) and the ack/worsening logic from research.md R8 (depends on T021–T024 failing tests, T005–T008)
- [ ] T027 [US1] `GetRiskRuleSetQuery`/handler in `backend/src/FinanceSentry.Modules.Risk/Application/Queries/GetRiskRuleSetQuery.cs` (`IQuery<RiskRuleSetDto?>` via `Core.Cqrs`, per research.md R2)
- [ ] T028 [US1] `SaveRiskRuleSetCommand`/handler in `backend/src/FinanceSentry.Modules.Risk/Application/Commands/SaveRiskRuleSetCommand.cs` — range validation, appends version, flips `IsCurrent` (depends on T024, T013)
- [ ] T029 [US1] `CheckRiskRulesQuery` (no-proposal branch) + handler in `backend/src/FinanceSentry.Modules.Risk/Application/Queries/CheckRiskRulesQuery.cs` — assembles `BookSnapshot` via `BookSnapshotReader`, loads current `RiskRuleSet` + acks, calls `RiskEvaluationService`, returns `ComplianceReport` (depends on T019, T026, T027)
- [ ] T030 [US1] `AcknowledgeViolationCommand`/handler in `backend/src/FinanceSentry.Modules.Risk/Application/Commands/AcknowledgeViolationCommand.cs` (depends on T013)
- [ ] T031 [P] [US1] Response DTOs `RiskRuleSetDto`, `ComplianceReportDto` in `backend/src/FinanceSentry.Modules.Risk/API/Responses/`
- [ ] T032 [US1] `RiskController` in `backend/src/FinanceSentry.Modules.Risk/API/Controllers/RiskController.cs` — `GET/PUT /api/v1/risk/rules`, `GET /api/v1/risk/compliance`, `POST /api/v1/risk/violations/{id}/acknowledge`, all scoped to `User.RequireUserId()` (depends on T025, T027–T030)
- [ ] T033 [US1] `RiskCheckJob`/`RiskCheckJobScheduler` in `backend/src/FinanceSentry.Modules.Risk/Infrastructure/Jobs/` — daily Hangfire recurring job (after-sync), calls `CheckRiskRulesQuery` handler per active user, writes a `HoldingSnapshot` row per position, and calls `IAlertGeneratorService.GeneratePolicyViolationAlertAsync`/`Resolve...` for each violation/clear transition (mirrors `NetWorthSnapshotJob`) (depends on T029, T016–T018)
- [ ] T034 [US1] Register `RiskCheckJob` recurring schedule in `backend/src/FinanceSentry.API/Program.cs` (`RecurringJob.AddOrUpdate`, daily, after the existing sync jobs)
- [ ] T035 [US1] `GetRiskRulesTool` MCP tool in `backend/src/FinanceSentry.Mcp/Tools/GetRiskRulesTool.cs` (`get_risk_rules`, mirrors `GetIpsTool`) (depends on T027)
- [ ] T036 [US1] `SaveRiskRulesTool` MCP tool in `backend/src/FinanceSentry.Mcp/Tools/SaveRiskRulesTool.cs` (`save_risk_rules`, mirrors `SaveThesisTool`) (depends on T028)
- [ ] T037 [US1] `CheckRiskRulesTool` MCP tool (no-arg branch only for this story) in `backend/src/FinanceSentry.Mcp/Tools/CheckRiskRulesTool.cs` (`check_risk_rules`, mirrors `GetAllocationVsTargetTool`) (depends on T029)
- [ ] T038 [US1] Update `ToolNameContractTests.AgreedToolSurface` in `backend/tests/FinanceSentry.Mcp.Tests/ContractTests/ToolNameContractTests.cs` to add `get_risk_rules`, `save_risk_rules`, `check_risk_rules` (27 → 30)
- [ ] T039 Bump backend version (minor) in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj` per constitution's Versioning Policy

**Checkpoint**: US1 is independently shippable — daily check runs, violations alert once, acknowledgement suppresses re-alerts, `get_risk_rules`/`save_risk_rules`/`check_risk_rules()` (report mode) work end-to-end. This is the MVP.

---

## Phase 4: User Story 2 — Promotion-time gate (Priority: P1)

**Goal**: `check_risk_rules(ticker, amount)` returns `Allowed | Refused` with the violated rule, observed/limit values, and max compliant size — the hard gate 019's `promote_candidate` calls before proposing a position. Explicit overrides are recorded as signals, never silent.

**Independent Test**: Call `check_risk_rules` with a proposal that would push a position over `maxPositionWeightPct` or cash under `minCashBufferPct`; assert `Refused` with the named rule and `maxCompliantSizeUsd`; assert a compliant proposal returns `Allowed` with headroom facts; assert an explicit override flag proceeds and records a signal.

### Tests for User Story 2

- [ ] T040 [P] [US2] Unit tests for the proposal-evaluation branch of `RiskEvaluationService` (or a dedicated `RiskEvaluationService.EvaluateProposal(...)`) in `backend/tests/FinanceSentry.Tests/Risk/RiskEvaluationServiceTests.cs` — proposal within limits → `Allowed` + headroom; proposal breaching `maxPositionWeightPct` → `Refused` + `maxCompliantSizeUsd` computed correctly; proposal breaching `minCashBufferPct` → `Refused`; proposal breaching `maxNewPositionPct` → `Refused`; `TurnoverBudgetPerQuarter` already at cap → `Refused(turnover)` per FR-001b
- [ ] T041 [P] [US2] Unit tests for override recording in `backend/tests/FinanceSentry.Tests/Risk/OverrideSignalTests.cs` — an override flag on a `Refused` verdict proceeds but MUST write a record (signal/Alert) that is queryable afterwards (FR-007, SC-004)
- [ ] T042 [P] [US2] REST/MCP contract test extension in `backend/tests/FinanceSentry.Tests/Risk/RiskRulesContractTests.cs` — `check_risk_rules`/`POST /risk/compliance/check` request schema with `ticker`+`proposedUsd`+optional `override` flag, response schema for both `Allowed` and `Refused`

### Implementation for User Story 2

- [ ] T043 [US2] Extend `CheckRiskRulesQuery` (or add `EvaluateRiskProposalQuery`) to accept an optional `(Ticker, ProposedUsd, Override)` payload in `backend/src/FinanceSentry.Modules.Risk/Application/Queries/CheckRiskRulesQuery.cs`, returning `RiskVerdictDto` when a proposal is present (depends on T029, T040)
- [ ] T044 [US2] Extend `RiskEvaluationService` with proposal-aware evaluation (max compliant size calculation, turnover-budget check via `ITurnoverTracker` from Phase 5 — stub/interface only if Phase 5 not yet done, see dependency note) in `backend/src/FinanceSentry.Modules.Risk/Application/Services/RiskEvaluationService.cs`
- [ ] T045 [US2] Override recording: extend `AcknowledgeViolationCommand`-adjacent flow or add `RecordRiskOverrideCommand` in `backend/src/FinanceSentry.Modules.Risk/Application/Commands/` — writes an Alert (`AlertType.PolicyViolation`, severity Info, message noting "override applied") via `IAlertGeneratorService` (depends on T041, T016–T018)
- [ ] T046 [P] [US2] `RiskVerdictDto` in `backend/src/FinanceSentry.Modules.Risk/API/Responses/RiskVerdictDto.cs`
- [ ] T047 [US2] Extend `RiskController` with `POST /api/v1/risk/compliance/check` accepting the proposal payload, or fold into the existing `GET /risk/compliance` as an optional body — match quickstart.md's example (depends on T043)
- [ ] T048 [US2] Extend `CheckRiskRulesTool` MCP tool to accept optional `(ticker, amount, override)` parameters and return `RiskVerdictDto`-shaped result (depends on T037, T043)

**Checkpoint**: US2 shippable independently on top of US1 — `check_risk_rules(ticker, amount)` is a real, tested gate. 019's `promote_candidate` can now call it (contract test with 019 deferred until 019 ships, per SC-003).

---

## Phase 5: User Story 3 — Adds to broken theses are flagged (Priority: P2)

**Goal**: Detect a quantity increase on a position whose 017 thesis is marked broken, and flag it (`add_to_broken_thesis`) — but only if the increase happens after the break, not before.

**Independent Test**: Seed a thesis marked broken at time T; seed a `HoldingSnapshot` history showing quantity increasing after T → assert an `add_to_broken_thesis` Alert fires. Seed the increase before T → assert no flag.

### Tests for User Story 3

- [ ] T049 [P] [US3] Unit tests for `AddToBrokenThesisDetector` (or a method on `RiskEvaluationService`) in `backend/tests/FinanceSentry.Tests/Risk/AddToBrokenThesisTests.cs` — quantity increase after `BrokenAt` → flagged; quantity increase before `BrokenAt` → not flagged; no broken thesis for the symbol → not flagged; multiple increases after break → flagged once per check run (dedup via existing Alert dedup, not re-invented here)
- [ ] T050 [P] [US3] Unit tests for `TurnoverTracker` in `backend/tests/FinanceSentry.Tests/Risk/TurnoverTrackerTests.cs` — counts distinct quantity-increase events per rolling quarter from `HoldingSnapshot` deltas; quarter rollover resets the count; a decrease is never counted as a trade toward the budget (FR-001b is about discretionary *adds*, per the Barber–Odean framing in ROADMAP)

### Implementation for User Story 3

- [ ] T051 [US3] `ITurnoverTracker`/`TurnoverTracker` in `backend/src/FinanceSentry.Modules.Risk/Application/Services/ITurnoverTracker.cs` + `TurnoverTracker.cs` — pure function over `IReadOnlyList<HoldingSnapshot>` (depends on T050, T007)
- [ ] T052 [US3] Wire `TurnoverTracker` into `RiskEvaluationService`'s proposal path (T044) so `TurnoverBudgetPerQuarter` breaches actually refuse — this task supersedes the stub from T044 if it landed first
- [ ] T053 [US3] Add-to-broken-thesis detection logic in `backend/src/FinanceSentry.Modules.Risk/Application/Services/RiskEvaluationService.cs` (or extracted `AddToBrokenThesisDetector.cs`) — reads `IThesisRepository.FindByTickerAsync` (Research module, read-only per research.md R3) + latest two `HoldingSnapshot` rows for the symbol (depends on T049, T004)
- [ ] T054 [US3] Wire the broken-thesis flag into `RiskCheckJob` (T033) — call the detector per position with a broken thesis, emit `Alert` (reuse `AlertType.PolicyViolation` with `RuleKey = "AddToBrokenThesis"`, or a distinct sub-type string — decide during implementation, document in code comment) via `IAlertGeneratorService`

**Checkpoint**: US3 shippable independently on top of Foundational (does not require US1/US2, only the shared `HoldingSnapshot`/`IThesisRepository` plumbing) — though in practice it ships alongside US1 since both fire from the same daily job.

---

## Phase 6: Polish & Cross-Cutting

**Purpose**: Final gates before merge — not story-specific.

- [ ] T055 [P] Run `dotnet build backend/` and resolve all warnings across every new/modified file (constitution Principle II gate)
- [ ] T056 [P] Run full `backend/tests/FinanceSentry.Tests` and `backend/tests/FinanceSentry.Mcp.Tests` suites — all green, coverage on `RiskEvaluationService`/`TurnoverTracker` well above the 80% target given they're pure functions
- [ ] T057 Update `specs/ROADMAP.md`'s 022 status row from "Spec drafted" to "Implemented" once merged (small doc task, not a code change)
- [ ] T058 Manual verification per `quickstart.md` end-to-end against the real test user's book (the actual ~46% DRAM position) — confirms SC-002 against real data, not just seeded fixtures

---

## Dependencies & Execution Order

**Phase dependencies**:
- Setup (Phase 1) → Foundational (Phase 2): strictly sequential, blocks everything
- Foundational (Phase 2) → User Stories (Phases 3–5): all stories need T005–T020 done
- User Story 1 (Phase 3) has no dependency on US2/US3 beyond Foundational — it is the MVP and should ship first
- User Story 2 (Phase 4) depends on `CheckRiskRulesQuery`/`RiskEvaluationService` existing (US1, T026/T029) — it extends rather than duplicates them
- User Story 3 (Phase 5) depends only on Foundational (`HoldingSnapshot`, `IThesisRepository` reference) — it can be built in parallel with US2 by a second contributor, but in solo development ships after US1 since both read/write via the same `RiskCheckJob`
- Polish (Phase 6) after all desired stories are done

**Within each story**: tests (written first, must fail) → services → commands/queries → controllers/MCP tools → job wiring, in that order per task numbering above.

**Parallel opportunities**:
- All `[P]` tasks within Phase 2 (T005–T010, T013, T019–T020) touch different files and can run concurrently once the module skeleton (T001–T004) exists
- All test-writing tasks within a story phase (e.g. T021–T025) are `[P]` — different test files, no shared state
- US2 (Phase 4) and US3 (Phase 5) can be implemented in parallel by two contributors once US1's Foundational pieces (T026, T029, T033) land, since they touch largely disjoint files (`RiskVerdictDto`/proposal logic vs. `TurnoverTracker`/broken-thesis detector) — the one shared file is `RiskEvaluationService.cs`, so coordinate on that file specifically

## MVP Scope

**Minimum viable slice**: Phase 1 (Setup) + Phase 2 (Foundational) + Phase 3 (User Story 1) = a working daily policy check with acknowledgement, three read/write MCP tools (`get_risk_rules`, `save_risk_rules`, `check_risk_rules` in report mode), and the seeded 46%-DRAM scenario passing (SC-002). This alone delivers the core spec promise ("the book *starts* in violation; the system manages remediation, not nag daily").

**Full feature**: adds Phase 4 (US2 — the actual gate 019 depends on, SC-003) and Phase 5 (US3 — the averaging-down-on-broken-thesis catch, and the turnover budget that's "the single highest-evidence finding" per ROADMAP's gap-check). Both are P1/P2 respectively and should ship in the same PR cycle as US1 given the small size of this feature, but are structured so US1 alone is a valid, revertible increment if time runs short.
