# Tasks: Net Worth History Chart

**Input**: Design documents from `/specs/015-net-worth-history/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Contract tests are mandatory per constitution for every new REST endpoint. Unit tests included for core business logic.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 = View Net Worth Over Time | US2 = Historical Accuracy | US3 = Date Range Selection

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create new module skeleton and Core interface before any story work begins.

- [ ] T001 Create `FinanceSentry.Modules.NetWorthHistory` classlib project with EF Core 9 + Npgsql + MediatR packages in `backend/src/FinanceSentry.Modules.NetWorthHistory/FinanceSentry.Modules.NetWorthHistory.csproj`
- [ ] T002 Add project references: NetWorthHistory → Core, NetWorthHistory → Infrastructure; API → NetWorthHistory in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj`
- [ ] T003 Define `INetWorthSnapshotService` interface and `NetWorthSnapshotData` record in `backend/src/FinanceSentry.Core/Interfaces/INetWorthSnapshotService.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entity, persistence layer, and service implementation — must be complete before any user story.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T004 [P] Create `NetWorthSnapshot` domain entity (immutable init-only properties, all fields from data-model.md) in `backend/src/FinanceSentry.Modules.NetWorthHistory/Domain/NetWorthSnapshot.cs`
- [ ] T005 [P] Create `INetWorthSnapshotRepository` with `PersistAsync`, `ExistsAsync`, `GetByUserIdAsync` methods in `backend/src/FinanceSentry.Modules.NetWorthHistory/Domain/Repositories/INetWorthSnapshotRepository.cs`
- [ ] T006 Create `NetWorthHistoryDbContext` with `NetWorthSnapshot` DbSet, entity config (table name, indexes, unique constraint on `user_id + snapshot_date`) in `backend/src/FinanceSentry.Modules.NetWorthHistory/Infrastructure/Persistence/NetWorthHistoryDbContext.cs`
- [ ] T007 Create `NetWorthHistoryDbContextFactory` (implements `IDesignTimeDbContextFactory<NetWorthHistoryDbContext>`) in `backend/src/FinanceSentry.Modules.NetWorthHistory/Infrastructure/Persistence/NetWorthHistoryDbContextFactory.cs`
- [ ] T008 Run EF migration `M001_InitialSchema` for `net_worth_snapshots` table with `idx_net_worth_snapshot_user_date` and unique `idx_net_worth_snapshot_user_date_unique` indexes under `backend/src/FinanceSentry.Modules.NetWorthHistory/Migrations/`
- [ ] T009 Implement `NetWorthSnapshotRepository` (upsert via `ExistsAsync` guard — no-op if exists; `GetByUserIdAsync` with range filtering) in `backend/src/FinanceSentry.Modules.NetWorthHistory/Infrastructure/Persistence/Repositories/NetWorthSnapshotRepository.cs`
- [ ] T010 Implement `NetWorthSnapshotService` (implements `INetWorthSnapshotService`; `PersistSnapshotAsync` delegates to repository; `HasSnapshotForCurrentMonthAsync` checks month-end date) in `backend/src/FinanceSentry.Modules.NetWorthHistory/Application/Services/NetWorthSnapshotService.cs`
- [ ] T011 Create `NetWorthHistoryModule.cs` boilerplate (empty static class or marker) in `backend/src/FinanceSentry.Modules.NetWorthHistory/NetWorthHistoryModule.cs`
- [ ] T012 Register `NetWorthHistoryDbContext` (UseNpgsql), `INetWorthSnapshotService → NetWorthSnapshotService` (scoped), CQRS assembly, and migration block in `backend/src/FinanceSentry.API/Program.cs`

**Checkpoint**: `dotnet build backend/` passes with zero warnings; `net_worth_snapshots` table created on migration.

---

## Phase 3: User Story 1 — View Net Worth Over Time on Dashboard (Priority: P1) 🎯 MVP

**Goal**: Replace hardcoded mock chart data with real monthly snapshots from a Hangfire job and a REST endpoint.

**Independent Test**: Trigger `NetWorthSnapshotJob` via Hangfire dashboard → `GET /api/v1/net-worth/history` returns one snapshot → Dashboard chart renders the real data point. Empty state shown before job runs.

### Contract Test

- [ ] T013 [P] [US1] Contract test: `GET /api/v1/net-worth/history` returns `200` with `{ snapshots: [], hasHistory: false, range: "1y" }` when no snapshots exist; returns correct shape when snapshots exist; `400 INVALID_RANGE` on bad `range` param in `backend/tests/FinanceSentry.Modules.NetWorthHistory.Tests/Contracts/GetNetWorthHistoryContractTests.cs`

### Backend — Data Generation

- [ ] T014 [US1] Implement `NetWorthSnapshotJob` (enumerate active users via `IBankAccountRepository`; for each user sum banking via `IAggregationService.GetTotalNetWorthUsdAsync`, crypto via `ICryptoHoldingsReader`, brokerage via `IBrokerageHoldingsReader`; compute month-end `DateOnly`; call `INetWorthSnapshotService.PersistSnapshotAsync`) in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Jobs/NetWorthSnapshotJob.cs`
- [ ] T015 [US1] Implement `FirstSyncSnapshotTrigger` (`INotificationHandler<AccountSyncCompletedEvent>`; resolves `UserId` from `AccountId` via `IBankAccountRepository`; if `status == "success"` and `!HasSnapshotForCurrentMonthAsync(userId)`: enqueues `NetWorthSnapshotJob` via `IBackgroundJobClient`) in `backend/src/FinanceSentry.Modules.BankSync/Application/EventHandlers/FirstSyncSnapshotTrigger.cs`
- [ ] T016 [US1] Register `net-worth-snapshot` recurring job with cron `"0 1 L * *"` in `SyncScheduler.ScheduleAllActiveAccounts` in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Jobs/HangfireSetup.cs`

### Backend — REST Endpoint

- [ ] T017 [P] [US1] Create `NetWorthSnapshotDto` (snapshotDate, bankingTotal, brokerageTotal, cryptoTotal, totalNetWorth, currency) and `NetWorthHistoryResponse` (snapshots, hasHistory, range) in `backend/src/FinanceSentry.Modules.NetWorthHistory/API/Responses/`
- [ ] T018 [US1] Create `GetNetWorthHistoryQuery` record (UserId, Range) and `GetNetWorthHistoryQueryHandler` (fetches from repository, maps to DTOs, sets `hasHistory`) in `backend/src/FinanceSentry.Modules.NetWorthHistory/Application/Queries/GetNetWorthHistoryQuery.cs`
- [ ] T019 [US1] Create `NetWorthHistoryController` (`GET /api/v1/net-worth/history?range=`; extracts userId from JWT; dispatches `GetNetWorthHistoryQuery`; validates `range` enum; returns `400 INVALID_RANGE` on bad value) in `backend/src/FinanceSentry.Modules.NetWorthHistory/API/Controllers/NetWorthHistoryController.cs`

### Frontend — Model + Service

- [ ] T020 [P] [US1] Add `NetWorthSnapshotDto`, `NetWorthHistoryResponse`, and `HistoryRange` type (`'3m' | '6m' | '1y' | 'all'`) to `frontend/src/app/modules/bank-sync/models/dashboard/dashboard.model.ts`
- [ ] T021 [P] [US1] Add `getNetWorthHistory(range: HistoryRange): Observable<NetWorthHistoryResponse>` to `frontend/src/app/modules/bank-sync/services/bank-sync.service.ts`

### Frontend — Store Wiring

- [ ] T022 [US1] Add `netWorthHistory: NetWorthSnapshotDto[] | null`, `historyRange: HistoryRange`, `historyHasHistory: boolean`, `historyLoading: boolean`, `historyError: string | null` to `DashboardState` and `initialDashboardState` in `frontend/src/app/modules/bank-sync/store/dashboard/dashboard.state.ts`
- [ ] T023 [US1] Add `setNetWorthHistory`, `setHistoryRange`, `setHistoryLoading`, `setHistoryError`, `setHistoryHasHistory` patchState mutations in `frontend/src/app/modules/bank-sync/store/dashboard/dashboard.methods.ts`
- [ ] T024 [US1] Update `netWorthHistoryData` computed to map `store.netWorthHistory()` to `ChartPoint[]` (remove mock import); add `isHistoryLoading` and `historyErrorMessage` computeds in `frontend/src/app/modules/bank-sync/store/dashboard/dashboard.computed.ts`
- [ ] T025 [US1] Add `loadNetWorthHistory` rxMethod (calls `bankSyncService.getNetWorthHistory(store.historyRange())`; sets loading/error/data via methods; trigger on `onInit` and whenever `historyRange` changes via `toObservable`) in `frontend/src/app/modules/bank-sync/store/dashboard/dashboard.effects.ts`
- [ ] T026 [US1] Update `dashboard.component.ts`: remove `NET_WORTH_HISTORY_MOCK` import; bind `[data]="store.netWorthHistoryData()"` to the chart; add empty-state block when `!store.netWorthHistoryHasHistory()` in `frontend/src/app/modules/bank-sync/pages/dashboard/dashboard.component.ts`

### Version Bumps

- [ ] T027 [P] [US1] Bump backend version `0.7.0` → `0.8.0` in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj`
- [ ] T028 [P] [US1] Bump frontend version `0.7.0` → `0.8.0` in `frontend/package.json`

