# Tasks: Budgets (013)

**Input**: Design documents from `/specs/013-budgets/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: Contract tests for all REST endpoints (mandatory per constitution). Unit tests for `CategoryNormalizationService` (core business logic). No E2E tests explicitly requested — Playwright QA at the end per CLAUDE.md.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story this task belongs to (US1–US3)
- Exact file paths in every description

---

## Phase 1: Setup (New Module Scaffolding)

**Purpose**: Create the `FinanceSentry.Modules.Budgets` project and wire it into the solution.

- [X] T001 Create `FinanceSentry.Modules.Budgets` csproj with references to `FinanceSentry.Core` and `FinanceSentry.Infrastructure`: `backend/src/FinanceSentry.Modules.Budgets/FinanceSentry.Modules.Budgets.csproj`
- [X] T002 Add project reference from API to Budgets module: `backend/src/FinanceSentry.API/FinanceSentry.API.csproj`
- [X] T003 [P] Create module marker class: `backend/src/FinanceSentry.Modules.Budgets/BudgetsModule.cs`
- [X] T004 [P] Create design-time DbContext factory: `backend/src/FinanceSentry.Modules.Budgets/Infrastructure/Persistence/BudgetsDbContextFactory.cs` (mirrors `BankSyncDbContextFactory` pattern)

**Checkpoint**: `dotnet build backend/` passes with zero warnings.

---

## Phase 2: Foundational (Domain Entity + DB + Services)

**Purpose**: Core infrastructure that MUST be complete before any user story can be implemented.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T005 [P] Create `CategoryTaxonomy` static class with the 9-category mapping dictionary: `backend/src/FinanceSentry.Modules.Budgets/Domain/CategoryTaxonomy.cs` — maps raw Plaid/Monobank strings to keys: `housing`, `food_and_drink`, `transport`, `shopping`, `entertainment`, `health`, `utilities`, `travel`, `other`; include `ValidKeys` readonly set and `CategoryLabels` display name dictionary
- [X] T006 [P] Create `Budget` domain entity: `backend/src/FinanceSentry.Modules.Budgets/Domain/Budget.cs` — fields: Id (Guid), UserId (string, max 450), Category (string, max 50), MonthlyLimit (decimal), Currency (string, max 3), CreatedAt (DateTimeOffset), UpdatedAt (DateTimeOffset); factory method `Budget.Create(userId, category, limit, currency)`
- [X] T007 Create `IBudgetRepository` interface: `backend/src/FinanceSentry.Modules.Budgets/Domain/Repositories/IBudgetRepository.cs` — methods: `GetByUserIdAsync`, `GetByIdAsync`, `FindByUserAndCategoryAsync` (dedup check), `CreateAsync`, `UpdateAsync`, `DeleteAsync`
- [X] T008 Create `BudgetsDbContext`: `backend/src/FinanceSentry.Modules.Budgets/Infrastructure/Persistence/BudgetsDbContext.cs` — DbSet<Budget>; OnModelCreating with `idx_budget_user_id` index and `idx_budget_user_category_unique` partial unique index from data-model.md
- [X] T009 Create `BudgetRepository`: `backend/src/FinanceSentry.Modules.Budgets/Infrastructure/Persistence/Repositories/BudgetRepository.cs` — implement all `IBudgetRepository` methods using `BudgetsDbContext`
- [X] T010 Create EF Core migration M001: `backend/src/FinanceSentry.Modules.Budgets/Migrations/` — run `dotnet ef migrations add M001_InitialSchema --project src/FinanceSentry.Modules.Budgets --context BudgetsDbContext`
- [X] T011 Create `ICategoryNormalizationService` and `CategoryNormalizationService`: `backend/src/FinanceSentry.Modules.Budgets/Application/Services/ICategoryNormalizationService.cs` and `CategoryNormalizationService.cs` — `Normalize(string? rawCategory) → string` returns matching internal key or `"other"`; `GetLabel(string categoryKey) → string` returns display name; delegates to `CategoryTaxonomy`
- [X] T012 Register Budgets module in `backend/src/FinanceSentry.API/Program.cs`: add `BudgetsModule` assembly to `AddCqrs`, register `BudgetsDbContext` with Npgsql, register `ICategoryNormalizationService → CategoryNormalizationService` as scoped, add migration block
- [X] T013 Add BankSync migration M003 for composite transaction index: `backend/src/FinanceSentry.Modules.BankSync/Migrations/` — run `dotnet ef migrations add M003_TransactionCategoryIndex --project src/FinanceSentry.Modules.BankSync --context BankSyncDbContext`; migration body: `CREATE INDEX idx_transaction_user_category_date ON transactions (user_id, merchant_category, posted_date DESC) WHERE is_active = true`

**Checkpoint**: `dotnet build backend/` zero warnings. Both migrations apply cleanly. `ICategoryNormalizationService` injectable.

---

## Phase 3: User Story 1 — Create and Manage Budgets (Priority: P1) 🎯 MVP

**Goal**: User can create a budget for a category with a monthly limit, edit the limit, and delete it. Duplicate category budgets are rejected with a 409.

**Independent Test**: `POST /api/v1/budgets` with `{ "category": "food_and_drink", "monthlyLimit": 400 }` → 201. `GET /api/v1/budgets` → item appears. `PUT /api/v1/budgets/{id}` with `{ "monthlyLimit": 500 }` → updated. Second `POST` with same category → 409. `DELETE /api/v1/budgets/{id}` → 204, item gone.

### Contract Tests for US1

- [X] T014 [P] [US1] Contract test for `GET /api/v1/budgets`: `backend/tests/FinanceSentry.Modules.Budgets.Tests/Contracts/GetBudgetsContractTest.cs` — 200 response with `{ items[], totalCount }` shape; 401 on missing token
- [X] T015 [P] [US1] Contract test for `POST /api/v1/budgets`: `backend/tests/FinanceSentry.Modules.Budgets.Tests/Contracts/CreateBudgetContractTest.cs` — 201 with full BudgetDto; 400 on invalid category; 400 on limit ≤ 0; 409 on duplicate category; 401 on missing token
- [X] T016 [P] [US1] Contract test for `PUT /api/v1/budgets/{id}`: `backend/tests/FinanceSentry.Modules.Budgets.Tests/Contracts/UpdateBudgetContractTest.cs` — 200 with updated dto; 400 on invalid limit; 404 on unknown id; 401 on missing token
- [X] T017 [P] [US1] Contract test for `DELETE /api/v1/budgets/{id}`: `backend/tests/FinanceSentry.Modules.Budgets.Tests/Contracts/DeleteBudgetContractTest.cs` — 204 on success; 404 on unknown id; 401 on missing token

### Backend Implementation for US1

- [X] T018 [P] [US1] Create response DTOs: `backend/src/FinanceSentry.Modules.Budgets/API/Responses/BudgetDto.cs` (id, category, categoryLabel, monthlyLimit, currency, createdAt) and `BudgetsListResponse.cs` (items[], totalCount)
- [X] T019 [P] [US1] Implement `GetBudgetsQuery` + handler: `backend/src/FinanceSentry.Modules.Budgets/Application/Queries/GetBudgetsQuery.cs` — calls `IBudgetRepository.GetByUserIdAsync`; maps to `BudgetDto` using `ICategoryNormalizationService.GetLabel`
- [X] T020 [P] [US1] Implement `CreateBudgetCommand` + handler: `backend/src/FinanceSentry.Modules.Budgets/Application/Commands/CreateBudgetCommand.cs` — validates `category` is in `CategoryTaxonomy.ValidKeys` (400 `BUDGET_INVALID_CATEGORY`); validates `monthlyLimit > 0` (400 `BUDGET_INVALID_LIMIT`); calls `FindByUserAndCategoryAsync` and throws 409 `BUDGET_DUPLICATE_CATEGORY` if found; reads `BaseCurrency` from `ApplicationUser` via `UserManager`; creates and persists `Budget`
- [X] T021 [P] [US1] Implement `UpdateBudgetCommand` + handler: `backend/src/FinanceSentry.Modules.Budgets/Application/Commands/UpdateBudgetCommand.cs` — validates `monthlyLimit > 0`; fetches budget by id + userId (404 if not found); updates limit and `UpdatedAt`; persists
- [X] T022 [P] [US1] Implement `DeleteBudgetCommand` + handler: `backend/src/FinanceSentry.Modules.Budgets/Application/Commands/DeleteBudgetCommand.cs` — fetches budget by id + userId (404 `BUDGET_NOT_FOUND` if not found); hard deletes
- [X] T023 [US1] Implement `BudgetsController` with GET, POST, PUT, DELETE routes: `backend/src/FinanceSentry.Modules.Budgets/API/Controllers/BudgetsController.cs` — route `/api/v1/budgets`; extract UserId from JWT claims; wire all 4 CRUD commands/queries

### Frontend Implementation for US1

- [X] T024 [P] [US1] Update `budget.model.ts`: `frontend/src/app/modules/budgets/models/budget/budget.model.ts` — add `id: string`, `currency: string`, `createdAt: string` to `Budget` interface; add `BudgetSummaryItem` interface with `id, category, categoryLabel, monthlyLimit, spent, remaining, isOverBudget, currency`; add `CreateBudgetRequest` and `UpdateBudgetRequest` interfaces
- [X] T025 [P] [US1] Create `budgets.service.ts`: `frontend/src/app/modules/budgets/services/budgets.service.ts` — HTTP methods: `getBudgets()`, `createBudget(req)`, `updateBudget(id, req)`, `deleteBudget(id)`, `getBudgetSummary(year?, month?)`
- [X] T026 [US1] Update `budgets.methods.ts`: `frontend/src/app/modules/budgets/store/budgets/budgets.methods.ts` — add `addBudget(budget: Budget)`, `updateBudgetInList(id, limit)`, `removeBudget(id)` patchState mutations
- [X] T027 [US1] Update `budgets.effects.ts`: `frontend/src/app/modules/budgets/store/budgets/budgets.effects.ts` — replace `BUDGET_MOCK_DATA` with `BudgetsService.getBudgets()` call; add `create`, `update`, `remove` rxMethods that call API and then dispatch `loadSummary` refresh
- [X] T028 [US1] Update `budgets.component.ts`: `frontend/src/app/modules/budgets/pages/budgets/budgets.component.ts` — add `createBudget(category, limit)`, `editLimit(id, newLimit)`, `deleteBudget(id)` methods that call store methods; bind to existing template edit/delete UI
- [X] T029 [P] [US1] Add CRUD error codes to registry: `frontend/src/app/core/errors/error-messages.registry.ts` — add `BUDGET_NOT_FOUND`, `BUDGET_DUPLICATE_CATEGORY`, `BUDGET_INVALID_CATEGORY`, `BUDGET_INVALID_LIMIT`

**Checkpoint**: GET /api/v1/budgets returns 200. Create → appears in list. Edit limit → updates. Duplicate category → error toast. Delete → removed.

---

## Phase 4: User Story 2 — View Spending Progress Against Budgets (Priority: P1)

**Goal**: Budgets page shows, for each budget, the amount spent in the current month, amount remaining, and a visual over-budget indicator.

**Independent Test**: With a budget for "food_and_drink" ($400 limit) and transactions categorised as food this month, `GET /api/v1/budgets/summary` returns the correct `spent` amount. Angular page shows progress bar and remaining amount.

### Contract Test for US2

- [X] T030 [P] [US2] Contract test for `GET /api/v1/budgets/summary`: `backend/tests/FinanceSentry.Modules.Budgets.Tests/Contracts/GetBudgetSummaryContractTest.cs` — 200 response with `{ year, month, items[], totalLimit, totalSpent }` shape; each item has `id, category, categoryLabel, monthlyLimit, spent, remaining, isOverBudget, currency`; 400 on invalid month; 401 on missing token

### Backend Implementation for US2

- [X] T031 [US2] Create response DTOs: `backend/src/FinanceSentry.Modules.Budgets/API/Responses/BudgetSummaryItemDto.cs` and `BudgetSummaryResponse.cs` (year, month, items[], totalLimit, totalSpent)
- [X] T032 [US2] Implement `GetBudgetSummaryQuery` + handler: `backend/src/FinanceSentry.Modules.Budgets/Application/Queries/GetBudgetSummaryQuery.cs` — parameters: UserId, Year (default current), Month (default current); validates month 1–12 and year ≥ 2020; calls `IBudgetRepository.GetByUserIdAsync` for the user's budgets; dispatches `GetTransactionSummaryQuery(UserId, from, to)` via `IMediator.Send` to get category totals for the month; for each budget, calls `ICategoryNormalizationService.Normalize` on each raw category key from transaction results; matches totals to budget category keys; computes `spent`, `remaining`, `isOverBudget`
- [X] T033 [US2] Unit test `CategoryNormalizationService`: `backend/tests/FinanceSentry.Modules.Budgets.Tests/CategoryNormalizationServiceTests.cs` — known Plaid strings normalize to correct keys; null/empty/unknown normalizes to `"other"`; all 9 taxonomy keys are recognized
- [X] T034 [US2] Add `GET /budgets/summary` route to `BudgetsController`: `backend/src/FinanceSentry.Modules.Budgets/API/Controllers/BudgetsController.cs` — `GET /summary?year=&month=` → sends `GetBudgetSummaryQuery`, returns `BudgetSummaryResponse`
- [X] T035 [P] [US2] Bump version: backend `FinanceSentry.API.csproj` minor version bump; `frontend/package.json` minor version bump
- [X] T036 [P] [US2] Add `BUDGET_INVALID_PERIOD` error code to registry: `frontend/src/app/core/errors/error-messages.registry.ts`

### Frontend Implementation for US2

- [X] T037 [US2] Update `budgets.state.ts`: `frontend/src/app/modules/budgets/store/budgets/budgets.state.ts` — rename `budgets: Budget[]` to `summaryItems: BudgetSummaryItem[]`; ensure existing computed signals (totalSpent, totalBudget, overBudgetCount, overallPct) use the new field name
- [X] T038 [US2] Update `budgets.effects.ts` to call summary endpoint: `frontend/src/app/modules/budgets/store/budgets/budgets.effects.ts` — change `onInit` load to call `BudgetsService.getBudgetSummary()` (current month); map response to `summaryItems`; ensure create/update/delete effects trigger a summary refresh after mutation

**Checkpoint**: Budget summary shows correct `spent` values from real transaction data. Over-budget cards visually highlighted. $0 spent shown when no transactions.

---

## Phase 5: User Story 3 — Budget Period and Summary (Priority: P2)

**Goal**: User can navigate to a previous month and see spending figures for that period.

**Independent Test**: Navigate to the previous month using the period picker; spending figures change to reflect that month's transactions only.

### Implementation for US3

- [X] T039 [US3] Update `budgets.state.ts` with period navigation state: `frontend/src/app/modules/budgets/store/budgets/budgets.state.ts` — add `selectedYear: number` (default: current year), `selectedMonth: number` (default: current month) to state and initial state
- [X] T040 [US3] Update `budgets.methods.ts` with period mutation: `frontend/src/app/modules/budgets/store/budgets/budgets.methods.ts` — add `setSelectedPeriod(year: number, month: number)` patchState method
- [X] T041 [US3] Update `budgets.effects.ts` to use selected period: `frontend/src/app/modules/budgets/store/budgets/budgets.effects.ts` — update `loadSummary` rxMethod to read `store.selectedYear()` and `store.selectedMonth()` and pass them to `BudgetsService.getBudgetSummary(year, month)`; add `navigateToPeriod` method that calls `setSelectedPeriod` then triggers `loadSummary`
- [X] T042 [US3] Wire period navigation UI in `budgets.component.ts`: `frontend/src/app/modules/budgets/pages/budgets/budgets.component.ts` — add `previousMonth()` and `nextMonth()` methods that call `store.navigateToPeriod()`; bind to prev/next chevron buttons in template; add computed `periodLabel()` from `store.selectedYear()` and `store.selectedMonth()`

**Checkpoint**: Clicking "previous month" button reloads summary with prior month's spending. Clicking "next month" returns to current month.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final quality pass and QA.

- [X] T043 [P] Run `dotnet build backend/` and fix all remaining warnings across Budgets module files
- [X] T044 [P] Run `npx eslint frontend/src/app/modules/budgets/ --fix` and fix all remaining errors; run `npx eslint frontend/src/app/core/errors/ --fix`
- [X] T045 Playwright QA: start full Docker stack; navigate to `/budgets` as test user (test@gmail.com / Darkfly21); verify: empty state with no budgets → create food_and_drink budget → spending shown from existing transactions → edit limit → over-budget visual state (if applicable) → delete budget; verify previous month navigation shows different spending figures

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundation)**: Depends on Phase 1 — **BLOCKS all user story phases**
- **Phase 3 (US1)**: Depends on Phase 2 — CRUD endpoints + frontend wiring
- **Phase 4 (US2)**: Depends on Phase 2 + Phase 3 (controller must exist to add summary route)
- **Phase 5 (US3)**: Depends on Phase 4 (summary endpoint must exist)
- **Phase 6 (Polish)**: Depends on all phases complete

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundation. Independently testable with empty spending.
- **US2 (P1)**: Depends on Foundation + US1 (BudgetsController must exist for the summary route). Independently testable via direct API call.
- **US3 (P2)**: Depends on US2 (summary endpoint with year/month params must exist). Purely frontend work.

### Within Each Phase

- Foundation: T005–T006 parallel → T007 → T008–T011 parallel → T012–T013 sequential
- US1 backend: T014–T022 largely parallel (all different files) → T023 (controller, depends on all commands/queries)
- US1 frontend: T024–T025 parallel → T026–T028 sequential (store composition) → T029 parallel

---

## Parallel Execution Examples

### Phase 2 parallelism

```
Parallel group A:
  T005 CategoryTaxonomy
  T006 Budget entity

