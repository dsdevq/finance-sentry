# Tasks: Subscriptions Detection (014)

**Input**: Design documents from `/specs/014-subscriptions/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: Contract tests for all REST endpoints (mandatory per constitution). Unit tests for `MerchantNameNormalizer` and detection algorithm core logic (critical business logic). No E2E tests explicitly requested — Playwright QA at end per CLAUDE.md.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story this task belongs to (US1–US3)
- Exact file paths in every description

---

## Phase 1: Setup (New Module Scaffolding)

**Purpose**: Create `FinanceSentry.Modules.Subscriptions` and wire it into the solution.

- [X] T001 Create `FinanceSentry.Modules.Subscriptions` csproj with references to `FinanceSentry.Core` and `FinanceSentry.Infrastructure`: `backend/src/FinanceSentry.Modules.Subscriptions/FinanceSentry.Modules.Subscriptions.csproj`
- [X] T002 Add project reference from API to Subscriptions module: `backend/src/FinanceSentry.API/FinanceSentry.API.csproj`
- [X] T003 [P] Create module marker class: `backend/src/FinanceSentry.Modules.Subscriptions/SubscriptionsModule.cs`
- [X] T004 [P] Create design-time DbContext factory: `backend/src/FinanceSentry.Modules.Subscriptions/Infrastructure/Persistence/SubscriptionsDbContextFactory.cs` (mirrors `BankSyncDbContextFactory`)

**Checkpoint**: `dotnet build backend/` passes with zero warnings.

---

## Phase 2: Foundational (Core Interface + Domain Entity + DB)

**Purpose**: Core interface, domain entity, repository, DbContext, and detection result service. Blocks all user story phases.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T005 Define `ISubscriptionDetectionResultService` interface and `DetectedSubscriptionData` record in `backend/src/FinanceSentry.Core/Interfaces/ISubscriptionDetectionResultService.cs` — two methods: `UpsertDetectedSubscriptionsAsync(userId, results, ct)` and `MarkStaleAsPotentiallyCancelledAsync(userId, ct)` (signatures from data-model.md)
- [X] T006 [P] Create `SubscriptionStatus` string constants class: `backend/src/FinanceSentry.Modules.Subscriptions/Domain/SubscriptionStatus.cs` — `Active = "active"`, `Dismissed = "dismissed"`, `PotentiallyCancelled = "potentially_cancelled"`
- [X] T007 Create `DetectedSubscription` domain entity: `backend/src/FinanceSentry.Modules.Subscriptions/Domain/DetectedSubscription.cs` — all fields from data-model.md; factory method `DetectedSubscription.Create(...)` and `MarkDismissed()`, `Restore()`, `MarkPotentiallyCancelled()` state methods
- [X] T008 Create `IDetectedSubscriptionRepository` interface: `backend/src/FinanceSentry.Modules.Subscriptions/Domain/Repositories/IDetectedSubscriptionRepository.cs` — methods: `GetByUserIdAsync(userId, includeDismissed, ct)`, `GetByIdAsync(id, ct)`, `FindByUserAndMerchantAsync(userId, merchantNameNormalized, ct)`, `UpsertAsync(subscription, ct)`, `UpdateStatusAsync(id, status, ct)`, `GetActiveByUserIdAsync(userId, ct)`, `GetStaleActiveAsync(userId, ct)` (for potentially-cancelled check)
- [X] T009 Create `SubscriptionsDbContext`: `backend/src/FinanceSentry.Modules.Subscriptions/Infrastructure/Persistence/SubscriptionsDbContext.cs` — DbSet<DetectedSubscription>; OnModelCreating with all three indexes from data-model.md
- [X] T010 Create `DetectedSubscriptionRepository`: `backend/src/FinanceSentry.Modules.Subscriptions/Infrastructure/Persistence/Repositories/DetectedSubscriptionRepository.cs` — implement all `IDetectedSubscriptionRepository` methods
- [X] T011 Create EF Core migration M001: `backend/src/FinanceSentry.Modules.Subscriptions/Migrations/` — run `dotnet ef migrations add M001_InitialSchema --project src/FinanceSentry.Modules.Subscriptions --context SubscriptionsDbContext`
- [X] T012 Implement `SubscriptionDetectionResultService`: `backend/src/FinanceSentry.Modules.Subscriptions/Application/Services/SubscriptionDetectionResultService.cs` — implements `ISubscriptionDetectionResultService`; `UpsertDetectedSubscriptionsAsync`: for each result, find existing by (userId, merchantNameNormalized); if not found → create; if found and status ≠ dismissed → update fields; if found and status = dismissed → skip (dismissals persist, FR-006); `MarkStaleAsPotentiallyCancelledAsync`: finds all `active` subscriptions where `now > LastChargeDate + 1.5 × average_interval_days` and updates status to `potentially_cancelled`
- [X] T013 Register Subscriptions module in `backend/src/FinanceSentry.API/Program.cs`: add `SubscriptionsModule` assembly to `AddCqrs`, register `SubscriptionsDbContext`, register `ISubscriptionDetectionResultService → SubscriptionDetectionResultService`, add migration block

**Checkpoint**: `dotnet build backend/` zero warnings. Migration applies cleanly. `ISubscriptionDetectionResultService` injectable.

---

## Phase 3: User Story 1 — View Detected Subscriptions (Priority: P1) 🎯 MVP

**Goal**: User opens the Subscriptions page and sees all detected recurring charges with merchant, cadence, amount, and next expected date. Shows empty state or insufficient-history message when appropriate.

**Independent Test**: With ≥ 3 months of transaction history, trigger the `subscription-detection` Hangfire job manually. Then `GET /api/v1/subscriptions` returns detected items with correct fields. Frontend renders them without mock data.

### Backend: Detection Algorithm

- [X] T014 [US1] Implement `MerchantNameNormalizer` static utility: `backend/src/FinanceSentry.Modules.BankSync/Application/Services/MerchantNameNormalizer.cs` — static `Normalize(string? input) → string`: lowercase, strip domain suffixes (`.com`, `.net`, `.io`, `.co`, `.org`), strip `paypal*` prefix, strip leading `*`/`#` and trailing numeric identifiers, trim/collapse whitespace; `GetDisplayName(IEnumerable<string?> rawNames) → string`: returns the most frequent non-null name
- [X] T015 [US1] Unit test `MerchantNameNormalizer`: `backend/tests/FinanceSentry.Modules.BankSync.Tests/MerchantNameNormalizerTests.cs` — test: "NETFLIX.COM" → "netflix"; "PAYPAL*SPOTIFY" → "spotify"; "  Amzn Mktp*123  " → "amzn mktp"; null/empty → "unknown"; identical strings after normalize → same group
- [X] T016 [US1] Implement `SubscriptionDetectionJob`: `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Jobs/SubscriptionDetectionJob.cs` — for each distinct userId in active BankAccounts: (1) fetch all debit, non-pending, active transactions from last 13 months via `ITransactionRepository`; (2) group by `MerchantNameNormalizer.Normalize(t.MerchantName ?? t.Description)`; (3) for groups with ≥3 transactions sorted by date: compute day intervals, check median interval in [28,35] (monthly) or [351,379] (annual), compute amount CV = stddev/mean, skip if CV > 0.20; (4) build `DetectedSubscriptionData` per qualifying group; (5) call `ISubscriptionDetectionResultService.UpsertDetectedSubscriptionsAsync`; (6) call `MarkStaleAsPotentiallyCancelledAsync`
- [X] T017 [US1] Unit test detection algorithm core logic: `backend/tests/FinanceSentry.Modules.BankSync.Tests/SubscriptionDetectionJobTests.cs` — test: 3 monthly transactions → detected as monthly; 3 annual transactions → detected as annual; 3 transactions with CV > 0.20 → excluded; < 3 transactions → not detected; dedup across accounts (same normalized merchant → one entry)
- [X] T018 [US1] Register `subscription-detection` recurring Hangfire job: `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Jobs/HangfireSetup.cs` — `AddOrUpdate<SubscriptionDetectionJob>("subscription-detection", job => job.ExecuteAsync(CancellationToken.None), Cron.Daily())`

