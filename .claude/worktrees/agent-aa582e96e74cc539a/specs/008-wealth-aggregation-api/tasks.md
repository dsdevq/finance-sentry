# Tasks: Financial Aggregation and Wealth Overview API

**Input**: Design documents from `specs/008-wealth-aggregation-api/`  
**Prerequisites**: plan.md ✅, spec.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Contract tests (2 new endpoints) and unit tests (3 services) are mandatory per constitution.

**Organization**: Tasks grouped by user story — each story is independently implementable and testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[Story]**: Which user story this task belongs to
- Paths are relative to repository root

---

## Phase 1: Setup (Shared Static Utilities + Domain Interface)

**Purpose**: Create the static building blocks and domain interface that all three user stories depend on. No user story work can begin until T001–T003 are complete.

**⚠️ CRITICAL**: T004–T005 (foundational tests) must pass before user story phases begin.

- [X] T001 Create `IWealthAggregationService` domain interface (methods: `GetWealthSummaryAsync`, `GetTransactionSummaryAsync`) in `backend/src/FinanceSentry.Modules.BankSync/Domain/Services/IWealthAggregationService.cs`
- [X] T002 [P] Implement `ProviderCategoryMapper` static class with provider-to-category dictionary (`plaid`→`banking`, `monobank`→`banking`, `binance`→`crypto`, `ibkr`→`brokerage`, `null`/unknown→`other`) in `backend/src/FinanceSentry.Modules.BankSync/Application/Services/ProviderCategoryMapper.cs`
- [X] T003 [P] Implement `CurrencyConverter` static class with USD rate table (`USD`=1.00, `EUR`=1.08, `GBP`=1.27, `UAH`=0.024; unknown currencies pass through at 1.00) in `backend/src/FinanceSentry.Modules.BankSync/Application/Services/CurrencyConverter.cs`

---

## Phase 2: Foundational (Unit Tests for Static Utilities)

**Purpose**: Validate the mapping and conversion logic before it is used in aggregation. These tests run in parallel and must all pass before user story phases begin.

**⚠️ CRITICAL**: No user story work can begin until Phase 2 tests pass.

- [X] T004 [P] Unit-test `ProviderCategoryMapper`: known providers (`plaid`, `monobank`, `binance`, `ibkr`), null input, and unknown string all map to correct categories in `backend/tests/FinanceSentry.Tests.Unit/BankSync/Wealth/ProviderCategoryMapperTests.cs`
- [X] T005 [P] Unit-test `CurrencyConverter`: USD passthrough, UAH→USD, EUR→USD, GBP→USD, and unknown currency passthrough in `backend/tests/FinanceSentry.Tests.Unit/BankSync/Wealth/CurrencyConverterTests.cs`

**Checkpoint**: Static utilities verified — user story phases can now proceed.

---

## Phase 3: User Story 1 — Total Wealth Snapshot (Priority: P1) 🎯 MVP

**Goal**: `GET /api/v1/wealth/summary` returns total net worth across all connected accounts, broken down by provider category, with accounts listed per category and balances converted to USD.

**Independent Test**: Call `GET /api/v1/wealth/summary` with a valid JWT. Verify `totalNetWorth` equals the USD sum of all account balances, `categories` contains one entry per provider category present, and each account has `nativeBalance`, `balanceInBaseCurrency`, and `syncStatus` fields. Verify 401 is returned without a JWT.

### Tests for User Story 1

- [X] T006 [P] [US1] Contract test for `GET /api/v1/wealth/summary` (no filters): 200 with full snapshot shape, 401 without JWT, 200 with empty categories when user has no accounts — in `backend/tests/FinanceSentry.Tests.Integration/Wealth/WealthContractTests.cs`
- [X] T007 [P] [US1] Unit-test `WealthAggregationService.GetWealthSummaryAsync`: correct USD total from mixed-currency accounts, null-balance accounts included in list but excluded from total, empty account list returns zero total — in `backend/tests/FinanceSentry.Tests.Unit/BankSync/Wealth/WealthAggregationServiceTests.cs`

### Implementation for User Story 1

- [X] T008 [US1] Create `WealthSummaryResponse`, `CategorySummaryDto`, `AccountBalanceDto`, and `AppliedFiltersDto` record types inside `backend/src/FinanceSentry.Modules.BankSync/Application/Queries/GetWealthSummaryQuery.cs`
- [X] T009 [US1] Implement `WealthAggregationService` class with `GetWealthSummaryAsync`: load all `BankAccount` rows for the user, group by `ProviderCategoryMapper`, convert balances via `CurrencyConverter`, build response — in `backend/src/FinanceSentry.Modules.BankSync/Application/Services/WealthAggregationService.cs` (depends on T002, T003, T008)
- [X] T010 [US1] Implement `GetWealthSummaryQuery` (MediatR `IRequest<WealthSummaryResponse>`) and its handler that calls `IWealthAggregationService.GetWealthSummaryAsync` in `backend/src/FinanceSentry.Modules.BankSync/Application/Queries/GetWealthSummaryQuery.cs` (depends on T001, T009)
- [X] T011 [US1] Create `WealthController` with `GET /api/v1/wealth/summary` action (sends `GetWealthSummaryQuery` via MediatR, reads user ID from JWT `sub` claim) in `backend/src/FinanceSentry.Modules.BankSync/API/Controllers/WealthController.cs` (depends on T010)
- [X] T012 [US1] Register `IWealthAggregationService` → `WealthAggregationService` (scoped) in `backend/src/FinanceSentry.API/Program.cs`