**Checkpoint**: Dashboard chart renders at least one real snapshot after job is manually triggered; empty state shown before job runs; no mock data in production build.

---

## Phase 4: User Story 2 — Historical Accuracy After Account Changes (Priority: P2)

**Goal**: Ensure past snapshots remain unchanged when accounts are connected/disconnected.

**Independent Test**: Trigger job → note snapshot totals → disconnect a bank account → verify stored snapshot totals are unchanged via `GET /api/v1/net-worth/history`.

> US2 is inherently enforced by the immutable snapshot design (init-only entity, no-op on duplicate). The single task here validates that guarantee.

- [ ] T029 [US2] Unit test: assert `NetWorthSnapshotService.PersistSnapshotAsync` is a no-op (no INSERT) when a snapshot already exists for the same `(userId, snapshotDate)` — verifies immutability guarantee in `backend/tests/FinanceSentry.Modules.NetWorthHistory.Tests/Unit/NetWorthSnapshotServiceTests.cs`

**Checkpoint**: Past snapshot totals are immutable; no retroactive modification possible.

---

## Phase 5: User Story 3 — Chart Date Range Selection (Priority: P3)

**Goal**: Let users toggle the chart between 3 months, 6 months, 1 year, and all-time views.

**Independent Test**: With 4+ months of snapshots — select "3m" → chart shows 3 data points; select "all" → chart shows all available points.