### Backend: Endpoints

- [X] T019 [P] [US1] Contract test for `GET /api/v1/subscriptions`: `backend/tests/FinanceSentry.Modules.Subscriptions.Tests/Contracts/GetSubscriptionsContractTest.cs` — 200 response with `{ items[], totalCount, hasInsufficientHistory }` shape; each item has all fields from contract; 401 on missing token
- [X] T020 [P] [US1] Contract test for `GET /api/v1/subscriptions/summary`: `backend/tests/FinanceSentry.Modules.Subscriptions.Tests/Contracts/GetSubscriptionSummaryContractTest.cs` — 200 with `{ totalMonthlyEstimate, totalAnnualEstimate, activeCount, potentiallyCancelledCount, currency }` shape
- [X] T021 [P] [US1] Create response DTOs: `backend/src/FinanceSentry.Modules.Subscriptions/API/Responses/SubscriptionDto.cs`, `SubscriptionsListResponse.cs`, `SubscriptionSummaryResponse.cs` — `monthlyEquivalent` computed in handler (averageAmount if monthly; averageAmount / 12 if annual)
- [X] T022 [P] [US1] Implement `GetSubscriptionsQuery` + handler: `backend/src/FinanceSentry.Modules.Subscriptions/Application/Queries/GetSubscriptionsQuery.cs` — parameters: UserId, IncludeDismissed (bool); calls `IDetectedSubscriptionRepository.GetByUserIdAsync`; `hasInsufficientHistory` = true when user has zero detected subscriptions AND total active transaction history < 3 months (check LastChargeDate of oldest entry or return flag from detection service)
- [X] T023 [P] [US1] Implement `GetSubscriptionSummaryQuery` + handler: `backend/src/FinanceSentry.Modules.Subscriptions/Application/Queries/GetSubscriptionSummaryQuery.cs` — sums monthlyEquivalent across active subscriptions; counts active and potentially_cancelled; uses user's BaseCurrency
- [X] T024 [US1] Implement `SubscriptionsController` with GET list and GET summary routes: `backend/src/FinanceSentry.Modules.Subscriptions/API/Controllers/SubscriptionsController.cs` — route `/api/v1/subscriptions`; extract UserId from JWT claims

