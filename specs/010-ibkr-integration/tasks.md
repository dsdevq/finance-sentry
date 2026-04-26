# Tasks: IBKR Integration

**Input**: Design documents from `specs/010-ibkr-integration/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

**Tests**: All test tasks below are MANDATORY per the project constitution:
- **External API contract tests** (IBKR adapter / IB Gateway HTTP calls) — mandatory
- **REST endpoint contract tests** (connect, disconnect, holdings) — mandatory
- **Unit tests** (commands, sync job) — mandatory (>80% coverage)

**Organization**: Grouped by user story. Each story is independently implementable and testable.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Parallelizable — different files, no incomplete-task dependencies
- **[Story]**: `[US1]`–`[US4]` maps to user stories from spec.md
- Exact file paths included in every task

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the new `BrokerageSync` module project and the shared `Core` contract before any story work begins.

- [x] T001 Create `FinanceSentry.Modules.BrokerageSync` C# project (`FinanceSentry.Modules.BrokerageSync.csproj`) with references to `FinanceSentry.Core` and `FinanceSentry.Infrastructure`; add empty `BrokerageSyncModule.cs` class in `backend/src/FinanceSentry.Modules.BrokerageSync/`
- [x] T002 [P] Add `IBrokerageHoldingsReader` interface and `BrokerageHoldingSummary` record to `backend/src/FinanceSentry.Core/Interfaces/IBrokerageHoldingsReader.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, interfaces, DbContext, HTTP client, repositories, and database migration that all user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T003 Create `IBKRCredential.cs` entity (fields: Id, UserId, EncryptedUsername, UsernameIv, UsernameAuthTag, EncryptedPassword, PasswordIv, PasswordAuthTag, KeyVersion, AccountId, IsActive, LastSyncAt, LastSyncError, CreatedAt; private-set properties; constructor) in `backend/src/FinanceSentry.Modules.BrokerageSync/Domain/IBKRCredential.cs`
- [x] T004 [P] Create `BrokerageHolding.cs` entity (fields: Id, UserId, Symbol, InstrumentType, Quantity, UsdValue, SyncedAt, Provider; upsert-friendly design) in `backend/src/FinanceSentry.Modules.BrokerageSync/Domain/BrokerageHolding.cs`
- [x] T005 [P] Create `IBrokerAdapter.cs` interface and `BrokerPosition` record (BrokerName, AuthenticateAsync, GetAccountIdAsync, GetPositionsAsync) in `backend/src/FinanceSentry.Modules.BrokerageSync/Domain/Interfaces/IBrokerAdapter.cs`
- [x] T006 [P] Create repository interfaces `IIBKRCredentialRepository` (Add, GetByUserId, GetAllActive, Update, Delete, SaveChanges) and `IBrokerageHoldingRepository` (UpsertRange, GetByUserId, DeleteByUserId, SaveChanges) in `backend/src/FinanceSentry.Modules.BrokerageSync/Domain/Repositories/IRepositories.cs`
- [x] T007 [P] Create `BrokerAuthException.cs` (message + optional broker name) in `backend/src/FinanceSentry.Modules.BrokerageSync/Domain/Exceptions/BrokerAuthException.cs`
- [x] T008 Create `BrokerageSyncDbContext.cs` with `DbSet<IBKRCredential>` and `DbSet<BrokerageHolding>`; configure EF model (keys, indexes — unique on `IBKRCredentials.UserId`; unique on `BrokerageHoldings.(UserId, Symbol, Provider)`) in `backend/src/FinanceSentry.Modules.BrokerageSync/Infrastructure/Persistence/BrokerageSyncDbContext.cs`
- [x] T009 [P] Create `BrokerageSyncDbContextFactory.cs` (implements `IDesignTimeDbContextFactory<BrokerageSyncDbContext>`) in `backend/src/FinanceSentry.Modules.BrokerageSync/Infrastructure/Persistence/BrokerageSyncDbContextFactory.cs`
- [x] T010 Create `IBKRGatewayClient.cs` (typed `HttpClient` wrapper; methods: `AuthenticateAsync(username, password)` — calls `POST /v1/api/iserver/auth/ssodh/init`; `GetAuthStatusAsync()` — calls `GET /v1/api/iserver/auth/status`; `GetAccountsAsync()` — calls `GET /v1/api/iserver/accounts`; `GetPositionsAsync(accountId)` — calls `GET /v1/api/portfolio/{accountId}/positions/0`; throws `BrokerAuthException` on auth failure; configurable base URL from `IBKR:GatewayBaseUrl`) in `backend/src/FinanceSentry.Modules.BrokerageSync/Infrastructure/IBKR/IBKRGatewayClient.cs`
- [x] T011 [P] Create `IBKRGatewayModels.cs` (C# records matching IB Gateway REST responses: `IBKRAuthStatusResponse` with `Authenticated` bool; `IBKRAccountsResponse` with `Accounts` string list; `IBKRPositionResponse` with `Conid`, `ContractDesc`, `AssetClass`, `Position`, `MktPrice`, `MktValue`) in `backend/src/FinanceSentry.Modules.BrokerageSync/Infrastructure/IBKR/IBKRGatewayModels.cs`
- [x] T012 [P] Implement repository classes `IBKRCredentialRepository` and `BrokerageHoldingRepository` (EF Core, including upsert for holdings via `ExecuteUpdateAsync` or `AddOrUpdate`) in `backend/src/FinanceSentry.Modules.BrokerageSync/Infrastructure/Persistence/Repositories/Repositories.cs`
- [x] T013 Run EF migration: `dotnet ef migrations add M001_InitialSchema --project backend/src/FinanceSentry.Modules.BrokerageSync --startup-project backend/src/FinanceSentry.API --context BrokerageSyncDbContext`; commit generated migration files in `backend/src/FinanceSentry.Modules.BrokerageSync/Migrations/`
- [x] T014 [P] Write external API contract tests for `IBKRAdapter` (mock `IBKRGatewayClient` HTTP responses; assert adapter correctly parses IB Gateway auth, accounts, and positions responses; assert `BrokerAuthException` thrown on auth failure; assert positions with zero `mktValue` are included with UsdValue=0) in `backend/tests/FinanceSentry.Tests.Integration/BrokerageSync/IBKRAdapterContractTests.cs`; add `FinanceSentry.Modules.BrokerageSync` project reference to `FinanceSentry.Tests.Integration.csproj`

**Checkpoint**: Foundation complete — all user story phases can now begin.

---

## Phase 3: User Story 1 — Connect IBKR Account (Priority: P1) 🎯 MVP

**Goal**: User submits IBKR username + password → credentials validated via IB Gateway → account ID discovered → credentials stored encrypted → initial holdings sync runs → endpoint returns holdings count + connectedAt.

**Independent Test**: `POST /api/v1/brokerage/ibkr/connect` with valid credentials returns 201 with `holdingsCount ≥ 0`; a second call returns 409; invalid credentials return 422.

### Tests for User Story 1

- [x] T015 [P] [US1] Write REST contract test for `POST /api/v1/brokerage/ibkr/connect`: assert 201 + response shape on valid credentials (mocked adapter), 409 on duplicate, 422 on gateway rejection, 400 on missing fields, 401 on no JWT in `backend/tests/FinanceSentry.Tests.Integration/BrokerageSync/BrokerageControllerConnectContractTests.cs`

### Implementation for User Story 1

- [x] T016 [P] [US1] Create `IBKRAdapter.cs` implementing `IBrokerAdapter`: `AuthenticateAsync` posts credentials to gateway's `ssodh/init` and polls `auth/status`; `GetAccountIdAsync` calls `GetAccountsAsync()` and returns first account ID; `GetPositionsAsync` calls gateway positions endpoint and maps to `BrokerPosition` records (Symbol from contractDesc, InstrumentType from assetClass, Quantity from position, UsdValue from mktValue); positions with mktValue=0 included in `backend/src/FinanceSentry.Modules.BrokerageSync/Infrastructure/IBKR/IBKRAdapter.cs`
- [x] T017 [US1] Create `ConnectIBKRCommand.cs` (MediatR command + handler): check for existing active credential (return conflict error if found); call `IBrokerAdapter.AuthenticateAsync` (throw `BrokerAuthException` on failure → map to 422); call `IBrokerAdapter.GetAccountIdAsync` to discover AccountId; encrypt username + password separately via `ICredentialEncryptionService`; persist `IBKRCredential` with AccountId; dispatch `SyncIBKRHoldingsCommand`; return holdings count + connectedAt in `backend/src/FinanceSentry.Modules.BrokerageSync/Application/Commands/ConnectIBKRCommand.cs`
- [x] T018 [US1] Create `SyncIBKRHoldingsCommand.cs` (MediatR command + handler): decrypt credentials; call `IBrokerAdapter.AuthenticateAsync`; call `IBrokerAdapter.GetPositionsAsync(accountId)`; upsert each `BrokerageHolding` (UserId, Symbol, InstrumentType, Quantity, UsdValue, SyncedAt, Provider="ibkr"); update `IBKRCredential.LastSyncAt` and clear `LastSyncError` on success; on exception set `LastSyncError` and rethrow in `backend/src/FinanceSentry.Modules.BrokerageSync/Application/Commands/SyncIBKRHoldingsCommand.cs`
- [x] T019 [US1] Create `BrokerageController.cs` with `POST /api/v1/brokerage/ibkr/connect` endpoint (reads userId from JWT claims; dispatches `ConnectIBKRCommand`; maps result to 201/409/422/400 responses) in `backend/src/FinanceSentry.Modules.BrokerageSync/API/Controllers/BrokerageController.cs`
- [x] T020 [US1] Register `BrokerageSync` in `Program.cs`: `AddDbContext<BrokerageSyncDbContext>`, `AddHttpClient<IBKRGatewayClient>`, `AddScoped<IBrokerAdapter, IBKRAdapter>`, `AddScoped<IIBKRCredentialRepository, IBKRCredentialRepository>`, `AddScoped<IBrokerageHoldingRepository, BrokerageHoldingRepository>`; add `BrokerageSyncModule` assembly to MediatR scan in `backend/src/FinanceSentry.API/Program.cs`
- [x] T021 [US1] Apply `BrokerageSyncDbContext` migrations at startup (add migration block after existing BankSync, Auth, and CryptoSync blocks) in `backend/src/FinanceSentry.API/Program.cs`
- [x] T022 [P] [US1] Write unit tests for `ConnectIBKRCommand`: assert conflict error when credential already exists; assert `AuthenticateAsync` called; assert encrypted password never stored in plaintext; assert `SyncIBKRHoldingsCommand` dispatched in `backend/tests/FinanceSentry.Tests.Unit/BrokerageSync/ConnectIBKRCommandTests.cs`; add `FinanceSentry.Modules.BrokerageSync` project reference to `FinanceSentry.Tests.Unit.csproj`

**Checkpoint**: `POST /api/v1/brokerage/ibkr/connect` works end-to-end.

---

## Phase 4: User Story 2 — View Brokerage Holdings & Total Value (Priority: P2)

**Goal**: Authenticated user can query `GET /brokerage/holdings` to see current IBKR portfolio positions. Holdings also appear in `GET /wealth/summary` under the `"brokerage"` category.

**Independent Test**: After a successful connect + sync, `GET /api/v1/brokerage/holdings` returns the positions list with at least one entry and `totalUsdValue > 0`. `GET /api/v1/wealth/summary` includes a `"brokerage"` category entry with matching total.

### Tests for User Story 2

- [x] T023 [P] [US2] Write REST contract test for `GET /api/v1/brokerage/holdings`: assert 200 + response shape (provider, syncedAt, positions array, totalUsdValue); assert isStale=true when syncedAt > 1 hour ago; assert 200 with empty positions when no account connected; assert 401 on missing JWT in `backend/tests/FinanceSentry.Tests.Integration/BrokerageSync/BrokerageControllerHoldingsContractTests.cs`

### Implementation for User Story 2

- [x] T024 [P] [US2] Create `GetBrokerageHoldingsQuery.cs` (MediatR query + handler + `BrokerageHoldingsResponse` DTO + `BrokeragePositionDto`; queries `IBrokerageHoldingRepository` by userId; computes `isStale` flag from `SyncedAt > 1 hour ago` (uses most recent SyncedAt across all positions); sums `UsdValue` for totalUsdValue; returns zero-holdings response when no IBKR account exists) in `backend/src/FinanceSentry.Modules.BrokerageSync/Application/Queries/GetBrokerageHoldingsQuery.cs`
- [x] T025 [US2] Add `GET /api/v1/brokerage/holdings` endpoint to `BrokerageController.cs` (dispatches `GetBrokerageHoldingsQuery`; maps to 200 response) in `backend/src/FinanceSentry.Modules.BrokerageSync/API/Controllers/BrokerageController.cs`
- [x] T026 [US2] Create `BrokerageHoldingsReader.cs` implementing `IBrokerageHoldingsReader` (queries `IBrokerageHoldingRepository`; maps `BrokerageHolding` → `BrokerageHoldingSummary`; returns empty list when no holdings) in `backend/src/FinanceSentry.Modules.BrokerageSync/Application/Services/BrokerageHoldingsReader.cs`
- [x] T027 [US2] Update `WealthAggregationService.GetWealthSummaryAsync` to inject optional `IBrokerageHoldingsReader?`; after building bank and crypto categories, call `GetHoldingsAsync`; if any holdings exist, add `CategorySummaryDto("brokerage", totalUsd, accountDtos)` where each holding maps to `AccountBalanceDto` (BankName="IBKR", AccountType="brokerage", AccountNumberLast4=first-4-chars-of-symbol, Provider="ibkr", NativeBalance=Quantity, BalanceInBaseCurrency=UsdValue, SyncStatus="synced"/"stale") in `backend/src/FinanceSentry.Modules.BankSync/Application/Services/WealthAggregationService.cs`
- [x] T028 [US2] Register `IBrokerageHoldingsReader → BrokerageHoldingsReader` in DI (confirm scoped registration present alongside existing CryptoHoldingsReader registration) in `backend/src/FinanceSentry.API/Program.cs`

**Checkpoint**: `GET /api/v1/brokerage/holdings` and `GET /api/v1/wealth/summary` both return IBKR holdings.

---

## Phase 5: User Story 3 — Automatic Periodic Holdings Sync (Priority: P3)

**Goal**: Hangfire background job re-syncs all active IBKR accounts every 15 minutes without user action.

**Independent Test**: After registration, trigger the Hangfire job manually and verify `BrokerageHolding.SyncedAt` timestamps are updated for all active users; a credential failure for one user does not abort other users.

### Tests for User Story 3

- [x] T029 [P] [US3] Write unit tests for `IBKRSyncJob`: assert job iterates all active credentials; assert `SyncIBKRHoldingsCommand` dispatched once per credential; assert failed individual sync (throws) does not abort remaining credentials in `backend/tests/FinanceSentry.Tests.Unit/BrokerageSync/IBKRSyncJobTests.cs`

### Implementation for User Story 3

- [x] T030 [P] [US3] Create `IBKRSyncJob.cs` (Hangfire job class; injects `IIBKRCredentialRepository`, `IMediator`, `ILogger<IBKRSyncJob>`; fetches all active credentials; dispatches `SyncIBKRHoldingsCommand` per credential; catches and logs per-credential exceptions without aborting the batch) in `backend/src/FinanceSentry.Modules.BrokerageSync/Infrastructure/Jobs/IBKRSyncJob.cs`
- [x] T031 [US3] Register `IBKRSyncJob` as scoped service and add Hangfire recurring job (cron `*/15 * * * *`, job ID `"ibkr-sync"`) in `backend/src/FinanceSentry.API/Program.cs`

**Checkpoint**: IBKR holdings update automatically every 15 minutes without user action.

---

## Phase 6: User Story 4 — Disconnect IBKR Account (Priority: P4)

**Goal**: User can remove their IBKR integration; credentials and all cached holdings are deleted, future syncs stop.

**Independent Test**: After connect, call `DELETE /api/v1/brokerage/ibkr/disconnect` → returns 204; subsequent `GET /brokerage/holdings` returns empty list; subsequent connect call succeeds.

### Tests for User Story 4

- [x] T032 [P] [US4] Write REST contract test for `DELETE /api/v1/brokerage/ibkr/disconnect`: assert 204 on success; assert 404 when no account connected; assert 401 on missing JWT in `backend/tests/FinanceSentry.Tests.Integration/BrokerageSync/BrokerageControllerDisconnectContractTests.cs`

### Implementation for User Story 4

- [x] T033 [P] [US4] Create `DisconnectIBKRCommand.cs` (MediatR command + handler; load credential by userId — return 404 error if not found; set `IsActive = false` on credential; call `IBrokerageHoldingRepository.DeleteByUserIdAsync`; save changes) in `backend/src/FinanceSentry.Modules.BrokerageSync/Application/Commands/DisconnectIBKRCommand.cs`
- [x] T034 [US4] Add `DELETE /api/v1/brokerage/ibkr/disconnect` endpoint to `BrokerageController.cs` (dispatches `DisconnectIBKRCommand`; maps to 204 / 404 responses) in `backend/src/FinanceSentry.Modules.BrokerageSync/API/Controllers/BrokerageController.cs`

**Checkpoint**: Full connect → view → sync → disconnect lifecycle is functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Version bump and build validation.

- [x] T035 [P] Bump backend API minor version (new endpoints: connect, disconnect, holdings) in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj` — increment `<Version>` MINOR field per constitution Versioning & Tagging Policy
- [x] T036 Run `dotnet build backend/` and fix all warnings to reach zero-warning build (StyleCop, nullable, CS* warnings)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user stories
- **Phase 3 (US1)**: Depends on Phase 2 — Core MVP; must complete before Phase 4 (SyncIBKRHoldingsCommand used in connect flow)
- **Phase 4 (US2)**: Depends on Phase 3 (holdings exist only after a connect+sync)
- **Phase 5 (US3)**: Depends on Phase 2 + Phase 3 (IBKRSyncJob dispatches SyncIBKRHoldingsCommand)
- **Phase 6 (US4)**: Depends on Phase 2; independent of Phases 4–5
- **Phase 7 (Polish)**: Depends on all phases complete

### User Story Dependencies

- **US1 (P1)**: Foundational phase complete → start immediately
- **US2 (P2)**: US1 must be complete (holdings only exist post-sync; wealth integration depends on BrokerageHoldingsReader)
- **US3 (P3)**: Foundational + US1 complete (SyncIBKRHoldingsCommand required)
- **US4 (P4)**: Foundational complete; independent of US2/US3

### Within Each Phase

- Tasks marked [P] within the same phase can run in parallel
- Non-[P] tasks within a phase run sequentially
- Contract tests (T014, T015, T023, T032) can be written in parallel with foundational implementation

---

## Parallel Opportunities

### Phase 2 (Foundational)

```text
Parallel group A (can start immediately):
  T003 IBKRCredential entity
  T004 BrokerageHolding entity
  T005 IBrokerAdapter interface
  T006 Repository interfaces
  T007 BrokerAuthException

Sequential after A:
  T008 BrokerageSyncDbContext (needs T003, T004)

Parallel group B (after T008):
  T009 DbContextFactory
  T011 IBKRGatewayModels
  T012 Repository implementations

Sequential after B:
  T010 IBKRGatewayClient (after T011 models)
  T013 EF migration (after T008 DbContext)
  T014 External API contract tests (after T010, T011)
```

### Phase 3 (US1)

```text
Parallel group:
  T015 Connect contract test
  T016 IBKRAdapter implementation

Sequential:
  T017 ConnectIBKRCommand (needs T016)
  T018 SyncIBKRHoldingsCommand (needs T016)
  T019 BrokerageController POST endpoint (needs T017, T018)
  T020 + T021 Program.cs DI + migration (needs T019)
  T022 Unit tests (parallel, needs T017)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (connect + initial sync endpoint)
4. **STOP and VALIDATE**: `POST /brokerage/ibkr/connect` returns 201 with holdings count
5. Deploy/demo if ready

### Incremental Delivery

1. Phase 1 + 2 → foundation ready
2. Phase 3 (US1) → connect flow works → demo
3. Phase 4 (US2) → holdings query + wealth summary shows brokerage → demo
4. Phase 5 (US3) → auto-sync running → demo
5. Phase 6 (US4) → disconnect works → feature complete
6. Phase 7 → version bump + clean build → PR ready

---

## Notes

- [P] tasks run in parallel — different files, no incomplete-task dependencies
- [US*] label maps each task to its user story for traceability
- IB Gateway local URL: `http://localhost:5001` (host-mapped) or `http://ibkr-gateway:5000` (Docker internal)
- `IBKRSyncJob` skips credentials with `IsActive = false` (disconnected users)
- `WealthAggregationService` change (T027) is backward-compatible: if `IBrokerageHoldingsReader` is not registered or returns empty, the `"brokerage"` category is simply absent from the summary
- IBKR password MUST never appear in logs, responses, or error messages — validate in contract tests
- Positions with `mktValue = 0` are stored with `UsdValue = 0` and included in responses (expired options, illiquid instruments)

---

## Post-implementation iterations (after the original task list landed)

These tasks were added on top of the implemented feature to fix gaps and align with architectural decisions made later. Recorded here so the spec, plan, and code stay in sync.

### Iteration 1 — Drop Newtonsoft.Json from BrokerageSync (2026-04-26)

- [X] **TI-010-001** Replace `[JsonProperty]` with `[JsonPropertyName]` in `IBKRGatewayModels.cs`. Replace manual `JsonConvert.SerializeObject` + `StringContent` with `HttpClient.PostAsJsonAsync` and `Content.ReadFromJsonAsync<T>` from `System.Net.Http.Json` in `IBKRGatewayClient`.
- [X] **TI-010-002** Drop `<PackageReference Include="Newtonsoft.Json" />` from `FinanceSentry.Modules.BrokerageSync.csproj`. (Same iteration also drops the package from `Modules.BankSync.csproj` and `Modules.CryptoSync.csproj`, plus the central `PackageVersion` in `Directory.Packages.props`.)

### Iteration 2 — Single-tenant gateway model (2026-04-26)

The original multi-tenant design (each user types IBKR credentials, encrypted at rest, decrypted at sync time, posted per-request to the gateway) was incompatible with the IBeam sidecar — IBKR's Client Portal Gateway only allows a single active session at a time, so per-user runtime credentials would fight IBeam's own session.

The model collapses to: IBeam holds one IBKR session for the deployment via env-var creds; Finance Sentry stores only the user-id ↔ IBKR-account-id link. See `research.md` Decision 3 for rationale and the future OAuth migration path.

- [X] **TI-010-010** **Image swap**: replace `ghcr.io/gnzsnz/ib-gateway` (wrong product — runs Trading API, not Client Portal Web API) with `voyz/ibeam:latest` in `docker-compose.dev.yml`. Add `IBEAM_ACCOUNT`, `IBEAM_PASSWORD`, `IBEAM_GATEWAY_BASE_URL`, `IBEAM_LOG_LEVEL`, `IBEAM_ERROR_SCREENSHOTS` env vars; expose host port `5055 → container 5000`.
- [X] **TI-010-011** Add `IBKR_ACCOUNT` / `IBKR_PASSWORD` to `docker/.env.example`. Compose substitutes `${IBKR_ACCOUNT:-}` so the rest of the stack still starts when IBKR creds are blank.
- [X] **TI-010-012** Add `IBKR__GatewayBaseUrl=https://ibkr-gateway:5000` and `IBKR__AllowSelfSignedCert=true` to the API container env. `Program.cs`'s `AddHttpClient<IBKRGatewayClient>()` now configures a primary `HttpClientHandler` with `ServerCertificateCustomValidationCallback = DangerousAcceptAnyServerCertificateValidator` when the flag is `true` or the env is `Development`.
- [X] **TI-010-013** Domain entity rewrite: `IBKRCredential` collapses to `(Id, UserId, AccountId, IsActive, LastSyncAt, LastSyncError, CreatedAt)`. Constructor takes only `(userId, accountId)`.
- [X] **TI-010-014** Adapter interface rewrite: `IBrokerAdapter` drops `AuthenticateAsync(username, password, ct)` and adds `EnsureSessionAsync(ct)` that calls the gateway's `/iserver/auth/status`. `IBKRAdapter.EnsureSessionAsync` throws `BrokerAuthException` (mapped to 422 / `INVALID_CREDENTIALS`) when the gateway is not authenticated.
- [X] **TI-010-015** Gateway-client hardening: `IBKRGatewayClient.GetAuthStatusAsync` no longer throws on non-200. Any non-success response (404 while IBeam boots, 401 before login completes) and any `HttpRequestException` (gateway down) collapse to `IBKRAuthStatusResponse(false, false)`. `EnsureSessionAsync` in the adapter then turns that into the friendly `BrokerAuthException` instead of leaking raw HTTP errors as 500. Drop the now-unused `IBKRAuthInitRequest` model.
- [X] **TI-010-016** Connect command rewrite: `ConnectIBKRRequest` becomes an empty record. `ConnectIBKRCommand(Guid UserId)` — no creds. Handler calls `adapter.EnsureSessionAsync` → `adapter.GetAccountIdAsync` → stores `new IBKRCredential(userId, accountId)` → triggers `SyncIBKRHoldingsCommand`. Drop dependency on `ICredentialEncryptionService`.
- [X] **TI-010-017** Sync handler rewrite: `SyncIBKRHoldingsCommandHandler` drops credential decryption + reauth. Calls `adapter.EnsureSessionAsync` then reads positions for the stored `accountId`.
- [X] **TI-010-018** Validator deletion: `ConnectIBKRCommandValidator` removed (nothing left to validate after dropping `Username` / `Password` fields).
- [X] **TI-010-019** Controller signature: `BrokerageController.Connect` no longer takes `[FromBody] ConnectIBKRRequest` — body is now empty.
- [X] **TI-010-020** EF migration `M002_RemoveIbkrCredentialEncryptedColumns` (timestamp `20260426093000`): `Up()` drops `EncryptedUsername`, `UsernameIv`, `UsernameAuthTag`, `EncryptedPassword`, `PasswordIv`, `PasswordAuthTag`, `KeyVersion` from `IBKRCredentials`. `Down()` re-adds them with empty defaults. Snapshot updated. Annotated with `[DbContext(typeof(BrokerageSyncDbContext))]` + `[Migration(...)]` so EF picks it up at startup.
- [X] **TI-010-021** EF context mapping: `BrokerageSyncDbContext.OnModelCreating` drops the seven `IsRequired()` lines for the encrypted columns; only `AccountId.HasMaxLength(20)`, `IsActive.IsRequired()`, `CreatedAt.IsRequired()`, `LastSyncError.HasMaxLength(1000)` remain.
- [X] **TI-010-022** Frontend launcher: `ibkr-form.component` rewritten as a launcher (single "Connect" button, no FormGroup, no `cmn-input`/`cmn-form-field`). Frontend `ConnectIBKRRequest` model removed; only the response shape remains. `IBKRService.connect()` is a no-arg POST. `IbkrConnectStrategy.submit()` takes no payload.
- [X] **TI-010-023** Test rewrite: `BrokerageControllerConnectContractTests` covers (a) 401 unauthorized, (b) 422 when gateway not authenticated, (c) 201 on success, (d) 409 already-connected. `IBKRAdapterContractTests` covers `EnsureSessionAsync` success / not-authenticated paths plus existing position-parsing tests. Unit tests `ConnectIBKRCommandTests` and `IBKRSyncJobTests` updated to the new `IBKRCredential(userId, accountId)` ctor and the new `EnsureSessionAsync` interface.

### Iteration 3 — Compose service temporarily disabled (2026-04-26)

- [X] **TI-010-030** Comment out the `ibkr-gateway` service block in `docker/docker-compose.dev.yml` with a one-line note explaining why. Reason: live-account login through IBeam succeeds at SSO but IBKR drops the session immediately afterwards (likely a pending agreement or a read-only-API exemption that hasn't propagated). Backend module, frontend launcher, migration, and tests are all in place — uncomment the block once IBKR-side configuration is sorted, or migrate to the OAuth Web API for public deployment.

### Future — OAuth Web API migration (when going public)

Not started. Bounded scope per `research.md` Decision 3:

- [ ] **TI-010-040** _(planned)_ Add a forward EF migration to `IBKRCredentials`: encrypted `AccessToken`, `RefreshToken`, plaintext `ExpiresAt`, plus `KeyVersion` for rotation. Reuse `ICredentialEncryptionService`.
- [ ] **TI-010-041** _(planned)_ Replace `IBKRGatewayClient` with `IBKRWebApiClient` calling IBKR's hosted OAuth endpoints (no IBeam sidecar).
- [ ] **TI-010-042** _(planned)_ Add OAuth callback endpoint (`GET /api/v1/brokerage/ibkr/oauth-callback?code=…`) that exchanges the auth code for tokens and stores them.
- [ ] **TI-010-043** _(planned)_ Frontend: replace the launcher button with an `<a href={IBKR_OAUTH_AUTHORIZE_URL}>` redirect.
- [ ] **TI-010-044** _(planned)_ Remove `ibkr-gateway` Compose service entirely once OAuth is live.
