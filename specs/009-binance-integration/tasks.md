# Tasks: Binance Integration

**Input**: Design documents from `specs/009-binance-integration/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

**Tests**: All test tasks below are MANDATORY per the project constitution:
- **External API contract tests** (Binance adapter) — mandatory
- **REST endpoint contract tests** (connect, disconnect, holdings) — mandatory
- **Unit tests** (commands, sync job) — mandatory (>80% coverage)

**Organization**: Grouped by user story. Each story is independently implementable and testable.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Parallelizable — different files, no incomplete-task dependencies
- **[Story]**: `[US1]`–`[US4]` maps to user stories from spec.md
- Exact file paths included in every task

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the new `CryptoSync` module project and the shared `Core` contract before any story work begins.

- [x] T001 Create `FinanceSentry.Modules.CryptoSync` C# project (`FinanceSentry.Modules.CryptoSync.csproj`) with references to `FinanceSentry.Core` and `FinanceSentry.Infrastructure`; add empty `CryptoSyncModule.cs` class in `backend/src/FinanceSentry.Modules.CryptoSync/`
- [x] T002 [P] Add `ICryptoHoldingsReader` interface and `CryptoHoldingSummary` record to `backend/src/FinanceSentry.Core/Interfaces/ICryptoHoldingsReader.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, interfaces, DbContext, HTTP client, repositories, and database migration that all user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T003 Create `BinanceCredential.cs` entity (fields: Id, UserId, EncryptedApiKey, ApiKeyIv, ApiKeyAuthTag, EncryptedApiSecret, ApiSecretIv, ApiSecretAuthTag, KeyVersion, IsActive, LastSyncAt, LastSyncError, CreatedAt; private-set properties; constructor) in `backend/src/FinanceSentry.Modules.CryptoSync/Domain/BinanceCredential.cs`
- [x] T004 [P] Create `CryptoHolding.cs` entity (fields: Id, UserId, Asset, FreeQuantity, LockedQuantity, UsdValue, SyncedAt, Provider; upsert-friendly design) in `backend/src/FinanceSentry.Modules.CryptoSync/Domain/CryptoHolding.cs`
- [x] T005 [P] Create `ICryptoExchangeAdapter.cs` interface and `CryptoAssetBalance` record (ExchangeName, ValidateCredentialsAsync, GetHoldingsAsync, DisconnectAsync) in `backend/src/FinanceSentry.Modules.CryptoSync/Domain/Interfaces/ICryptoExchangeAdapter.cs`
- [x] T006 [P] Create repository interfaces `IBinanceCredentialRepository` (Add, GetByUserId, GetAllActive, Update, Delete, SaveChanges) and `ICryptoHoldingRepository` (UpsertRange, GetByUserId, DeleteByUserId, SaveChanges) in `backend/src/FinanceSentry.Modules.CryptoSync/Domain/Repositories/IRepositories.cs`
- [x] T007 [P] Create `BinanceException.cs` (message + optional Binance error code) in `backend/src/FinanceSentry.Modules.CryptoSync/Domain/Exceptions/BinanceException.cs`
- [x] T008 Create `CryptoSyncDbContext.cs` with `DbSet<BinanceCredential>` and `DbSet<CryptoHolding>`; configure EF model (keys, indexes — unique on `BinanceCredentials.UserId`; unique on `CryptoHoldings.(UserId, Asset)`) in `backend/src/FinanceSentry.Modules.CryptoSync/Infrastructure/Persistence/CryptoSyncDbContext.cs`
- [x] T009 [P] Create `CryptoSyncDbContextFactory.cs` (implements `IDesignTimeDbContextFactory<CryptoSyncDbContext>`) in `backend/src/FinanceSentry.Modules.CryptoSync/Infrastructure/Persistence/CryptoSyncDbContextFactory.cs`
- [x] T010 Create `BinanceHttpClient.cs` (typed `HttpClient` wrapper; builds HMAC-SHA256 signed query strings via `System.Security.Cryptography.HMACSHA256`; sets `X-MBX-APIKEY` header; sends requests to configurable base URL; throws `BinanceException` on non-success) in `backend/src/FinanceSentry.Modules.CryptoSync/Infrastructure/Binance/BinanceHttpClient.cs`
- [x] T011 [P] Create `BinanceAdapterModels.cs` (C# records matching Binance REST API responses: `BinanceAccountResponse`, `BinanceBalance`, `BinancePriceTicker`) in `backend/src/FinanceSentry.Modules.CryptoSync/Infrastructure/Binance/BinanceAdapterModels.cs`
- [x] T012 [P] Implement repository classes `BinanceCredentialRepository` and `CryptoHoldingRepository` (EF Core, including upsert via `ExecuteUpdateAsync` or `AddOrUpdate`) in `backend/src/FinanceSentry.Modules.CryptoSync/Infrastructure/Persistence/Repositories/Repositories.cs`
- [x] T013 Run EF migration: `dotnet ef migrations add M001_InitialSchema --project backend/src/FinanceSentry.Modules.CryptoSync --startup-project backend/src/FinanceSentry.API --context CryptoSyncDbContext`; commit generated migration files in `backend/src/FinanceSentry.Modules.CryptoSync/Migrations/`
- [x] T014 [P] Write external API contract tests for `BinanceAdapter` (mock `BinanceHttpClient` HTTP responses; assert adapter correctly parses Binance account + ticker responses; assert `BinanceException` thrown on auth failure; assert HMAC signature format) in `backend/tests/FinanceSentry.Tests.Integration/Binance/BinanceAdapterContractTests.cs`; add `FinanceSentry.Modules.CryptoSync` project reference to `FinanceSentry.Tests.Integration.csproj`

**Checkpoint**: Foundation complete — all user story phases can now begin.

---

## Phase 3: User Story 1 — Connect Binance Account (Priority: P1) 🎯 MVP

**Goal**: User submits Binance API key + secret → credentials validated with Binance → stored encrypted → initial holdings sync runs → endpoint returns holdings count.

**Independent Test**: `POST /api/v1/crypto/binance/connect` with valid testnet credentials returns 201 with `holdingsCount ≥ 0`; a second call returns 409; an invalid key returns 422.

### Tests for User Story 1

- [x] T015 [P] [US1] Write REST contract test for `POST /api/v1/crypto/binance/connect`: assert 201 + response shape on valid credentials (mocked adapter), 409 on duplicate, 422 on Binance rejection, 400 on missing fields, 401 on no JWT in `backend/tests/FinanceSentry.Tests.Integration/Binance/CryptoControllerConnectContractTests.cs`

### Implementation for User Story 1

- [x] T016 [P] [US1] Create `BinanceAdapter.cs` implementing `ICryptoExchangeAdapter`: `ValidateCredentialsAsync` calls `GET /api/v3/account`; `GetHoldingsAsync` calls `GET /api/v3/account` + bulk `GET /api/v3/ticker/price`; computes USD values (stablecoin 1:1, USDT pair, BTC bridge, else 0); filters by dust threshold from config; `DisconnectAsync` is a no-op (returns immediately) in `backend/src/FinanceSentry.Modules.CryptoSync/Infrastructure/Binance/BinanceAdapter.cs`
- [x] T017 [US1] Create `ConnectBinanceCommand.cs` (MediatR command + handler): check for existing active credential (return conflict error if found); call `ICryptoExchangeAdapter.ValidateCredentialsAsync` (throw on failure); encrypt API key + secret via `ICredentialEncryptionService`; persist `BinanceCredential`; dispatch `SyncBinanceHoldingsCommand`; return holdings count + syncedAt in `backend/src/FinanceSentry.Modules.CryptoSync/Application/Commands/ConnectBinanceCommand.cs`
- [x] T018 [US1] Create `SyncBinanceHoldingsCommand.cs` (MediatR command + handler): decrypt credentials; call `ICryptoExchangeAdapter.GetHoldingsAsync`; upsert each `CryptoHolding` (UserId, Asset, FreeQuantity, LockedQuantity, UsdValue, SyncedAt); update `BinanceCredential.LastSyncAt`; on exception set `LastSyncError` and rethrow in `backend/src/FinanceSentry.Modules.CryptoSync/Application/Commands/SyncBinanceHoldingsCommand.cs`
- [x] T019 [US1] Create `CryptoController.cs` with `POST /api/v1/crypto/binance/connect` endpoint (reads userId from JWT claims; dispatches `ConnectBinanceCommand`; maps result to 201/409/422/400 responses) in `backend/src/FinanceSentry.Modules.CryptoSync/API/Controllers/CryptoController.cs`
- [x] T020 [US1] Register `CryptoSync` in `Program.cs`: `AddDbContext<CryptoSyncDbContext>`, `AddHttpClient<BinanceHttpClient>`, `AddScoped<ICryptoExchangeAdapter, BinanceAdapter>`, `AddScoped<IBinanceCredentialRepository, BinanceCredentialRepository>`, `AddScoped<ICryptoHoldingRepository, CryptoHoldingRepository>`; add `CryptoSyncModule` assembly to MediatR scan in `backend/src/FinanceSentry.API/Program.cs`
- [x] T021 [US1] Apply `CryptoSyncDbContext` migrations at startup (add migration block after existing BankSync + Auth blocks) in `backend/src/FinanceSentry.API/Program.cs`
- [x] T022 [P] [US1] Write unit tests for `ConnectBinanceCommand`: assert conflict error when credential already exists; assert `ValidateCredentialsAsync` called; assert encrypted secret never logged; assert `SyncBinanceHoldingsCommand` dispatched in `backend/tests/FinanceSentry.Tests.Unit/CryptoSync/ConnectBinanceCommandTests.cs`; add `FinanceSentry.Modules.CryptoSync` project reference to `FinanceSentry.Tests.Unit.csproj`

**Checkpoint**: `POST /api/v1/crypto/binance/connect` works end-to-end.

---

## Phase 4: User Story 2 — View Crypto Holdings & Portfolio Value (Priority: P2)

**Goal**: Authenticated user can query `GET /crypto/holdings` to see current Binance holdings. Holdings also appear in `GET /wealth/summary` under the `"crypto"` category.

**Independent Test**: After a successful connect + sync, `GET /api/v1/crypto/holdings` returns the holdings list with `usdValue > 0` for non-stablecoin assets. `GET /api/v1/wealth/summary` includes a `"crypto"` category entry with matching total.

### Tests for User Story 2

- [x] T023 [P] [US2] Write REST contract test for `GET /api/v1/crypto/holdings`: assert 200 + response shape (provider, syncedAt, holdings array, totalUsdValue); assert 200 with empty array when no account connected; assert 401 on missing JWT in `backend/tests/FinanceSentry.Tests.Integration/Binance/CryptoControllerHoldingsContractTests.cs`

### Implementation for User Story 2

- [x] T024 [P] [US2] Create `GetCryptoHoldingsQuery.cs` (MediatR query + handler + `CryptoHoldingsResponse` DTO + `CryptoHoldingDto`; queries `ICryptoHoldingRepository` by userId; computes `isStale` flag from `SyncedAt > 1 hour ago`; returns zero-holdings response when no Binance account exists) in `backend/src/FinanceSentry.Modules.CryptoSync/Application/Queries/GetCryptoHoldingsQuery.cs`
- [x] T025 [US2] Add `GET /api/v1/crypto/holdings` endpoint to `CryptoController.cs` (dispatches `GetCryptoHoldingsQuery`; maps to 200 response) in `backend/src/FinanceSentry.Modules.CryptoSync/API/Controllers/CryptoController.cs`
- [x] T026 [US2] Create `CryptoHoldingsReader.cs` implementing `ICryptoHoldingsReader` (queries `ICryptoHoldingRepository`; maps `CryptoHolding` → `CryptoHoldingSummary`; returns empty list when no holdings) in `backend/src/FinanceSentry.Modules.CryptoSync/Application/Services/CryptoHoldingsReader.cs`
- [x] T027 [US2] Update `WealthAggregationService.GetWealthSummaryAsync` to inject `ICryptoHoldingsReader`; after building bank account categories, call `GetHoldingsAsync`; if any holdings exist, add a `CategorySummaryDto("crypto", totalUsd, accountDtos)` where each holding maps to `AccountBalanceDto` (BankName="Binance", AccountType="crypto", AccountNumberLast4=asset, Provider="binance", NativeBalance=free+locked, BalanceInBaseCurrency=usdValue) in `backend/src/FinanceSentry.Modules.BankSync/Application/Services/WealthAggregationService.cs`
- [x] T028 [US2] Register `ICryptoHoldingsReader → CryptoHoldingsReader` and `ICryptoHoldingRepository` in DI (already started in T020; confirm `CryptoHoldingsReader` scoped registration present) in `backend/src/FinanceSentry.API/Program.cs`

**Checkpoint**: `GET /api/v1/crypto/holdings` and `GET /api/v1/wealth/summary` both return Binance holdings.

---

## Phase 5: User Story 3 — Automatic Periodic Sync (Priority: P3)

**Goal**: Hangfire background job re-syncs all active Binance accounts every 15 minutes without user action.

**Independent Test**: After registration, trigger the Hangfire job manually and verify `CryptoHolding.SyncedAt` timestamps are updated.

### Tests for User Story 3

- [x] T029 [P] [US3] Write unit tests for `BinanceSyncJob`: assert job iterates all active credentials; assert `SyncBinanceHoldingsCommand` dispatched once per credential; assert failed individual sync (throws) does not abort other credentials in `backend/tests/FinanceSentry.Tests.Unit/CryptoSync/BinanceSyncJobTests.cs`

### Implementation for User Story 3

- [x] T030 [P] [US3] Create `BinanceSyncJob.cs` (Hangfire job class; injects `IBinanceCredentialRepository`, `IMediator`; fetches all active credentials; dispatches `SyncBinanceHoldingsCommand` per credential; catches and logs per-credential exceptions without aborting the batch) in `backend/src/FinanceSentry.Modules.CryptoSync/Infrastructure/Jobs/BinanceSyncJob.cs`
- [x] T031 [US3] Register `BinanceSyncJob` as scoped service and add Hangfire recurring job (cron `*/15 * * * *`, job ID `"binance-sync"`) in `backend/src/FinanceSentry.API/Program.cs`

**Checkpoint**: Binance holdings update automatically every 15 minutes without user action.

---

## Phase 6: User Story 4 — Disconnect Binance Account (Priority: P4)

**Goal**: User can remove their Binance integration; credentials and holdings are deleted, future syncs stop.

**Independent Test**: After connect, call `DELETE /api/v1/crypto/binance/disconnect` → returns 204; subsequent `GET /crypto/holdings` returns empty list; subsequent connect call succeeds.

### Tests for User Story 4

- [x] T032 [P] [US4] Write REST contract test for `DELETE /api/v1/crypto/binance/disconnect`: assert 204 on success; assert 404 when no account connected; assert 401 on missing JWT in `backend/tests/FinanceSentry.Tests.Integration/Binance/CryptoControllerDisconnectContractTests.cs`

### Implementation for User Story 4

- [x] T033 [P] [US4] Create `DisconnectBinanceCommand.cs` (MediatR command + handler; load credential by userId — return 404 error if not found; set `IsActive = false` on credential; call `ICryptoHoldingRepository.DeleteByUserIdAsync`; save changes) in `backend/src/FinanceSentry.Modules.CryptoSync/Application/Commands/DisconnectBinanceCommand.cs`
- [x] T034 [US4] Add `DELETE /api/v1/crypto/binance/disconnect` endpoint to `CryptoController.cs` (dispatches `DisconnectBinanceCommand`; maps to 204 / 404 responses) in `backend/src/FinanceSentry.Modules.CryptoSync/API/Controllers/CryptoController.cs`

**Checkpoint**: Full connect → view → sync → disconnect lifecycle is functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Version bump, build validation, and any integration wiring cleanup.

- [x] T035 [P] Bump backend API minor version (new endpoints: connect, disconnect, holdings) in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj` — increment `<Version>` MINOR field per constitution Versioning & Tagging Policy
- [x] T036 Run `dotnet build backend/` and fix all warnings to reach zero-warning build (StyleCop, nullable, CS* warnings)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user stories
- **Phase 3 (US1)**: Depends on Phase 2 — Core MVP; must complete before Phase 4 (SyncBinanceHoldingsCommand used in connect flow)
- **Phase 4 (US2)**: Depends on Phase 3 (holdings exist only after a connect+sync)
- **Phase 5 (US3)**: Depends on Phase 2 (BinanceSyncJob uses SyncBinanceHoldingsCommand from Phase 3 — do Phase 3 first)
- **Phase 6 (US4)**: Depends on Phase 2; independent of Phases 4–5
- **Phase 7 (Polish)**: Depends on all phases complete

### User Story Dependencies

- **US1 (P1)**: Foundational phase complete → start immediately
- **US2 (P2)**: US1 must be complete (holdings only exist post-sync; wealth integration depends on CryptoHoldingsReader from CryptoSync)
- **US3 (P3)**: Foundational + US1 complete (SyncBinanceHoldingsCommand required)
- **US4 (P4)**: Foundational complete; independent of US2/US3 (can run in parallel with Phase 4/5 if separated into different files)

### Within Each Phase

- Tasks marked [P] within the same phase can run in parallel
- Non-[P] tasks within a phase run sequentially (check description for implicit ordering)
- Contract tests (T014, T015, T023, T032) can be written in parallel with foundational implementation

---

## Parallel Opportunities

### Phase 2 (Foundational)

```text
Parallel group A (can start day 1):
  T003 BinanceCredential entity
  T004 CryptoHolding entity
  T005 ICryptoExchangeAdapter interface
  T006 Repository interfaces
  T007 BinanceException

Sequential after A:
  T008 CryptoSyncDbContext (needs T003, T004)

Parallel group B (after T008):
  T009 DbContextFactory
  T011 BinanceAdapterModels
  T012 Repository implementations

Sequential after B:
  T010 BinanceHttpClient (after T011 models)
  T013 EF migration (after T008 DbContext)
  T014 External API contract tests (after T010, T011)
```

### Phase 3 (US1)

```text
Parallel group:
  T015 Connect contract test
  T016 BinanceAdapter implementation

Sequential:
  T017 ConnectBinanceCommand (needs T016)
  T018 SyncBinanceHoldingsCommand (needs T016)
  T019 CryptoController POST endpoint (needs T017, T018)
  T020 + T021 Program.cs DI + migration (needs T019)
  T022 Unit tests (parallel, needs T017)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (connect + initial sync endpoint)
4. **STOP and VALIDATE**: `POST /crypto/binance/connect` returns 201 with holdings
5. Deploy/demo if ready

### Incremental Delivery

1. Phase 1 + 2 → foundation ready
2. Phase 3 (US1) → connect flow works → demo
3. Phase 4 (US2) → holdings query + wealth summary shows crypto → demo
4. Phase 5 (US3) → auto-sync running → demo
5. Phase 6 (US4) → disconnect works → feature complete
6. Phase 7 → version bump + clean build → PR ready

---

## Notes

- [P] tasks run in parallel — different files, no incomplete-task dependencies
- [US*] label maps each task to its user story for traceability
- Binance Testnet URL: `https://testnet.binance.vision` — configure in `appsettings.Development.json`
- `BinanceSyncJob` skips credentials with `IsActive = false` (disconnected users)
- `WealthAggregationService` change (T027) is backward-compatible: if `ICryptoHoldingsReader` returns empty, the `"crypto"` category is simply absent from the summary
- API secret MUST never appear in logs, responses, or error messages — validate in contract tests