### Frontend Implementation for US1

- [X] T025 [P] [US1] Update `subscription.model.ts`: `frontend/src/app/modules/subscriptions/models/subscription/subscription.model.ts` — replace `status: 'active'|'paused'` with `'active'|'dismissed'|'potentially_cancelled'`; replace `frequency` with `cadence: 'monthly'|'annual'`; replace `amount` with `averageAmount`, `lastKnownAmount`, `monthlyEquivalent`; add `lastChargeDate`, `nextExpectedDate`, `occurrenceCount`; keep `color` as frontend-only; add `SubscriptionSummary` interface; add `DismissedSubscription` type alias
- [X] T026 [P] [US1] Create `subscriptions.service.ts`: `frontend/src/app/modules/subscriptions/services/subscriptions.service.ts` — HTTP methods: `getSubscriptions(includeDismissed?: boolean)`, `getSummary()`, `dismiss(id)`, `restore(id)`
- [X] T027 [US1] Update `subscriptions.state.ts`: `frontend/src/app/modules/subscriptions/store/subscriptions/subscriptions.state.ts` — add `summary: Nullable<SubscriptionSummary>`, `hasInsufficientHistory: boolean`; remove `cancelTargetId` state (replace with `dismissTargetId`)
- [X] T028 [US1] Update `subscriptions.computed.ts`: `frontend/src/app/modules/subscriptions/store/subscriptions/subscriptions.computed.ts` — update `activeSubscriptions` to filter `status === 'active'`; add `dismissedSubscriptions` filter; add `potentiallyCancelledSubscriptions` filter; update `monthlyTotal` to use `monthlyEquivalent`; remove `yearlyTotal` or derive from summary
- [X] T029 [US1] Update `subscriptions.methods.ts`: `frontend/src/app/modules/subscriptions/store/subscriptions/subscriptions.methods.ts` — rename `setCancelTarget` → `setDismissTarget`; rename `confirmCancel` → `confirmDismiss`; add `restoreSubscription(id)` and `setSummary(summary)` patchState mutations
- [X] T030 [US1] Update `subscriptions.effects.ts`: `frontend/src/app/modules/subscriptions/store/subscriptions/subscriptions.effects.ts` — replace `SUBSCRIPTION_MOCK_DATA` with `SubscriptionsService.getSubscriptions()` + `getSummary()` parallel calls on load; add `dismiss` and `restore` rxMethods that call API then reload
- [X] T031 [US1] Update `subscriptions.component.ts`: `frontend/src/app/modules/subscriptions/pages/subscriptions/subscriptions.component.ts` — rename `confirmCancel` → `confirmDismiss`; add `restore(id)` handler; add `insufficientHistory` banner binding from `store.hasInsufficientHistory()`; bind `daysUntil` to `nextExpectedDate` field
- [X] T032 [P] [US1] Add `SUBSCRIPTION_NOT_FOUND` to error registry: `frontend/src/app/core/errors/error-messages.registry.ts`
- [X] T033 [P] [US1] Bump version: backend `FinanceSentry.API.csproj` minor version; `frontend/package.json` minor version

**Checkpoint**: `GET /api/v1/subscriptions` returns 200 (empty if detection not yet run). Trigger `subscription-detection` via Hangfire dashboard → items appear on reload. `hasInsufficientHistory` flag works correctly.

---

## Phase 4: User Story 2 — Mark or Dismiss a Subscription (Priority: P2)

