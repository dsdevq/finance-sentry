# Tasks: Alerts System (012)

**Input**: Design documents from `/specs/012-alerts-system/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: Contract tests for all REST endpoints (mandatory per constitution). Unit tests for `AlertGeneratorService` business logic (deduplication, auto-resolution). No E2E tests explicitly requested — Playwright QA at the end per CLAUDE.md.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story this task belongs to (US1–US4)
- Exact file paths in every description

---

## Phase 1: Setup (New Module Scaffolding)

**Purpose**: Create the `FinanceSentry.Modules.Alerts` project and wire it into the solution.

- [ ] T001 Create `FinanceSentry.Modules.Alerts` csproj and add it to the solution: `backend/src/FinanceSentry.Modules.Alerts/FinanceSentry.Modules.Alerts.csproj` + add references to `FinanceSentry.Core` and `FinanceSentry.Infrastructure`
- [ ] T002 Add project reference from `backend/src/FinanceSentry.API/FinanceSentry.API.csproj` → `FinanceSentry.Modules.Alerts`
- [ ] T003 [P] Create module marker class `backend/src/FinanceSentry.Modules.Alerts/AlertsModule.cs`
- [ ] T004 [P] Create `backend/src/FinanceSentry.Modules.Alerts/Infrastructure/Persistence/AlertsDbContextFactory.cs` (IDesignTimeDbContextFactory, mirrors BankSyncDbContextFactory)

**Checkpoint**: `dotnet build backend/` passes with zero warnings.

---

## Phase 2: Foundational (Core Interface + Domain Entity + DB)

**Purpose**: Domain entity, repository interface, DbContext, and migration. Blocks all user stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T005 Define `IAlertGeneratorService` interface in `backend/src/FinanceSentry.Core/Interfaces/IAlertGeneratorService.cs` — five methods: GenerateLowBalanceAlertAsync, ResolveLowBalanceAlertAsync, GenerateSyncFailureAlertAsync, ResolveSyncFailureAlertAsync, GenerateUnusualSpendAlertAsync (signatures from data-model.md)
- [ ] T006 [P] Create `AlertType` string-constant enum: `backend/src/FinanceSentry.Modules.Alerts/Domain/AlertType.cs` (LowBalance, SyncFailure, UnusualSpend)
- [ ] T007 [P] Create `AlertSeverity` string-constant enum: `backend/src/FinanceSentry.Modules.Alerts/Domain/AlertSeverity.cs` (Error, Warning, Info)
- [ ] T008 Create `Alert` domain entity: `backend/src/FinanceSentry.Modules.Alerts/Domain/Alert.cs` — all fields from data-model.md (Id, UserId, Type, Severity, Title, Message, ReferenceId, ReferenceLabel, IsRead, IsResolved, IsDismissed, CreatedAt, UpdatedAt, ResolvedAt)
- [ ] T009 Create `IAlertRepository` interface: `backend/src/FinanceSentry.Modules.Alerts/Domain/Repositories/IAlertRepository.cs` — methods: GetPagedAsync, GetUnreadCountAsync, FindActiveAsync (dedup check), MarkReadAsync, MarkAllReadAsync, DismissAsync, ResolveAsync, PurgeOldAsync
- [ ] T010 Create `AlertsDbContext`: `backend/src/FinanceSentry.Modules.Alerts/Infrastructure/Persistence/AlertsDbContext.cs` — DbSet<Alert>, OnModelCreating with all indexes from data-model.md (idx_alert_user_created, idx_alert_dedup partial unique, idx_alert_purge)
- [ ] T011 Create `AlertRepository`: `backend/src/FinanceSentry.Modules.Alerts/Infrastructure/Persistence/Repositories/AlertRepository.cs` — implement all IAlertRepository methods using AlertsDbContext
- [ ] T012 Create EF Core migration M001: `backend/src/FinanceSentry.Modules.Alerts/Migrations/` — run `dotnet ef migrations add M001_InitialSchema --project ... --context AlertsDbContext`
- [ ] T013 Register Alerts module in `backend/src/FinanceSentry.API/Program.cs`: add AlertsModule assembly to AddCqrs, register AlertsDbContext with Npgsql, register IAlertGeneratorService→AlertGeneratorService, add migration block, bump API version 0.7.0 → 0.8.0 in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj`
- [ ] T014 Implement `AlertGeneratorService`: `backend/src/FinanceSentry.Modules.Alerts/Application/Services/AlertGeneratorService.cs` — implements IAlertGeneratorService; deduplication via FindActiveAsync before insert; auto-resolution sets IsResolved=true, ResolvedAt; all five methods
- [ ] T015 Unit test `AlertGeneratorService` deduplication and auto-resolution: `backend/tests/FinanceSentry.Modules.Alerts.Tests/AlertGeneratorServiceTests.cs` — test: no duplicate alert created when unresolved exists; low-balance alert resolved when balance recovers; sync-failure resolved on success

