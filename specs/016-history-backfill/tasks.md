# Tasks: Historical Net Worth Backfill

**Input**: Design documents from `specs/016-history-backfill/`  
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, quickstart.md ✓

**Tests**: Per constitution — contract tests for new external API endpoints (Binance accountSnapshot, IBKR performance) are MANDATORY. Unit tests required for all business logic. No new REST endpoints → no new REST contract tests.

**Organization**: Tasks grouped by user story for independent implementation and delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Maps to spec.md user story (US1–US4)

---

## Phase 1: Setup (Baseline Verification)

**Purpose**: Confirm build is clean before starting. No new packages or projects required.

- [X] T001 Verify `dotnet build backend/` passes with zero warnings — establishes clean baseline before any changes

---

## Phase 2: Foundational (Core Interfaces + Wealth Module Plumbing)

**Purpose**: Core abstractions and Wealth module infrastructure that ALL user stories depend on. Must be complete before any provider history source is implemented.

**⚠️ CRITICAL**: US1, US2, US3 cannot start until T002–T012 are complete.

- [X] T002 Add `IHistoricalBackfillScheduler` interface to `backend/src/FinanceSentry.Core/Interfaces/IHistoricalBackfillScheduler.cs` — single method `void ScheduleForUser(Guid userId)`
- [X] T003 Add `IProviderMonthlyHistorySource` interface and `ProviderMonthlyBalance` record to `backend/src/FinanceSentry.Core/Interfaces/IProviderMonthlyHistorySource.cs` — record has `DateOnly MonthEnd`, `decimal TotalUsd`, `string AssetCategory` ("banking" | "brokerage" | "crypto")
- [X] T004 [P] Add `DeleteAllByUserIdAsync(Guid userId, CancellationToken ct)` to `backend/src/FinanceSentry.Modules.Wealth/Domain/Repositories/INetWorthSnapshotRepository.cs`
- [X] T005 [P] Add `ReplaceAllSnapshotsAsync(Guid userId, IReadOnlyList<NetWorthSnapshotData> snapshots, CancellationToken ct)` to `FinanceSentry.Core/Interfaces/INetWorthSnapshotService.cs`
- [X] T006 [P] Implement `DeleteAllByUserIdAsync` in `backend/src/FinanceSentry.Modules.Wealth/Infrastructure/Persistence/Repositories/NetWorthSnapshotRepository.cs` — plain `DELETE FROM net_worth_snapshots WHERE user_id = @userId` via EF `ExecuteDeleteAsync`
- [X] T007 [P] Implement `ReplaceAllSnapshotsAsync` in `backend/src/FinanceSentry.Modules.Wealth/Application/Services/NetWorthSnapshotService.cs` — calls `DeleteAllByUserIdAsync` then bulk-inserts all snapshots in a single EF transaction via `AddRangeAsync` + `SaveChangesAsync`
- [X] T008 Write unit tests for `NetWorthSnapshotService.ReplaceAllSnapshotsAsync` in `backend/tests/FinanceSentry.Tests.Unit/Wealth/NetWorthSnapshotServiceReplaceTests.cs` — verify: (a) delete is called before insert, (b) all snapshots are inserted with correct totals, (c) empty list → delete only, no inserts
- [X] T009 Create `HistoricalBackfillJob` in `backend/src/FinanceSentry.Modules.Wealth/Infrastructure/Jobs/HistoricalBackfillJob.cs` — injects `IEnumerable<IProviderMonthlyHistorySource>` and `INetWorthSnapshotService`; `ExecuteForUserAsync(Guid userId)` gathers all `ProviderMonthlyBalance` from all sources, groups by `MonthEnd`, aggregates banking/brokerage/crypto totals per month, then calls `ReplaceAllSnapshotsAsync`; `[AutomaticRetry(Attempts = 2)]`
- [X] T010 Create `HistoricalBackfillJobScheduler` in `backend/src/FinanceSentry.Modules.Wealth/Infrastructure/Jobs/HistoricalBackfillJobScheduler.cs` — implements `IHistoricalBackfillScheduler`; `ScheduleForUser` enqueues `HistoricalBackfillJob.ExecuteForUserAsync` via Hangfire `IBackgroundJobClient`
- [X] T011 Register in `backend/src/FinanceSentry.Modules.Wealth/WealthModule.cs` — add `HistoricalBackfillJob` as scoped, `HistoricalBackfillJobScheduler` implementing `IHistoricalBackfillScheduler` as scoped
- [X] T012 Write unit tests for `HistoricalBackfillJob` in `backend/tests/FinanceSentry.Tests.Unit/Wealth/HistoricalBackfillJobTests.cs` — mock 2 `IProviderMonthlyHistorySource` sources with overlapping and non-overlapping months; verify correct per-month aggregation and that `ReplaceAllSnapshotsAsync` is called once with the full combined list; verify no snapshots created when all sources return empty