**Goal**: User can dismiss a false-positive subscription (it disappears and survives re-runs) and restore a dismissed subscription.

**Independent Test**: Dismiss a subscription via `PATCH /api/v1/subscriptions/{id}/dismiss` → it's gone from default list. Run detection job again → still dismissed. `PATCH /api/v1/subscriptions/{id}/restore` → reappears as active.

### Contract Tests for US2

- [X] T034 [P] [US2] Contract test for `PATCH /api/v1/subscriptions/{id}/dismiss`: `backend/tests/FinanceSentry.Modules.Subscriptions.Tests/Contracts/DismissSubscriptionContractTest.cs` — 204 on valid id; 404 `SUBSCRIPTION_NOT_FOUND` on unknown id; 401 on missing token
- [X] T035 [P] [US2] Contract test for `PATCH /api/v1/subscriptions/{id}/restore`: `backend/tests/FinanceSentry.Modules.Subscriptions.Tests/Contracts/RestoreSubscriptionContractTest.cs` — 204 on dismissed id; 404 on non-dismissed or unknown id; 401 on missing token

### Backend Implementation for US2

- [X] T036 [US2] Implement `DismissSubscriptionCommand` + handler: `backend/src/FinanceSentry.Modules.Subscriptions/Application/Commands/DismissSubscriptionCommand.cs` — fetches by id + userId (404 if not found); sets status to `dismissed`, sets `DismissedAt = DateTimeOffset.UtcNow`; persists via `IDetectedSubscriptionRepository.UpdateStatusAsync`
- [X] T037 [US2] Implement `RestoreSubscriptionCommand` + handler: `backend/src/FinanceSentry.Modules.Subscriptions/Application/Commands/RestoreSubscriptionCommand.cs` — fetches by id + userId (404 if not found or not dismissed); sets status to `active`, clears `DismissedAt`; persists
- [X] T038 [US2] Add dismiss and restore routes to `SubscriptionsController`: `backend/src/FinanceSentry.Modules.Subscriptions/API/Controllers/SubscriptionsController.cs` — `PATCH /{id}/dismiss` and `PATCH /{id}/restore`

**Checkpoint**: Dismiss → subscription gone from default GET. Detection job re-run → still dismissed. Restore → subscription returns as active.

---

## Phase 5: User Story 3 — Subscription Cost Summary (Priority: P2)

**Goal**: User sees total estimated monthly and annual cost across active subscriptions, with all amounts normalised to monthly equivalent.

**Independent Test**: With subscriptions of $15/month and $120/year, the summary returns `totalMonthlyEstimate ≈ $25` ($15 + $10).

**Note**: US3 is substantially implemented by T023 (`GetSubscriptionSummaryQuery`) and T028 (`subscriptions.computed.ts` monthlyTotal). This phase covers wiring the summary to the frontend summary cards.

### Implementation for US3

- [X] T039 [P] [US3] Verify `GetSubscriptionSummaryQuery` handler correctly computes monthly equivalents: `backend/src/FinanceSentry.Modules.Subscriptions/Application/Queries/GetSubscriptionSummaryQuery.cs` — confirm annual subscription amounts are divided by 12 before summing into `totalMonthlyEstimate`; add unit test in `backend/tests/FinanceSentry.Modules.Subscriptions.Tests/GetSubscriptionSummaryQueryTests.cs`
- [X] T040 [US3] Wire summary data to frontend summary cards: `frontend/src/app/modules/subscriptions/pages/subscriptions/subscriptions.component.ts` — bind Monthly Cost card to `store.summary()?.totalMonthlyEstimate ?? store.monthlyTotal()`; bind Annual Cost card to `store.summary()?.totalAnnualEstimate`; bind Active Count card to `store.summary()?.activeCount`

**Checkpoint**: Summary cards show correct totals. Annual subscriptions contribute 1/12 of their annual cost to the monthly estimate.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final quality pass and QA.