**Checkpoint**: `dotnet build backend/` zero warnings. Migration applies cleanly. `IAlertGeneratorService` injectable.

---

## Phase 3: User Story 1 — View and Dismiss Alerts (Priority: P1) 🎯 MVP

**Goal**: Authenticated user can view, filter, mark read, mark all read, and dismiss alerts via working REST endpoints and a real-data Angular store.

**Independent Test**: Navigate to `/alerts`; empty state shown with zero alerts. Call `POST /api/v1/alerts` (manual seed) then reload — alert appears. Mark as read → persists on reload. Dismiss → removed from list. Sidebar badge shows accurate unread count.

### Contract Tests for US1

- [ ] T016 [P] [US1] Contract test for `GET /api/v1/alerts`: `backend/tests/FinanceSentry.Modules.Alerts.Tests/Contracts/GetAlertsContractTest.cs` — validates 200 response shape (items[], totalCount, unreadCount, page, pageSize, totalPages), 401 on missing token
- [ ] T017 [P] [US1] Contract test for `GET /api/v1/alerts/unread-count`: `backend/tests/FinanceSentry.Modules.Alerts.Tests/Contracts/GetUnreadCountContractTest.cs` — validates 200 `{ count: N }` shape
- [ ] T018 [P] [US1] Contract test for `PATCH /api/v1/alerts/{id}/read`: `backend/tests/FinanceSentry.Modules.Alerts.Tests/Contracts/MarkAlertReadContractTest.cs` — validates 204 on valid id, 404 on unknown id, 401 on missing token
- [ ] T019 [P] [US1] Contract test for `PATCH /api/v1/alerts/read-all`: `backend/tests/FinanceSentry.Modules.Alerts.Tests/Contracts/MarkAllReadContractTest.cs` — validates 204 on success, 401 on missing token
- [ ] T020 [P] [US1] Contract test for `DELETE /api/v1/alerts/{id}`: `backend/tests/FinanceSentry.Modules.Alerts.Tests/Contracts/DismissAlertContractTest.cs` — validates 204 on valid id, 404 on unknown id, 401 on missing token

### Backend Implementation for US1

- [ ] T021 [P] [US1] Create response DTOs: `backend/src/FinanceSentry.Modules.Alerts/API/Responses/AlertDto.cs`, `AlertsPageResponse.cs`, `UnreadCountResponse.cs`
- [ ] T022 [P] [US1] Implement `GetAlertsQuery` + handler: `backend/src/FinanceSentry.Modules.Alerts/Application/Queries/GetAlertsQuery.cs` — parameters: UserId, Filter, Page, PageSize; handler calls IAlertRepository.GetPagedAsync
- [ ] T023 [P] [US1] Implement `GetUnreadCountQuery` + handler: `backend/src/FinanceSentry.Modules.Alerts/Application/Queries/GetUnreadCountQuery.cs` — calls IAlertRepository.GetUnreadCountAsync
- [ ] T024 [P] [US1] Implement `MarkAlertReadCommand` + handler: `backend/src/FinanceSentry.Modules.Alerts/Application/Commands/MarkAlertReadCommand.cs` — verifies ownership, calls IAlertRepository.MarkReadAsync, returns 404 if not found
- [ ] T025 [P] [US1] Implement `MarkAllAlertsReadCommand` + handler: `backend/src/FinanceSentry.Modules.Alerts/Application/Commands/MarkAllAlertsReadCommand.cs` — bulk update for user's unread alerts
- [ ] T026 [P] [US1] Implement `DismissAlertCommand` + handler: `backend/src/FinanceSentry.Modules.Alerts/Application/Commands/DismissAlertCommand.cs` — verifies ownership, sets IsDismissed=true, returns 404 if not found
- [ ] T027 [US1] Implement `AlertsController`: `backend/src/FinanceSentry.Modules.Alerts/API/Controllers/AlertsController.cs` — all 5 endpoints wired to CQRS handlers; route `/api/v1/alerts`; extract UserId from JWT claims

### Frontend Implementation for US1