**Checkpoint**: `GET /api/v1/wealth/summary` returns a full wealth snapshot for an authenticated user.

---

## Phase 4: User Story 2 — Filtered Slice by Category or Provider (Priority: P2)

**Goal**: `GET /api/v1/wealth/summary?category=banking` or `?provider=monobank` returns only matching accounts; unknown filter values return empty results (not errors); invalid `category` values return 400.

**Independent Test**: Call `GET /api/v1/wealth/summary?category=banking` — verify only banking accounts appear. Call `?provider=monobank` — verify only Monobank accounts appear. Call `?category=unknown` — verify 200 with empty categories. Call `?category=invalid` — verify 400.

### Tests for User Story 2

- [X] T013 [P] [US2] Add filter contract tests to `backend/tests/FinanceSentry.Tests.Integration/Wealth/WealthContractTests.cs`: `category=banking` returns only banking accounts, `provider=monobank` returns only Monobank, `category=crypto` with no crypto accounts returns 200 with empty categories, invalid `category` value returns 400 `INVALID_FILTER`
- [X] T014 [P] [US2] Add filter unit tests to `backend/tests/FinanceSentry.Tests.Unit/BankSync/Wealth/WealthAggregationServiceTests.cs`: `category` filter excludes non-matching accounts, `provider` filter takes precedence over `category` when both supplied, unknown provider returns empty result

### Implementation for User Story 2

- [X] T015 [US2] Add `Category` and `Provider` optional properties to `GetWealthSummaryQuery`; update handler to pass filters to `IWealthAggregationService` in `backend/src/FinanceSentry.Modules.BankSync/Application/Queries/GetWealthSummaryQuery.cs` (depends on T010)
- [X] T016 [US2] Add category/provider filter logic to `WealthAggregationService.GetWealthSummaryAsync`: filter by provider string if `Provider` set (takes precedence); filter by mapped category if `Category` set; validate `Category` against allowed values (`banking`, `crypto`, `brokerage`, `other`) — in `backend/src/FinanceSentry.Modules.BankSync/Application/Services/WealthAggregationService.cs` (depends on T009, T015)
- [X] T017 [US2] Add query-parameter binding and `category` validation (return 400 `INVALID_FILTER` for unrecognised category) to `GET /api/v1/wealth/summary` in `backend/src/FinanceSentry.Modules.BankSync/API/Controllers/WealthController.cs` (depends on T011, T015)

**Checkpoint**: Filtered wealth summary works; invalid filters rejected; unknown values return empty — not errors.

---

## Phase 5: User Story 3 — Expense and Income Summary (Priority: P3)

**Goal**: `GET /api/v1/wealth/transactions/summary?from=...&to=...` returns total debits (expenses) and credits (income) for the date window, broken down by provider category. Missing or invalid date params return 400. Optionally filterable by `category` and `provider`.

**Independent Test**: Call `GET /api/v1/wealth/transactions/summary?from=2026-04-01&to=2026-04-30`. Verify `totalDebits` and `totalCredits` sum all posted transactions in the window, `categories` contains per-category breakdowns. Call with `from > to` — verify 400 `INVALID_DATE_RANGE`. Call without `from` — verify 400 `MISSING_DATE_RANGE`.

### Tests for User Story 3

- [X] T018 [P] [US3] Contract test for `GET /api/v1/wealth/transactions/summary`: 200 with correct shape, missing `from`/`to` → 400 `MISSING_DATE_RANGE`, `from > to` → 400 `INVALID_DATE_RANGE`, empty window → 200 with zero totals, 401 without JWT — in `backend/tests/FinanceSentry.Tests.Integration/Wealth/WealthContractTests.cs`
- [X] T019 [P] [US3] Unit-test `WealthAggregationService.GetTransactionSummaryAsync`: debit/credit split is correct, category grouping is correct, empty window returns zeros, provider filter scopes transactions — in `backend/tests/FinanceSentry.Tests.Unit/BankSync/Wealth/WealthAggregationServiceTests.cs`

### Implementation for User Story 3