Sequential from A:
  T007 IBudgetRepository (depends on T006)
  T008 BudgetsDbContext (depends on T006)

Parallel group B (depends on T007+T008):
  T009 BudgetRepository
  T011 CategoryNormalizationService

Sequential from B:
  T010 EF migration (depends on T008)
  T012 Program.cs registration (depends on T008+T011)
  T013 BankSync M003 migration (independent)
```

### Phase 3 backend parallelism

```
Parallel group (can run simultaneously after Foundation):
  T014 Contract test GET /budgets
  T015 Contract test POST /budgets
  T016 Contract test PUT /budgets/{id}
  T017 Contract test DELETE /budgets/{id}
  T018 Response DTOs
  T019 GetBudgetsQuery
  T020 CreateBudgetCommand
  T021 UpdateBudgetCommand
  T022 DeleteBudgetCommand

Depends on all above:
  T023 BudgetsController
```

### Phase 3 frontend parallelism

```
Parallel group:
  T024 budget.model.ts
  T025 budgets.service.ts
  T029 error-messages.registry.ts

Sequential (depends on T024+T025):
  T026 budgets.methods.ts
  T027 budgets.effects.ts
  T028 budgets.component.ts
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Phase 1: Setup
2. Phase 2: Foundation (entity, DB, normalization service)
3. Phase 3: US1 — CRUD endpoints + frontend wiring
4. Phase 4: US2 — Spending summary + real data in UI
5. **VALIDATE**: Budgets page shows real spending. Create/edit/delete work.
6. Deploy/demo — users can manage budgets and see spending

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready
2. Phase 3 → Budget CRUD works (page shows mock until US2 done)
3. Phase 4 → Real spending data in summary → MVP complete
4. Phase 5 → Historical period navigation
5. Phase 6 → Polish + QA