- [ ] T028 [P] [US1] Update `alert.model.ts`: `frontend/src/app/modules/alerts/models/alert/alert.model.ts` — add `dismissed: boolean`, `resolved: boolean`, `resolvedAt: number | null` fields; rename `body` → `message` to match API contract
- [ ] T029 [P] [US1] Create `alerts.service.ts`: `frontend/src/app/modules/alerts/services/alerts.service.ts` — HTTP methods: getAlerts(filter, page, pageSize), getUnreadCount(), markRead(id), markAllRead(), dismiss(id)
- [ ] T030 [US1] Update `alerts.state.ts`: `frontend/src/app/modules/alerts/store/alerts/alerts.state.ts` — add `totalCount: number`, `currentPage: number`, `pageSize: number` to AlertsState and initial state
- [ ] T031 [US1] Update `alerts.methods.ts`: `frontend/src/app/modules/alerts/store/alerts/alerts.methods.ts` — add `setTotalCount`, `setPage`, `setPageSize` patchState mutations
- [ ] T032 [US1] Update `alerts.effects.ts`: `frontend/src/app/modules/alerts/store/alerts/alerts.effects.ts` — replace ALERT_MOCK_DATA with AlertsService HTTP calls; implement load, markRead, markAllRead, dismiss rxMethods; call getUnreadCount separately for badge refresh
- [ ] T033 [US1] Update `alerts.store.ts`: `frontend/src/app/modules/alerts/store/alerts/alerts.store.ts` — add `{providedIn: 'root'}` to make store root-scoped (required for sidebar badge)
- [ ] T034 [US1] Update `alerts.component.ts`: `frontend/src/app/modules/alerts/pages/alerts/alerts.component.ts` — remove `providers: [AlertsStore]` if present (now root-scoped); bind all actions to store methods
- [ ] T035 [US1] Update `app-shell.component.ts`: `frontend/src/app/core/shell/app-shell.component.ts` — inject AlertsStore, add unread count badge to the `Alerts` NavItem's Bell icon (bind `alertsStore.unreadCount()`)
- [ ] T036 [P] [US1] Add `ALERT_NOT_FOUND` to error registry: `frontend/src/app/core/errors/error-messages.registry.ts`
- [ ] T037 [P] [US1] Bump frontend version `0.7.0 → 0.8.0` in `frontend/package.json`

**Checkpoint**: `GET /api/v1/alerts` returns 200 with empty list. Angular alerts page loads with empty state. Sidebar badge shows 0. All 5 endpoints return correct status codes.

---

## Phase 4: User Story 2 — Low Balance Alert Generation (Priority: P2)

**Goal**: System automatically creates a low-balance alert when an account syncs below the user's threshold, and auto-resolves it when balance recovers.

**Independent Test**: Set `LowBalanceThreshold=9999` for test user. Trigger a sync. Verify an alert appears on `GET /api/v1/alerts` with `type=LowBalance`. Trigger another sync with the account now above threshold → alert marked resolved.

### Implementation for US2

- [ ] T038 Extend `AccountSyncCompletedEvent`: `backend/src/FinanceSentry.Modules.BankSync/Domain/Events/AccountSyncCompletedEvent.cs` — add `UserId` (string), `Provider` (string), `BalanceAfterSync` (decimal?), `ErrorCode` (string?) to the record
- [ ] T039 Update all publish calls for `AccountSyncCompletedEvent` in `backend/src/FinanceSentry.Modules.BankSync/Application/Services/ScheduledSyncService.cs` — pass new fields (UserId from account, Provider from account.Provider, BalanceAfterSync from fetched balance, ErrorCode from exception)
- [ ] T040 Add `IAlertGeneratorService` injection and call sites to `ScheduledSyncService`: `backend/src/FinanceSentry.Modules.BankSync/Application/Services/ScheduledSyncService.cs` — after successful sync call `GenerateLowBalanceAlertAsync` (if balance < threshold AND LowBalanceAlerts=true) and `ResolveLowBalanceAlertAsync` (if balance ≥ threshold); read threshold from `ApplicationUser` via UserManager
- [ ] T041 Add failure call site to `ScheduledSyncService` — in the exception handler call `GenerateSyncFailureAlertAsync` (if SyncFailureAlerts=true); on success call `ResolveSyncFailureAlertAsync`
- [ ] T042 Unit test low-balance alert lifecycle in `AlertGeneratorService`: `backend/tests/FinanceSentry.Modules.Alerts.Tests/AlertGeneratorServiceTests.cs` — alert created when balance < threshold; no duplicate when already active; alert resolved when balance ≥ threshold