- [X] T041 [P] Run `dotnet build backend/` and fix all remaining warnings in Subscriptions module and BankSync detection job files
- [X] T042 [P] Run `npx eslint frontend/src/app/modules/subscriptions/ --fix` and fix all errors; run `npx eslint frontend/src/app/core/errors/ --fix`
- [ ] T043 Playwright QA: start full Docker stack; navigate to `/subscriptions` as test user (test@gmail.com / Darkfly21); verify: page loads (empty or with data); trigger Hangfire `subscription-detection` job manually via dashboard at `http://localhost:5001/hangfire`; reload page → verify detected subscriptions appear; dismiss one → verify it disappears; restore it via dismissed toggle → verify it reappears; verify monthly cost summary card is correct

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundation)**: Depends on Phase 1 — **BLOCKS all user story phases**
- **Phase 3 (US1)**: Depends on Phase 2; has two sub-streams (detection algorithm T014–T018 and endpoints T019–T033) that can proceed in parallel after Foundation
- **Phase 4 (US2)**: Depends on Foundation + Phase 3 controller (to add dismiss/restore routes)
- **Phase 5 (US3)**: Depends on Phase 3 (summary query exists); mostly verification
- **Phase 6 (Polish)**: Depends on all phases complete

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundation. Independently testable after detection job runs.
- **US2 (P2)**: Depends on Foundation. Controller from US1 must exist to add dismiss/restore routes. Independently testable (dismiss/restore API calls).
- **US3 (P2)**: Largely covered by US1 backend (`GetSubscriptionSummaryQuery`). Frontend wiring is the only new work.

### Within Phase 3

- T014–T018 (detection algorithm stream) can proceed in parallel with T019–T024 (endpoint stream) — different files
- T025–T026 (model + service) in parallel → T027–T031 sequential (store composition) → T032–T033 in parallel

---

## Parallel Execution Examples

### Phase 2 parallelism

```
Parallel group A:
  T005 ISubscriptionDetectionResultService (Core)
  T006 SubscriptionStatus constants

Sequential from A:
  T007 DetectedSubscription entity (depends on T006)
  T009 SubscriptionsDbContext (depends on T007)

Parallel group B (depends on T007):
  T008 IDetectedSubscriptionRepository
  T010 DetectedSubscriptionRepository (depends on T009)

Sequential from B:
  T011 EF migration (depends on T009)
  T012 SubscriptionDetectionResultService (depends on T008+T010)
  T013 Program.cs registration (depends on T012)
```

### Phase 3 two parallel streams

```
Stream A (detection algorithm — all in BankSync):
  T014 MerchantNameNormalizer
  T015 Unit tests for normalizer
  T016 SubscriptionDetectionJob (depends on T014)
  T017 Unit tests for detection logic
  T018 Hangfire job registration (depends on T016)

Stream B (endpoints — all in Subscriptions module):
  Parallel: T019 contract test, T020 contract test, T021 DTOs, T022 GetSubscriptionsQuery, T023 GetSummaryQuery
  Sequential: T024 SubscriptionsController (depends on T022+T023)

Stream C (frontend — all in Angular):
  Parallel: T025 model, T026 service, T032 error registry, T033 version bumps
  Sequential: T027 → T028 → T029 → T030 → T031 (store files)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup
2. Phase 2: Foundation (entity, DB, detection result service)
3. Phase 3 Stream A: Detection algorithm (normalizer + job)
4. Phase 3 Stream B: Endpoints (GET list + summary)
5. Phase 3 Stream C: Frontend wiring
6. **VALIDATE**: Trigger job → subscriptions appear on page
7. Deploy/demo

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready
2. Phase 3 → MVP: subscriptions page shows real detected data
3. Phase 4 → Dismiss/restore flow
4. Phase 5 → Summary card totals verified
5. Phase 6 → QA

---

## Task Summary

| Phase | Tasks | Parallel [P] |
|---|---|---|
| Phase 1: Setup | T001–T004 | T003, T004 |
| Phase 2: Foundation | T005–T013 | T006 |
| Phase 3: US1 | T014–T033 | T014–T015, T019–T023, T025–T026, T032–T033 |
| Phase 4: US2 | T034–T038 | T034, T035 |
| Phase 5: US3 | T039–T040 | T039 |
| Phase 6: Polish | T041–T043 | T041, T042 |
| **Total** | **43 tasks** | **~18 parallelizable** |

---

## Notes

- [P] tasks operate on different files with no shared dependencies — safe to run concurrently
- [US*] label maps each task to its user story for traceability
- Constitution mandates contract tests for every REST endpoint (T019, T020, T034, T035)
- `MerchantNameNormalizer` (T014) and detection algorithm (T016) are the most critical business logic — test thoroughly (T015, T017)
- `SubscriptionDetectionResultService.UpsertDetectedSubscriptionsAsync` MUST skip updating dismissed entries (FR-006) — critical invariant
- T016 (detection job) is the most complex task: group by merchant, compute intervals, check variance, call Core service
- Run `npx eslint <file> --fix` after every Angular file; run `dotnet build backend/` after every C# file
- The existing `SUBSCRIPTION_MOCK_DATA` in `subscription.constants.ts` can be kept for dev reference but must NOT be loaded by the store in production
- Detection job runs daily; first run after deployment generates all subscriptions; page shows empty state with `hasInsufficientHistory` flag until first run completes