---

## Task Summary

| Phase | Tasks | Parallel [P] |
|---|---|---|
| Phase 1: Setup | T001–T004 | T003, T004 |
| Phase 2: Foundation | T005–T013 | T005, T006, T009, T011, T013 |
| Phase 3: US1 | T014–T029 | T014–T022, T024, T025, T029 |
| Phase 4: US2 | T030–T038 | T030, T035, T036 |
| Phase 5: US3 | T039–T042 | — |
| Phase 6: Polish | T043–T045 | T043, T044 |
| **Total** | **45 tasks** | **~20 parallelizable** |

---

## Notes

- [P] tasks operate on different files with no shared dependencies — safe to run concurrently
- [US*] label maps each task to its user story for traceability
- Constitution mandates contract tests for every REST endpoint (T014–T017, T030) — mandatory
- `CategoryNormalizationService` unit tests (T033) are critical for correctness of spending calculations
- T032 (`GetBudgetSummaryQuery`) is the most complex task — cross-module via MediatR; normalize raw categories from `GetTransactionSummaryQuery` response before matching against budget categories
- Run `npx eslint <file> --fix` after every Angular file; run `dotnet build backend/` after every C# file
- The existing mock `BUDGET_MOCK_DATA` in `budget.constants.ts` can be kept for dev reference but must not be loaded by the store in production