**Checkpoint**: After a sync with balance below threshold, `GET /api/v1/alerts` returns a LowBalance alert. After recovery sync, alert `isResolved=true`.

---

## Phase 5: User Story 3 — Sync Failure Alert Generation (Priority: P2)

**Goal**: System creates a sync-failure alert when any provider sync job fails, and auto-resolves it when sync succeeds. Covers bank (Plaid/Monobank), crypto (Binance), and brokerage (IBKR).

**Independent Test**: For any provider, simulate sync failure (bad credentials or offline). Verify a SyncFailure alert appears. Restore credentials and sync successfully → alert auto-resolved.

### Implementation for US3

- [ ] T043 Inject `IAlertGeneratorService` into `BinanceSyncJob`: `backend/src/FinanceSentry.Modules.CryptoSync/Infrastructure/Jobs/BinanceSyncJob.cs` — in the per-user catch block call `GenerateSyncFailureAlertAsync`; after success call `ResolveSyncFailureAlertAsync`; wrap with null-check: only if SyncFailureAlerts is enabled (requires fetching user preference via UserManager or passing it through credential)
- [ ] T044 Inject `IAlertGeneratorService` into `IBKRSyncJob`: `backend/src/FinanceSentry.Modules.BrokerageSync/Infrastructure/Jobs/IBKRSyncJob.cs` — same pattern as T043
- [ ] T045 Unit test sync-failure alert lifecycle in `AlertGeneratorService`: `backend/tests/FinanceSentry.Modules.Alerts.Tests/AlertGeneratorServiceTests.cs` — failure alert created; no duplicate on repeated failures; resolved after success

**Checkpoint**: Manual Binance/IBKR credential invalidation triggers SyncFailure alert. Restore → alert resolved.

---

## Phase 6: User Story 4 — Unusual Spend Alert Generation (Priority: P3)

**Goal**: Nightly job detects when current-month spend in a category exceeds 2× its 3-month rolling average and emits an alert, for users with ≥ 3 months of history.

**Independent Test**: Seed transactions with 3+ months of history in a category where current month is 3× average. Run the job manually. Verify an `UnusualSpend` alert appears.

### Implementation for US4

- [ ] T046 Implement `UnusualSpendDetectionJob`: `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Jobs/UnusualSpendDetectionJob.cs` — queries `Transactions` grouped by UserId and MerchantCategory; computes 3-month rolling average; identifies categories where current month > 2× average and user has ≥ 3 months of history; calls `IAlertGeneratorService.GenerateUnusualSpendAlertAsync`; skips users without sufficient history
- [ ] T047 Register `UnusualSpendDetectionJob` in Hangfire: `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Jobs/HangfireSetup.cs` — add recurring job `unusual-spend-detection` with `Cron.Daily()` (runs nightly)
- [ ] T048 Unit test unusual-spend detection logic in `UnusualSpendDetectionJob`: `backend/tests/FinanceSentry.Modules.BankSync.Tests/UnusualSpendDetectionJobTests.cs` — correctly identifies 2× threshold breach; skips users with < 3 months history; no duplicate alert when one already active

**Checkpoint**: Running the Hangfire job manually produces UnusualSpend alerts for qualifying users. No duplicates on re-run.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Purge job, edge-case handling, and final validation.

- [ ] T049 Implement `AlertPurgeJob`: `backend/src/FinanceSentry.Modules.Alerts/Infrastructure/Jobs/AlertPurgeJob.cs` — deletes dismissed/resolved alerts older than 90 days via IAlertRepository.PurgeOldAsync; respects FR-012
- [ ] T050 Register `AlertPurgeJob` in `AlertsHangfireSetup` and wire in `Program.cs`: `backend/src/FinanceSentry.Modules.Alerts/Infrastructure/Jobs/AlertsHangfireSetup.cs` — recurring job `alert-purge`, `Cron.Monthly()`
- [ ] T051 [P] Handle disconnected-account alert deletion: add `OnAccountDisconnected` call site in the account disconnect flow (`backend/src/FinanceSentry.Modules.BankSync/Application/Commands/`) — call `IAlertRepository.DeleteByReferenceIdAsync(accountId)` (add method to interface)
- [ ] T052 [P] Run `dotnet build backend/` and resolve all remaining warnings. Run `npx eslint frontend/src/app/modules/alerts/ --fix` and fix all errors.
- [ ] T053 Playwright QA: start full Docker stack; navigate to `/alerts` as test user (test@gmail.com); verify empty state → mark-all-read → dismiss flows work end-to-end; verify sidebar badge updates without page reload

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundation)**: Depends on Phase 1 — **BLOCKS all user story phases**
- **Phase 3 (US1)**: Depends on Phase 2 completion — backend endpoints + frontend wiring
- **Phase 4 (US2)**: Depends on Phase 2 (AlertGeneratorService must exist)
- **Phase 5 (US3)**: Depends on Phase 2; may share T041 with Phase 4 (ScheduledSyncService changes are sequential)
- **Phase 6 (US4)**: Depends on Phase 2; fully independent of US1–US3
- **Phase 7 (Polish)**: Depends on Phases 3–6