**Checkpoint**: Foundation ready — all provider history sources can now be implemented and plugged in independently.

---

## Phase 3: User Story 1 — Binance Backfill on Connect (Priority: P1) 🎯 MVP

**Goal**: When a user connects Binance for the first time, the net worth chart immediately shows monthly crypto history reconstructed from Binance's daily snapshot API.

**Independent Test**: Connect Binance account → within 5 minutes, `GET /api/v1/net-worth/history` returns monthly snapshots with non-zero `cryptoTotal` for months covered by the Binance snapshot history.

- [X] T013 [P] [US1] Add `BinanceSnapshotResponse`, `BinanceSnapshotVo`, `BinanceSnapshotData` records to `backend/src/FinanceSentry.Modules.CryptoSync/Infrastructure/Binance/BinanceAdapterModels.cs` — model the `/sapi/v1/accountSnapshot?type=SPOT` response shape per data-model.md
- [X] T014 [P] [US1] Contract test for Binance accountSnapshot API response parsing in `backend/tests/FinanceSentry.Tests.Unit/CryptoSync/BinanceAccountSnapshotContractTests.cs` — deserialize a fixture JSON response into `BinanceSnapshotResponse`; assert `SnapshotVos` count, `UpdateTime`, `Balances` asset/free/locked values; this test validates the external API shape matches our model *(MANDATORY per constitution — write before T015, expect it to fail until models are complete)*
- [X] T015 [US1] Add `GetAccountSnapshotAsync(string apiKey, string apiSecret, long startTime, long endTime, CancellationToken ct)` to `backend/src/FinanceSentry.Modules.CryptoSync/Infrastructure/Binance/BinanceHttpClient.cs` — signed GET to `/sapi/v1/accountSnapshot?type=SPOT&limit=30&startTime={startTime}&endTime={endTime}`, returns `BinanceSnapshotResponse`
- [X] T016 [US1] Create `BinanceHistorySource` in `backend/src/FinanceSentry.Modules.CryptoSync/Infrastructure/History/BinanceHistorySource.cs` — implements `IProviderMonthlyHistorySource`; reads `BinanceCredential` via `IBinanceCredentialRepository`; if no active credential → return empty list; fetches last 30 days of snapshots; for each day entry sums asset USD values using existing price ticker (`BinanceHttpClient.GetAllPricesAsync`); groups by calendar month; picks last entry per month; returns `ProviderMonthlyBalance[]` with `AssetCategory = "crypto"`
- [X] T017 [US1] Register `BinanceHistorySource` as `IProviderMonthlyHistorySource` in `backend/src/FinanceSentry.Modules.CryptoSync/CryptoSyncModule.cs` — use `services.AddScoped<IProviderMonthlyHistorySource, BinanceHistorySource>()` (collection registration)
- [X] T018 [US1] Inject `IHistoricalBackfillScheduler` into `ConnectBinanceCommandHandler` in `backend/src/FinanceSentry.Modules.CryptoSync/Application/Commands/ConnectBinanceCommand.cs` — call `scheduler.ScheduleForUser(request.UserId)` after the initial `SyncBinanceHoldingsCommand` succeeds

**Checkpoint**: Connect Binance → Hangfire job runs → `BinanceHistorySource` fetches snapshot history → `ReplaceAllSnapshotsAsync` creates monthly crypto snapshots.

---

## Phase 4: User Story 2 — IBKR Backfill on Connect (Priority: P1)

**Goal**: When a user connects IBKR for the first time, the net worth chart immediately shows monthly brokerage history from IBKR's NAV performance API.

**Independent Test**: Connect IBKR account → within 5 minutes, `GET /api/v1/net-worth/history` returns monthly snapshots with non-zero `brokerageTotal` for months covered by the IBKR NAV history.