- [ ] T030 [US3] Add range selector toggle group (3m / 6m / 1y / all buttons using `cmn-button`) to `dashboard.component.ts`; bind active state to `store.historyRange()`; call `store.setHistoryRange(range)` on selection in `frontend/src/app/modules/bank-sync/pages/dashboard/dashboard.component.ts`
- [ ] T031 [US3] Add `INVALID_RANGE: 'Invalid date range selection.'` to `frontend/src/app/core/errors/error-messages.registry.ts`

**Checkpoint**: Range selector visible on dashboard; switching range updates chart data points.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T032 [P] Unit test `NetWorthSnapshotJob` core logic: given mocked balance readers returning fixed values, assert correct `PersistSnapshotAsync` call with summed totals in `backend/tests/FinanceSentry.Modules.BankSync.Tests/Jobs/NetWorthSnapshotJobTests.cs`
- [ ] T033 Run quickstart.md manual QA: start Docker stack; trigger `net-worth-snapshot` job via Hangfire dashboard; verify snapshot in DB; confirm `GET /api/v1/net-worth/history` returns the snapshot; confirm dashboard chart renders real data; verify empty state on new test user

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user stories
- **Phase 3 (US1)**: Depends on Phase 2 — primary delivery
- **Phase 4 (US2)**: Depends on Phase 2 (service must exist to test); independent of US1/US3
- **Phase 5 (US3)**: Depends on Phase 3 (store `historyRange` state must exist)
- **Phase 6 (Polish)**: Depends on Phases 3–5

### User Story Dependencies

- **US1 (P1)**: Foundation complete → full backend + frontend implementation
- **US2 (P2)**: Foundation complete → single unit test; independent of US1 and US3
- **US3 (P3)**: US1 store state (`historyRange`) must exist before range selector UI can bind

### Within US1

- T014 (job) and T015 (trigger) can start after T012 (Program.cs registration)
- T017 (DTOs) and T018 (query handler) can run in parallel
- Frontend T020–T021 can run in parallel with backend T013–T019
- T022 → T023 → T024 → T025 → T026 must run sequentially (each adds to the store)
- T027–T028 (version bumps) can run in parallel with any task

### Parallel Opportunities

```bash
# Phase 2 parallel group
T004 (NetWorthSnapshot entity) || T005 (INetWorthSnapshotRepository)

# US1 backend parallel group (after T012)
T013 (SnapshotJob) || T014 (FirstSyncTrigger) || T017 (response DTOs) || T013 contract test

# US1 frontend parallel group (independent of backend)
T020 (dashboard.model.ts) || T021 (bank-sync.service.ts)

# Version bumps (parallel with any task)
T027 (backend version) || T028 (frontend version)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T012)
3. Complete Phase 3: User Story 1 (T013–T028)
4. **STOP and VALIDATE**: Trigger Hangfire job → confirm chart shows real data
5. Deploy/demo

### Incremental Delivery

1. Phase 1 + 2 → foundation ready
2. US1 (Phase 3) → chart shows real snapshots (MVP)
3. US2 (Phase 4) → immutability verified
4. US3 (Phase 5) → range selector added
5. Polish (Phase 6) → unit tests + QA pass

---

## Notes

- [P] tasks = different files, no dependencies on each other
- [Story] label maps to spec.md user story
- Snapshots are immutable by design (unique constraint + no-op on duplicate) — US2 requires no new implementation, only validation
- `historyRange` default is `'1y'`; frontend requests with default on `onInit`
- Hangfire cron `"0 1 L * *"` = 1 AM on the last day of each month (GNU/Quartz `L` extension)
- Run `npx eslint <file>` after every Angular `.ts` change; run `dotnet build backend/` after every `.cs` change