### User Story Dependencies

- **US1 (P1)**: Depends on Foundation (Phase 2). No dependency on US2–US4 — works with empty alert list.
- **US2 (P2)**: Depends on Foundation. No dependency on US1 (alert generation is backend-only).
- **US3 (P2)**: Depends on Foundation. T041 must follow T039–T040 (same file). Independent of US1.
- **US4 (P3)**: Fully independent — different job, different module section.

### Within Each Phase

- Contract tests (T016–T020) run in parallel
- Backend response DTOs, queries, and commands (T021–T026) run in parallel
- Frontend tasks: T028–T029 parallel; T030–T034 sequential (store composition); T035–T037 parallel
- T038 must precede T039–T041 (event extension before publish call sites)

---

## Parallel Execution Examples

### Phase 2 parallelism
```
Parallel group A (can run simultaneously):
  T006 AlertType enum
  T007 AlertSeverity enum
  T005 IAlertGeneratorService (Core)
Depends on A:
  T008 Alert entity
  T010 AlertsDbContext
Depends on T008+T009:
  T011 AlertRepository
  T012 Migration
```

### Phase 3 backend parallelism
```
Parallel group (can run simultaneously after T008 exists):
  T016 Contract test GET /alerts
  T017 Contract test GET /unread-count
  T018 Contract test PATCH /{id}/read
  T019 Contract test PATCH /read-all
  T020 Contract test DELETE /{id}
  T021 Response DTOs
  T022 GetAlertsQuery
  T023 GetUnreadCountQuery
  T024 MarkAlertReadCommand
  T025 MarkAllAlertsReadCommand
  T026 DismissAlertCommand
Depends on all above:
  T027 AlertsController
```

### Phase 3 frontend parallelism
```
Parallel group:
  T028 alert.model.ts update
  T029 alerts.service.ts (new)
  T036 error-messages.registry.ts
  T037 package.json version bump
Depends on T028+T029:
  T030 → T031 → T032 → T033 → T034 (sequential store files)
  T035 app-shell.component.ts (depends on T033)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup
2. Phase 2: Foundation — entity, DB, AlertGeneratorService
3. Phase 3: US1 — all endpoints + frontend wiring
4. **VALIDATE**: Empty alerts page works; all 5 endpoints return correct shapes; sidebar badge shows 0
5. Deploy/demo — users can see the alerts page; no alerts generated yet

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready (AlertGeneratorService injectable)
2. Phase 3 → US1 complete → alerts page works with real API (empty)
3. Phase 4 → US2 → low-balance alerts appear after sync
4. Phase 5 → US3 → sync-failure alerts appear
5. Phase 6 → US4 → unusual-spend nightly detection
6. Phase 7 → purge job, edge cases, QA

---

## Task Summary

| Phase | Tasks | Parallel [P] |
|---|---|---|
| Phase 1: Setup | T001–T004 | T003, T004 |
| Phase 2: Foundation | T005–T015 | T006, T007, T005 |
| Phase 3: US1 | T016–T037 | T016–T026, T028, T029, T036, T037 |
| Phase 4: US2 | T038–T042 | — |
| Phase 5: US3 | T043–T045 | T043, T044 |
| Phase 6: US4 | T046–T048 | — |
| Phase 7: Polish | T049–T053 | T051, T052 |
| **Total** | **53 tasks** | **~22 parallelizable** |

---

## Notes

- [P] tasks operate on different files with no shared dependencies — safe to run concurrently
- [US*] label maps each task to its user story for traceability
- Constitution mandates contract tests for every REST endpoint (T016–T020) — mandatory
- `AlertGeneratorService` deduplication logic (T014) is the most critical business logic — test thoroughly (T015)
- T038–T039 modify `AccountSyncCompletedEvent` — verify all existing handlers compile after the change (`dotnet build` gate)
- Run `npx eslint <file> --fix` after every Angular file change; run `dotnet build backend/` after every C# change