- [X] T019 [P] [US2] Add `IBKRPerformanceResponse`, `IBKRNavData`, `IBKRNavEntry` records to `backend/src/FinanceSentry.Modules.BrokerageSync/Infrastructure/IBKR/IBKRGatewayModels.cs` — model the `/v1/api/portfolio/{accountId}/performance` response per data-model.md; date field is a string (`"20240101"`)
- [X] T020 [P] [US2] Contract test for IBKR performance API response parsing in `backend/tests/FinanceSentry.Tests.Unit/BrokerageSync/IBKRPerformanceContractTests.cs` — deserialize a fixture JSON response into `IBKRPerformanceResponse`; assert `Nav.Data` entries with correct date strings and NAV decimal values *(MANDATORY per constitution — write before T021, expect it to fail until models are complete)*
- [X] T021 [US2] Add `GetPerformanceAsync(string accountId, CancellationToken ct)` to `backend/src/FinanceSentry.Modules.BrokerageSync/Infrastructure/IBKR/IBKRGatewayClient.cs` — GET `/v1/api/portfolio/{accountId}/performance`, returns `IBKRPerformanceResponse`; handle gracefully if gateway is unreachable (return empty response, log warning)
- [X] T022 [US2] Create `IBKRHistorySource` in `backend/src/FinanceSentry.Modules.BrokerageSync/Infrastructure/History/IBKRHistorySource.cs` — implements `IProviderMonthlyHistorySource`; reads `IBKRCredential` via `IIBKRCredentialRepository`; if no active credential → return empty list; calls `IBKRAdapter.EnsureSessionAsync` + `IBKRAdapter.GetAccountIdAsync` then `IBKRGatewayClient.GetPerformanceAsync`; parses `Nav.Data` entries (date format `"YYYYMMDD"` → `DateOnly`); picks the last NAV entry per calendar month; returns `ProviderMonthlyBalance[]` with `AssetCategory = "brokerage"`; on session error → return empty list and log warning (do not throw)
- [X] T023 [US2] Register `IBKRHistorySource` as `IProviderMonthlyHistorySource` in `backend/src/FinanceSentry.Modules.BrokerageSync/BrokerageSyncModule.cs`
- [X] T024 [US2] Inject `IHistoricalBackfillScheduler` into `ConnectIBKRCommandHandler` in `backend/src/FinanceSentry.Modules.BrokerageSync/Application/Commands/ConnectIBKRCommand.cs` — call `scheduler.ScheduleForUser(request.UserId)` after the IBKR credential is saved successfully

**Checkpoint**: Connect IBKR → Hangfire job runs → `IBKRHistorySource` fetches NAV history → monthly brokerage snapshots created.

---

## Phase 5: User Story 3 — Monobank Backfill on Connect (Priority: P2)

**Goal**: When a user connects Monobank, the net worth chart reflects banking balances reconstructed from Monobank's statement transaction history.

**Independent Test**: Connect Monobank account → within the rate-limit window, `GET /api/v1/net-worth/history` returns monthly snapshots with non-zero `bankingTotal` for months covered by statement history.