- [X] T020 [US3] Create `TransactionSummaryResponse`, `TransactionCategoryDto`, and `GetTransactionSummaryQuery` (`IRequest<TransactionSummaryResponse>`) record types in `backend/src/FinanceSentry.Modules.BankSync/Application/Queries/GetTransactionSummaryQuery.cs`
- [X] T021 [US3] Implement `WealthAggregationService.GetTransactionSummaryAsync`: join `Transaction` to its `BankAccount` for provider info; filter by `PostedDate` (fall back to `TransactionDate`) within `[from, to]`; split by `TransactionType` (debit/credit); group by `ProviderCategoryMapper`; apply optional `category`/`provider` filters; sum without currency conversion — in `backend/src/FinanceSentry.Modules.BankSync/Application/Services/WealthAggregationService.cs` (depends on T009)
- [X] T022 [US3] Implement `GetTransactionSummaryQuery` handler that calls `IWealthAggregationService.GetTransactionSummaryAsync` in `backend/src/FinanceSentry.Modules.BankSync/Application/Queries/GetTransactionSummaryQuery.cs` (depends on T020, T021)
- [X] T023 [US3] Add `GET /api/v1/wealth/transactions/summary` action to `WealthController`: validate `from`/`to` presence (400 `MISSING_DATE_RANGE`) and order (`from ≤ to`, 400 `INVALID_DATE_RANGE`); parse dates; send `GetTransactionSummaryQuery` — in `backend/src/FinanceSentry.Modules.BankSync/API/Controllers/WealthController.cs` (depends on T011, T022)

**Checkpoint**: All three user stories are independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Version bump, smoke-test all quickstart scenarios.

- [X] T024 Bump API version from `0.4.0` → `0.5.0` in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj`
- [ ] T025 Validate all 7 scenarios in `specs/008-wealth-aggregation-api/quickstart.md` pass against the running Docker stack

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately; T002 and T003 run in parallel.
- **Phase 2 (Foundational)**: Depends on T002–T003; T004 and T005 run in parallel.
- **Phase 3 (US1)**: Depends on Phase 1 + 2 complete; T006 and T007 can run in parallel before T008.
- **Phase 4 (US2)**: Depends on Phase 3 complete (T011); T013 and T014 run in parallel.
- **Phase 5 (US3)**: Depends on Phase 3 complete (T009, T011); T018 and T019 run in parallel.
- **Phase 6 (Polish)**: Depends on all user story phases complete.

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 1 + 2. No dependency on US2 or US3.
- **US2 (P2)**: Extends US1 controller and service — depends on Phase 3 complete.
- **US3 (P3)**: Adds a new endpoint to the existing controller — depends on Phase 3 complete (WealthController scaffold), independent of US2.

### Within Each User Story

- Tests (T006/T007, T013/T014, T018/T019) should be written first and confirmed to fail.
- DTOs/query records before service implementation.
- Service before query handler.
- Query handler before controller action.
- Controller action before contract test can pass.

### Parallel Opportunities

- T002 and T003 (Phase 1) run in parallel.
- T004 and T005 (Phase 2) run in parallel.
- T006 and T007 (US1 tests) run in parallel.
- T013 and T014 (US2 tests) run in parallel.
- T018 and T019 (US3 tests) run in parallel.
- US2 (Phase 4) and US3 (Phase 5) can proceed in parallel once Phase 3 is complete.

---

## Parallel Example: User Story 1

```bash
# After Phase 1 + 2 complete, launch US1 tests together:
Task T006: Contract test for GET /wealth/summary → WealthSummaryContractTests.cs
Task T007: Unit test WealthAggregationService.GetWealthSummaryAsync → WealthAggregationServiceTests.cs

# Confirm both FAIL, then implement:
Task T008: WealthSummaryResponse DTOs
Task T009: WealthAggregationService.GetWealthSummaryAsync
Task T010: GetWealthSummaryQuery + handler
Task T011: WealthController GET /wealth/summary
Task T012: Program.cs registration
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Static utilities + interface
2. Complete Phase 2: Utility unit tests (T004–T005)
3. Complete Phase 3: US1 — full wealth snapshot endpoint
4. **STOP and VALIDATE**: `GET /api/v1/wealth/summary` returns correct data for a real user
5. Deploy/demo if ready

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready
2. Phase 3 → US1 (net worth snapshot) — **MVP**
3. Phase 4 → US2 (filtering) — value add
4. Phase 5 → US3 (transaction summary) — full feature
5. Each phase adds a new capability without breaking previous phases.

---

## Notes

- [P] tasks target different files — safe to parallelize.
- [Story] label maps each task to its user story for traceability.
- **No DB migrations** — read-only queries on existing `BankAccounts` + `Transactions`.
- **No new NuGet packages** — all dependencies exist in the project.
- `BankAccount.Provider` may be null until feature 007 merges — `ProviderCategoryMapper` maps null → `"other"` gracefully.
- Transaction amounts are summed in their native currency for US3 (no cross-currency conversion on transaction totals).
- `from`/`to` matched against `PostedDate`, falling back to `TransactionDate`.
- Only posted, active transactions count toward transaction summary totals.