- [X] T025 [US3] Create `MonobankHistorySource` in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Monobank/History/MonobankHistorySource.cs` — implements `IProviderMonthlyHistorySource`; reads `MonobankCredential` via `IMonobankCredentialRepository`; if no credential → return empty list; decrypts token; fetches `BankAccount` list for user (Monobank provider accounts only); for each Monobank account chains 31-day statement windows back from now (reuse same logic as `MonobankAdapter.SyncTransactionsAsync` initial import); applies `Task.Delay(TimeSpan.FromSeconds(60))` between each window (FR-010); per window per account takes the last transaction's `Balance` (kopecks) → `KopecksToDecimal` → `CurrencyConverter.ToUsd(balance, MapCurrency(account.CurrencyCode))`; groups by calendar month; for months with no transactions carries forward the last known balance; sums across all accounts per month; returns `ProviderMonthlyBalance[]` with `AssetCategory = "banking"`
- [X] T026 [US3] Write unit tests for `MonobankHistorySource` in `backend/tests/FinanceSentry.Tests.Unit/BankSync/MonobankHistorySourceTests.cs` — mock `IMonobankCredentialRepository` and `IBankAccountRepository`; test: (a) no credential → empty list, (b) single account, single month — correct USD balance, (c) multiple accounts in different currencies — balances summed in USD, (d) month with no transactions → carry-forward logic
- [X] T027 [US3] Register `MonobankHistorySource` as `IProviderMonthlyHistorySource` in `backend/src/FinanceSentry.Modules.BankSync/BankSyncModule.cs`
- [X] T028 [US3] Inject `IHistoricalBackfillScheduler` into `ConnectMonobankAccountCommandHandler` in `backend/src/FinanceSentry.Modules.BankSync/Application/Commands/ConnectMonobankAccountCommand.cs` — call `scheduler.ScheduleForUser(request.UserId)` after accounts are saved successfully (after the initial statement sync completes)

**Checkpoint**: Connect Monobank → Hangfire job runs (with rate-limit delays) → `MonobankHistorySource` reconstructs banking history → monthly banking snapshots created.

---

## Phase 6: User Story 4 — Full Recompute When Additional Provider Is Connected (Priority: P2)

**Goal**: When a second or third provider is connected, all existing snapshots are recomputed to reflect contributions from all connected providers. This is structurally guaranteed by the delete-and-recreate strategy — no new implementation; only a targeted test is added.

**Independent Test**: Connect Binance (verify crypto-only snapshots). Connect IBKR. Verify all months now show both `cryptoTotal` and `brokerageTotal`.

- [X] T029 [P] [US4] Extend `HistoricalBackfillJob` unit tests in `backend/tests/FinanceSentry.Tests.Unit/Wealth/HistoricalBackfillJobTests.cs` — add scenario: two sources with overlapping months → verify `ReplaceAllSnapshotsAsync` receives snapshots with both categories summed correctly; add scenario: source A has months Jan–Mar, source B has months Feb–Apr → verify Feb–Mar have both contributions, Jan has only A, Apr has only B

**Checkpoint**: All four user stories are now independently implemented and testable.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T030 Run `dotnet build backend/` and fix all warnings to zero across all modified files — mandatory build gate before declaring feature complete

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — **blocks all provider work**
- **Phase 3 (US1 Binance)**: Depends on Phase 2 complete
- **Phase 4 (US2 IBKR)**: Depends on Phase 2 complete — can run in parallel with Phase 3
- **Phase 5 (US3 Monobank)**: Depends on Phase 2 complete — can run in parallel with Phase 3 and 4
- **Phase 6 (US4)**: Depends on Phase 3 + Phase 4 being complete (needs Binance + IBKR)
- **Phase 7 (Polish)**: Depends on all phases complete

### Within Foundational Phase (Phase 2)

```
T001                       (baseline)
  └── T002                 (IHistoricalBackfillScheduler)
  └── T003                 (IProviderMonthlyHistorySource)
  └── T004 [P] T005 [P]    (interface extensions — parallel)
        └── T006 [P] T007 [P]  (implementations — parallel)
              └── T008     (tests for service)
              └── T009     (HistoricalBackfillJob — needs T003,T005)
                    └── T010   (scheduler)
                    └── T011   (WealthModule registration)
                    └── T012   (job unit tests)
```

### Within US1 (Phase 3)

```
T013 [P] (models) ← T014 [P] (contract test, write first and let fail)
T013 → T015 (GetAccountSnapshotAsync)
T013, T015 → T016 (BinanceHistorySource)
T016 → T017 (module registration)
T003 → T018 (trigger in connect command, needs IHistoricalBackfillScheduler)
```

### User Story Parallelism

- US1, US2, US3 phases can all start as soon as Phase 2 completes — they touch different modules.
- US4 validation extends an existing test file — can be added any time after T012.

---

## Parallel Opportunities

### Phase 2 Parallel Batch

```
# Once T003 and T004 are done, run in parallel:
T006 — implement DeleteAllByUserIdAsync (Repositories.cs)
T007 — implement ReplaceAllSnapshotsAsync (NetWorthSnapshotService.cs)
```

### US1 + US2 + US3 in Parallel (after Phase 2 completes)

```
US1: T013 → T014 → T015 → T016 → T017 → T018
US2: T019 → T020 → T021 → T022 → T023 → T024
US3: T025 → T026 → T027 → T028
```

---

## Implementation Strategy

### MVP (US1 + US2 only — Phase 2 + 3 + 4)

1. Complete Phase 2 (Foundational)
2. Complete Phase 3 (US1 Binance) — delivers crypto history backfill
3. Complete Phase 4 (US2 IBKR) — delivers brokerage history backfill
4. **VALIDATE**: Both providers backfill independently; connecting IBKR after Binance triggers recompute with combined totals
5. Deploy as partial feature (Monobank backfill deferred)

### Full Delivery (all phases)

1. Phase 2 → Phase 3 + 4 (parallel) → Phase 5 → Phase 6 → Phase 7

---

## Notes

- No new REST endpoints → no new REST contract tests required
- Two mandatory external API contract tests: Binance accountSnapshot (T014), IBKR performance (T020)
- No DB migration — reuses existing `net_worth_snapshots` table
- Monobank rate limiting means US3 backfill will exceed the 5-minute SC-004 target; accepted per spec
- `FirstSyncSnapshotTrigger` is **not modified** — it continues to schedule the regular monthly snapshot job for Plaid accounts; backfill is triggered separately from connect command handlers
