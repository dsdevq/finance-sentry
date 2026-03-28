# Task Breakdown: Bank Account Sync Feature (001)

**Feature**: Bank Account Aggregation & Sync
**Component**: Finance Sentry Backend + Frontend
**Status**: Ready for execution
**Last Updated**: 2026-03-28
**MVP Target**: Complete Phase 1 → Phase 2 → Phase 3 (US1 only), then deploy

---

## Overview: Phase Dependencies & Timeline

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundational Infrastructure)
    ├→ Phase 3 (US1 - P1 Connect & View) [Independent, no blockers except 1+2]
    ├→ Phase 4 (US2 - P2 Auto Sync) [Depends on US1]
    └→ Phase 5 (US3 - P3 Aggregation) [Depends on US1+US2]
         ↓
Phase 6 (Polish & Cross-Cutting Concerns)
```

**Execution Strategy**:
- Phases 1-2 are blocking (all downstream work depends on them)
- Phase 3 (US1) can start in parallel with Phase 2
- Phase 4 (US2) waits for Phase 3 to deliver working accounts
- Phase 5 (US3) waits for Phase 4 to deliver working syncing
- All tasks marked `[P]` can run in parallel with other `[P]` tasks within the same phase
- **TDD Rule (Constitution §Testing Discipline)**: Within each user story phase, test tasks appear first. Write failing tests before implementing the feature under test.

---

## Phase 1: Setup & Scaffolding

**Objective**: Create project structure, establish database, containerize application.
**Duration**: 2-3 days
**Parallelizable**: Yes - backend scaffold, frontend scaffold, Docker setup can run in parallel

### Phase 1 Tasks

- [ ] T001 [P] Create .NET 9 ASP.NET Core backend project structure per plan.md
  - **File Paths**: `backend/` folder, `backend/src/Modules/BankSync/`, `backend/src/Startup/`
  - **Details**: Create solution, project files, folder structure for modular monolith. Include Program.cs with DependencyInjection.cs
  - **Success Criteria**: `dotnet build` succeeds, no compiler warnings

- [ ] T002 [P] Create Angular frontend SPA project structure per plan.md
  - **File Paths**: `frontend/src/app/modules/bank-sync/`, `frontend/src/app/core/`, `frontend/src/app/shared/`
  - **Details**: Initialize Angular 20+ project with strict mode enabled, create folder structure for bank-sync module. Install core dependencies:
    - `npm install @plaid/link-web` (Plaid Link SDK - required for account linking in T217)
    - `npm install ngx-charts` (charts library - required for Phase 5 dashboard)
    - Configure: ESLint (strict rules), Prettier (code formatting), tsconfig.json with strict=true
    - Create: folder structure per plan.md (modules/, core/, shared/)
  - **Success Criteria**: `npm install && ng build` succeeds, no linting errors, Plaid and ngx-charts packages available in package.json

- [ ] T003 [P] Set up PostgreSQL Docker container with initialization scripts
  - **File Paths**: `docker/docker-compose.yml`, `docker/init-db.sql`
  - **Details**: Create docker-compose with PostgreSQL 14+, volume persistence. Include init script that creates database and runs migrations
  - **Success Criteria**: Container starts, database accessible from app, can connect via psql

- [ ] T004 Create Entity Framework Core DbContext for bank-sync module
  - **File Paths**: `backend/src/Modules/BankSync/Infrastructure/Persistence/BankSyncDbContext.cs`
  - **Details**: DbContext with DbSets for BankAccount, Transaction, SyncJob, EncryptedCredential. Include OnModelCreating with indexes from data-model.md
  - **Success Criteria**: Context compiles, migration generation succeeds

- [ ] T005 Create EF Core migration for initial schema (all 4 entities)
  - **File Paths**: `backend/src/Migrations/M001_BankSyncSchema_Initial.cs`
  - **Details**: Migration that creates all tables with columns, constraints, indexes per data-model.md. Include test migration rollback
  - **Success Criteria**: Migration applies cleanly to PostgreSQL, rollback succeeds, schema matches data-model.md exactly

- [ ] T006 Set up CI/CD pipeline configuration
  - **File Paths**: `.github/workflows/ci-build.yml`, `.github/workflows/test.yml`
  - **Details**: GitHub Actions workflow for: build, linting (C# StyleCop + Angular ESLint), test execution, coverage reporting
  - **Success Criteria**: Workflow runs on PR, fails on warnings (strict), reports coverage

- [ ] T007 Create .NET build task and run configurations in VS Code
  - **File Paths**: `.vscode/tasks.json`, `.vscode/launch.json`
  - **Details**: Task for `dotnet build`, `dotnet test`, `dotnet watch`. Launch config for backend debugging
  - **Success Criteria**: Build task works, debug breakpoints hit

- [ ] T008 Create Docker multi-stage build for backend container
  - **File Paths**: `docker/Dockerfile`
  - **Details**: Multi-stage build (build → runtime). Publish as release build, expose port 5000
  - **Success Criteria**: Image builds, container runs, health endpoint responds

- [ ] T009 Create Docker network configuration for backend + PostgreSQL coordination
  - **File Paths**: `docker/docker-compose.override.dev.yml`
  - **Details**: Docker Compose override for development with environment variables (Plaid keys, JWT secret, DB connection string)
  - **Success Criteria**: `docker-compose up` starts both services, they can communicate

- [ ] T010 Add Plaid SDK and MediatR NuGet packages to backend project
  - **File Paths**: `backend/finance-sentry.csproj`
  - **Details**: Add packages: PlaidNet, MediatR, EntityFrameworkCore, Hangfire (for Phase 4)
  - **Success Criteria**: `dotnet restore` succeeds, no package conflicts

**Phase 1 Checkpoint**: Backend scaffold + database ready, frontend scaffold ready, containers running locally.

---

## Phase 2: Foundational Infrastructure (Shared Services)

**Objective**: Implement encryption, retry logic, logging—required by all user stories.
**Duration**: 3-4 days
**Dependencies**: Phase 1 complete
**Parallelizable**: Yes - encryption, retry, logging can be built in parallel

### Phase 2 Tasks

- [ ] T101 [P] Implement AES-256 credential encryption service
  - **File Paths**: `backend/src/Modules/Shared/Encryption/CredentialEncryptionService.cs`, `backend/src/Modules/Shared/Encryption/EncryptionKeyManager.cs`
  - **Details**: Service to encrypt/decrypt Plaid access tokens using AES-256-GCM. Include IV generation, auth tag validation, key versioning (for rotation). Encrypt on creation, decrypt on use only
  - **Key Features**:
    - Encrypt method: takes plaintext token, returns (encrypted_data, iv, auth_tag, key_version)
    - Decrypt method: takes ciphertext + iv + auth_tag, validates auth tag, returns plaintext
    - Key derivation from master key (from environment)
    - Never log plaintext tokens or keys
  - **Success Criteria**: Unit tests verify encrypt↔decrypt roundtrip, auth_tag validation rejects tampered data, no plaintext in logs

- [ ] T102 [P] Implement exponential backoff retry policy for Plaid API calls
  - **File Paths**: `backend/src/Modules/Shared/Retry/ExponentialBackoffPolicy.cs`, `backend/src/Modules/Shared/Retry/RetryPolicyFactory.cs`
  - **Details**: Polly-based retry policy: max 3 attempts (per FR-005), delays = [5 min, 15 min, 1 hour]. Retry on timeout, rate limit (429), service unavailable (5xx). Do NOT retry on auth failures (401, 403) or validation errors (400)
  - **Key Features**:
    - Configurable attempt count and delays
    - Different policies for different error types (transient vs permanent)
    - Correlation ID tracking for retry chains
  - **Success Criteria**: Unit tests verify delays are correct (5m/15m/1h), permanent errors fail immediately without retry, transient errors retry exactly 3 times

- [ ] T103 [P] Implement structured logging service with correlation IDs
  - **File Paths**: `backend/src/Modules/Shared/Logging/LoggingService.cs`, `backend/src/Modules/Shared/Logging/CorrelationIdMiddleware.cs`
  - **Details**: Wrapper around ILogger that includes: correlation ID (unique per sync attempt), timestamp, user ID (never plaintext passwords). Middleware to capture correlation ID from request headers
  - **Key Features**:
    - Correlation ID injected into all sync-related logs
    - Never log: plaintext tokens, passwords, full account numbers
    - Log levels: Info (sync start/end), Warn (retry), Error (failure with error code)
  - **Success Criteria**: Log output shows correlation ID consistently, Plaid errors logged with code + message (no sensitive data)

- [ ] T104 [P] Create transaction deduplication service (using unique_hash)
  - **File Paths**: `backend/src/Modules/BankSync/Application/Services/TransactionDeduplicationService.cs`
  - **Details**: Service to detect duplicate transactions. Hash calculation: HMAC-SHA256(account_id|amount|date|description, master_key). Compare incoming transaction hashes against existing hashes in database
  - **Key Features**:
    - Deterministic hash: same transaction always produces same hash
    - Bulk deduplication: takes list of 100+ transactions, returns only new ones
    - Handles pending→posted transition: treat pending + posted as different (different dates)
  - **Success Criteria**: Unit tests verify: same transaction gets same hash, different amounts get different hashes, deduplication filters out 95%+ of test duplicates

- [ ] T105 [P] Create domain models for DDD (Value Objects, Aggregates)
  - **File Paths**: `backend/src/Modules/BankSync/Domain/ValueObjects/Money.cs`, `backend/src/Modules/BankSync/Domain/ValueObjects/AccountNumber.cs`, `backend/src/Modules/BankSync/Domain/Aggregates/BankAccountAggregate.cs`
  - **Details**: Value objects (Money with currency, AccountNumber with validation), Aggregates (BankAccount as root aggregate). Include business logic for state transitions
  - **Key Domain Rules**:
    - Money: immutable, enforces currency matching
    - AccountNumber: immutable, stores only last 4 digits
    - BankAccount: enforces state machine (pending → syncing → active/failed)
  - **Success Criteria**: Value object equality works by value (not reference), aggregate methods enforce invariants

- [ ] T106 Create repository interface for BankAccount (generic repository pattern)
  - **File Paths**: `backend/src/Modules/Shared/Repository/IRepository.cs`, `backend/src/Modules/BankSync/Infrastructure/Persistence/BankAccountRepository.cs`
  - **Details**: Generic IRepository<T> with methods: GetById, GetAll, Add, Update, Delete, SaveChangesAsync. Implement for BankAccount with user-scoped queries
  - **Success Criteria**: Repository compiles, unit tests mock IRepository successfully

- [ ] T107 Create MediatR handlers for basic bank sync queries
  - **File Paths**: `backend/src/Modules/BankSync/Application/Queries/GetAccountsQuery.cs`, `backend/src/Modules/BankSync/Application/Queries/GetAccountsQueryHandler.cs`
  - **Details**: Query handler for GetAccountsQuery (returns list of accounts for authenticated user). Include filtering by status/currency
  - **Success Criteria**: Handler compiles, unit tests verify query execution with mock repository

- [ ] T108 Set up dependency injection container with all Phase 2 services
  - **File Paths**: `backend/src/Startup/DependencyInjection.cs`
  - **Details**: Register: EncryptionService, RetryPolicy, LoggingService, Repositories, MediatR handlers. Separate modules so Phase 3 can add new registrations without conflicts
  - **Success Criteria**: Application starts, all registered services resolve without errors

- [ ] T109 Create integration tests for credential encryption + decryption
  - **File Paths**: `backend/tests/Integration/Shared/CredentialEncryptionTests.cs`
  - **Details**: Tests: encrypt token, store to DB, retrieve, decrypt, verify matches original. Use testcontainers PostgreSQL
  - **Success Criteria**: Tests pass, no plaintext tokens in database, encrypted data > plaintext length

- [ ] T110 Create unit tests for deduplication logic (100+ transactions)
  - **File Paths**: `backend/tests/Unit/BankSync/TransactionDeduplicationTests.cs`
  - **Details**: Test data: 100 transactions (50 duplicates, 50 unique). Verify deduplication filters exactly 50. Test hash consistency
  - **Success Criteria**: Tests pass, deduplication accuracy > 95%

**Phase 2 Checkpoint**: Encryption, retry, logging, deduplication all working. Core infrastructure ready for user story implementation.

---

## Phase 3: User Story 1 (P1) - Connect Bank Account & View Transactions

**Objective**: Enable users to connect bank accounts via Plaid and view transaction history.
**Duration**: 5-7 days
**Dependencies**: Phase 1 + Phase 2 complete
**TDD Order**: Test skeletons first → entity implementations → service implementations → integration tests

**Delivered**:
- Users connect bank account via Plaid Link UI
- System securely stores Plaid access token
- System fetches 6-12 months transaction history
- Dashboard shows account balance + transaction list
- Last sync timestamp visible to user

### Phase 3 Tasks

<!-- TDD: Write unit + contract tests FIRST, then implement to make them pass -->

- [ ] T211 [P] [US1] Create unit tests for BankAccount entity in `backend/tests/Unit/BankSync/Domain/BankAccountTests.cs`
  - **Details**: Tests:
    - Constructor validates required fields
    - State transitions (pending → syncing → active) work
    - Invalid transitions fail
    - Equality based on account_id
  - **Success Criteria**: All tests compile (may fail until T201 implemented), 90%+ code coverage target

- [ ] T212 [P] [US1] Create unit tests for Transaction entity in `backend/tests/Unit/BankSync/Domain/TransactionTests.cs`
  - **Details**: Tests:
    - Constructor validates amount > 0
    - Immutability after creation
    - Equality based on transaction_id
    - transaction_type and merchant_category fields present and validated
  - **Success Criteria**: All tests compile, ready to pass once T202 implemented

- [ ] T213 [P] [US1] Create unit tests for Plaid adapter in `backend/tests/Unit/BankSync/Infrastructure/PlaidAdapterTests.cs`
  - **Details**: Mock PlaidNet SDK. Tests:
    - CreateLinkToken() returns valid token
    - ExchangePublicToken() converts public to access token
    - GetAccountsWithBalance() maps response to domain models
    - Error handling: 4xx and 5xx responses thrown correctly
  - **Success Criteria**: All tests compile, ready to pass once T204 implemented

- [ ] T215 [P] [US1] Create REST API contract tests in `backend/tests/Contract/BankSyncAPIContractTests.cs`
  - **Details**: Contract tests validate:
    - POST /accounts/connect returns { linkToken, expiresIn, ... }
    - POST /accounts/link returns { accountId, bankName, ... }
    - GET /accounts returns { accounts[], totalCount, currency_totals }
    - GET /accounts/{id}/transactions returns { transactions[], totalCount, hasMore }
  - **Success Criteria**: All contract tests compile, ready to pass once endpoints implemented

<!-- Implementation: Make the tests above pass -->

- [ ] T201 [P] [US1] Create BankAccount domain entity (full implementation) in `backend/src/Modules/BankSync/Domain/Aggregates/BankAccount.cs`
  - **Details**: Entity with: account_id, user_id, plaid_item_id, bank_name, account_type, account_number_last4, currency, current_balance, available_balance, sync_status, timestamps. Include state machine: pending → syncing → active/failed/reauth_required
  - **Success Criteria**: T211 tests pass, constructor validates invariants, state transitions work

- [ ] T202 [P] [US1] Create Transaction domain entity in `backend/src/Modules/BankSync/Domain/Aggregates/Transaction.cs`
  - **Details**: Entity with: transaction_id, account_id, amount, date, description, unique_hash, is_pending, transaction_type (debit/credit/transfer), merchant_category. Immutable after creation
  - **Success Criteria**: T212 tests pass, equality based on transaction_id, created_at cannot be modified

- [ ] T203 [US1] Create EncryptedCredential domain entity in `backend/src/Modules/BankSync/Domain/Aggregates/EncryptedCredential.cs`
  - **Details**: Entity with: credential_id, account_id, encrypted_data, iv, auth_tag, key_version, created_at, last_used_at
  - **Success Criteria**: Entity compiles, stores encrypted data (never decrypts in entity itself)

- [ ] T204 [P] [US1] Create Plaid adapter service in `backend/src/Modules/BankSync/Infrastructure/PlaidAdapter.cs`
  - **Details**: Service methods:
    - CreateLinkToken(): generates Plaid Link token for frontend
    - ExchangePublicToken(publicToken): exchanges public token for access token (plaid_item_id)
    - GetAccountsWithBalance(accessToken): fetches accounts + current balances from Plaid
    - GetTransactions(accessToken, startDate, endDate): fetches transactions using pagination
    - RevokeAccess(accessToken): disconnects item from Plaid
  - **Key Features**:
    - Error handling with retry logic (via Polly)
    - Correlation ID passed to Plaid API
    - Response mapped to domain models (including transaction_type, merchant_category)
  - **Success Criteria**: T213 tests pass, adapter compiles, methods return expected types

- [ ] T205 [P] [US1] Create REST endpoint: POST /accounts/connect (get link token) in `backend/src/Modules/BankSync/API/Controllers/BankSyncController.cs`
  - **Details**: Endpoint that calls PlaidAdapter.CreateLinkToken(), returns { linkToken, expiresIn, expiresAt, requestId }. Authenticate with JWT
  - **Success Criteria**: T215 contract test passes, endpoint callable via HTTP, returns 200 with link token, 401 if no JWT

- [ ] T206 [P] [US1] Create REST endpoint: POST /accounts/link (exchange public token) in `backend/src/Modules/BankSync/API/Controllers/BankSyncController.cs`
  - **Details**: Endpoint that:
    1. Calls PlaidAdapter.ExchangePublicToken(publicToken)
    2. Get plaid_item_id from response
    3. Call PlaidAdapter.GetAccountsWithBalance(accessToken)
    4. For each account: create BankAccount entity, encrypt access token → EncryptedCredential
    5. Save to database
    6. Trigger initial sync job
    7. Return response with account details + "syncing" message
  - **Success Criteria**: T215 contract test passes, endpoint saves account to DB, stores encrypted token, returns 200 OK

- [ ] T207 [P] [US1] Create REST endpoint: GET /accounts (list all accounts) in `backend/src/Modules/BankSync/API/Controllers/BankSyncController.cs`
  - **Details**: Endpoint that:
    1. Gets authenticated user ID from JWT
    2. Queries database for all BankAccounts where user_id = authenticated user
    3. Filters by status/currency if provided in query params
    4. Returns list of accounts with: accountId, bankName, accountType, currency, currentBalance, availableBalance, syncStatus, lastSyncTimestamp
    5. Include aggregated currency_totals
  - **Success Criteria**: T215 contract test passes, returns accounts for authenticated user, respects filters

- [ ] T208 [P] [US1] Create REST endpoint: GET /accounts/{accountId}/transactions in `backend/src/Modules/BankSync/API/Controllers/BankSyncController.cs`
  - **Details**: Endpoint that:
    1. Verify authenticated user owns the account (user_id match)
    2. Query transactions with filters: start_date, end_date, offset, limit (pagination)
    3. Order by posted_date DESC
    4. Return transactions with: transactionId, amount, date, description, transactionType, merchantCategory, isPending, createdAt
    5. Include: totalCount, hasMore
  - **Success Criteria**: T215 contract test passes, returns paginated transactions, returns 404 if account not owned by user

- [ ] T209 [US1] Create command: ConnectBankAccountCommand + handler in `backend/src/Modules/BankSync/Application/Commands/ConnectBankAccountCommand.cs` and `ConnectBankAccountCommandHandler.cs`
  - **Details**: MediatR command for linking account. Handler:
    1. Validates publicToken is not expired
    2. Calls PlaidAdapter.ExchangePublicToken()
    3. Creates BankAccount aggregate
    4. Encrypts access token → EncryptedCredential
    5. Saves to repository
    6. Publishes event: BankAccountConnectedEvent
  - **Success Criteria**: Command compiles, handler saves account, event published

- [ ] T210 [US1] Create event handler: Initial sync on BankAccountConnectedEvent in `backend/src/Modules/BankSync/Application/EventHandlers/BankAccountConnectedEventHandler.cs`
  - **Details**: When BankAccountConnectedEvent published:
    1. Create SyncJob entity with status=pending
    2. Call PlaidAdapter.GetTransactions() for last 12 months
    3. Deduplicate transactions
    4. Bulk insert into database
    5. Update BankAccount.syncStatus → active
    6. Update SyncJob status → success
  - **Success Criteria**: Handler executes when event published, fetches and stores transactions

<!-- Integration tests: validate end-to-end after implementation -->

- [ ] T214 [P] [US1] Create integration tests: PlaidAdapter + Database in `backend/tests/Integration/BankSync/PlaidAdapterIntegrationTests.cs`
  - **Details**: Use testcontainers. Tests:
    - Link account: save BankAccount + EncryptedCredential to DB
    - Encrypted token stored correctly, plaintext never in DB
    - Transaction save: 100 transactions stored with correct hashes, transaction_type and merchant_category populated
  - **Success Criteria**: Tests pass, encrypted data in DB, no plaintext tokens

<!-- Frontend: US1 components -->

- [ ] T216 [US1] Create frontend service: BankSyncService in `frontend/src/app/modules/bank-sync/services/bank-sync.service.ts`
  - **Details**: Angular service with methods:
    - getLinkToken(): calls POST /accounts/connect, returns linkToken
    - exchangePublicToken(publicToken): calls POST /accounts/link
    - getAccounts(): calls GET /accounts
    - getTransactions(accountId, params): calls GET /accounts/{id}/transactions
  - **Success Criteria**: Service compiles, HTTP calls correct, mock interceptor works

- [ ] T217 [US1] Create frontend component: ConnectAccountComponent in `frontend/src/app/modules/bank-sync/pages/connect-account/connect-account.component.ts` and `.html`
  - **Details**: Component that integrates Plaid Link for account connection:
    1. Dependency: @plaid/link-web package (installed in T002)
    2. On load: fetch linkToken from BankSyncService (calls backend POST /accounts/connect)
    3. Render Plaid Link Button component (from @plaid/link-web)
    4. On user success: extract publicToken from Plaid callback
    5. Send publicToken to backend via BankSyncService.exchangePublicToken()
    6. Show "Account linked! Syncing transaction history..." message
    7. Poll GET /accounts until linked account shows syncStatus = active (max 60 seconds)
    8. Redirect to accounts list on completion
  - **Success Criteria**: Component renders, Plaid Link button opens, completes SDK flow, backend receives public token

- [ ] T218 [US1] Create frontend component: AccountsListComponent in `frontend/src/app/modules/bank-sync/pages/accounts-list/accounts-list.component.ts` and `.html`
  - **Details**: Component:
    1. Fetch accounts on load via BankSyncService
    2. Display table: Bank Name | Account Number (last 4) | Balance | Currency | Last Sync Time | Status
    3. Show "Connect Account" button to link new account
    4. Show status badge: Active (green), Syncing (yellow), Failed (red), Reauth Required (orange)
    5. Click account row → show transactions
  - **Success Criteria**: Component renders, fetches accounts, displays status correctly

- [ ] T219 [US1] Create frontend component: TransactionListComponent in `frontend/src/app/modules/bank-sync/pages/transaction-list/transaction-list.component.ts` and `.html`
  - **Details**: Component:
    1. Accept accountId as input parameter
    2. Fetch transactions for account via BankSyncService with pagination
    3. Display table: Date | Description | Amount | Type (Debit/Credit) | Category | Status (Pending/Posted)
    4. Pagination controls: Previous/Next buttons, show 50 transactions per page
    5. Filter by date range (start_date, end_date query params)
  - **Success Criteria**: Component renders, fetches transactions, pagination works

- [ ] T220 [US1] Create E2E test: Connect account + view transactions in `frontend/tests/integration/bank-sync/connect-account-flow.e2e.spec.ts`
  - **Details**: E2E test:
    1. Navigate to /connect-account
    2. Click "Connect Bank Account" button
    3. Plaid Link opens (mock)
    4. User selects bank account, enters credentials
    5. Verify redirected to /accounts
    6. Verify account appears in list
    7. Click account → see transactions
  - **Success Criteria**: Test passes with mock Plaid backend

**Phase 3 Checkpoint**:
- ✅ User can connect bank account via Plaid
- ✅ Transaction history fetched and stored (with transaction_type and merchant_category)
- ✅ Dashboard shows accounts + transactions
- ✅ Last sync timestamp visible
- ✅ Unit + integration + E2E tests passing (written first, then made green)
- ✅ API contracts validated
- **MVP-Ready**: Deploy Phase 1+2+3 to beta environment for user feedback

---

## Phase 4: User Story 2 (P2) - Automatic Background Sync & Status Tracking

**Objective**: Enable scheduled, automatic transaction syncing with webhook fallback and user-visible status.
**Duration**: 4-5 days
**Dependencies**: Phase 1 + Phase 2 + Phase 3 (US1) complete
**TDD Order**: Test skeletons first → service implementations → endpoint implementations

**Delivered**:
- Automatic sync every 2 hours (configurable)
- Real-time sync via Plaid webhooks
- User sees "Syncing..." status while sync in progress
- User can manually trigger sync
- Failed syncs show error message + retry countdown
- Sync metadata logged (transaction count, duration, error details)

### Phase 4 Tasks

<!-- TDD: Write unit + integration tests FIRST, then implement to make them pass -->

- [ ] T313 [P] [US2] Create unit tests for ScheduledSyncService in `backend/tests/Unit/BankSync/Application/ScheduledSyncServiceTests.cs`
  - **Details**: Tests:
    - Successful sync: creates job, fetches transactions, deduplicates, saves to DB
    - Failed sync: catches exception, updates status to failed
    - Deduplication: new transactions saved, duplicates filtered
    - Idempotency: same sync job ID run twice does not create duplicates
  - **Success Criteria**: All tests compile, ready to pass once T302 implemented

- [ ] T314 [P] [US2] Create unit tests for webhook handler in `backend/tests/Unit/BankSync/API/WebhookHandlerTests.cs`
  - **Details**: Tests:
    - Valid signature accepted, invalid rejected
    - TRANSACTIONS_READY enqueues sync job
    - ITEM_ERROR marks account for reauth
    - Non-matching webhook_type ignored
  - **Success Criteria**: All tests compile, mocks signature validation correctly

- [ ] T315 [P] [US2] Create integration tests: Hangfire job execution in `backend/tests/Integration/BankSync/HangfireJobExecutionTests.cs`
  - **Details**: Integration test:
    - Enqueue ScheduledSyncJob
    - Wait for execution (use Hangfire background worker)
    - Verify SyncJob created in DB with status=success
    - Verify transactions inserted
  - **Success Criteria**: Tests compile, ready to pass once T303/T304 implemented

<!-- Implementation: Make the tests above pass -->

- [ ] T301 [P] [US2] Create SyncJob domain entity (for audit trail) in `backend/src/Modules/BankSync/Domain/Aggregates/SyncJob.cs`
  - **Details**: Entity: sync_job_id, account_id, user_id, correlation_id, status, started_at, completed_at, transaction_count_fetched, transaction_count_deduped, retry_count, error_code, error_message, webhook_triggered. State machine: pending → in_progress → success/failed
  - **Success Criteria**: Entity compiles, state transitions work

- [ ] T302 [P] [US2] Create ScheduledSyncService (polling every 2 hours) in `backend/src/Modules/BankSync/Application/Services/ScheduledSyncService.cs`
  - **Details**: Service with method PerformFullSyncAsync(accountId):
    1. Create SyncJob with status=in_progress
    2. Decrypt access token from EncryptedCredential
    3. Call PlaidAdapter.GetTransactions(since last sync date, now)
    4. Deduplicate transactions
    5. Bulk insert new transactions
    6. Update BankAccount.current_balance, last_sync_timestamp
    7. Update SyncJob.status → success, transaction_count_fetched, transaction_count_deduped
    8. Handle errors: catch exception, update SyncJob.status → failed, error_code, error_message, retry_count
    9. Use exponential backoff retry policy (T102)
  - **Key Features**:
    - Idempotent: same sync job ID twice should not create duplicates
    - Tracks sync duration (milliseconds)
    - Never logs plaintext tokens
  - **Success Criteria**: T313 tests pass, service syncs transactions, updates sync metadata

- [ ] T303 [P] [US2] Set up Hangfire for background job scheduling in `backend/src/Modules/BankSync/Infrastructure/Jobs/HangfireSetup.cs` and `SyncScheduler.cs`
  - **Details**:
    - Register Hangfire using in-memory storage (for dev) or PostgreSQL (for production)
    - Create SyncScheduler service that enqueues ScheduledSyncJob every 2 hours for all active accounts
    - Implement recurring background job that runs on schedule
  - **Success Criteria**: T315 integration test passes, Hangfire starts up, job scheduled, Hangfire dashboard accessible

- [ ] T304 [P] [US2] Create Hangfire background job: ScheduledSyncJob in `backend/src/Modules/BankSync/Infrastructure/Jobs/ScheduledSyncJob.cs`
  - **Details**: Job class with method:
    ```csharp
    [AutomaticRetry(Attempts = 0)] // Retry handled by business logic
    public async Task ExecuteSyncAsync(Guid accountId)
    ```
    Calls ScheduledSyncService.PerformFullSyncAsync(accountId). Handles exceptions, logs errors
  - **Success Criteria**: Job executes when scheduled, completes successfully

- [ ] T305 [P] [US2] Create webhook endpoint: POST /webhook/plaid in `backend/src/Modules/BankSync/API/Controllers/BankSyncController.cs`
  - **Details**: Endpoint:
    1. Receive webhook from Plaid (POST)
    2. Verify signature with HMAC-SHA256 using Plaid webhook key (via T306)
    3. Extract webhook_type and webhook_code from payload
    4. Route based on webhook_type:
       - TRANSACTIONS_READY: enqueue immediate sync (IBackgroundJobClient.Enqueue)
       - ITEM_ERROR with code ITEM_LOGIN_REQUIRED: mark account as reauth_required, show in-app alert
       - ITEM_ERROR with code PRODUCT_NOT_READY: log and retry, don't mark reauth needed
       - SYNC_UPDATES_AVAILABLE: enqueue sync
    5. Log all webhook events with correlation ID (no plaintext sensitive data)
    6. Return 200 OK to acknowledge
  - **Success Criteria**: T314 webhook handler tests pass, endpoint routes to correct handler

- [ ] T306 [P] [US2] Implement webhook signature verification in `backend/src/Modules/Shared/Security/WebhookSignatureValidator.cs`
  - **Details**: Verifies Plaid webhook signature:
    - Expected signature = HMAC-SHA256(webhook_body, plaid_webhook_key)
    - Compare with provided signature header
    - Reject if mismatch
  - **Success Criteria**: Unit tests verify valid signatures pass, invalid signatures rejected

- [ ] T306-A [P] [US2] Create PlaidErrorMapper (translate Plaid errors → REST API responses) in `backend/src/Modules/BankSync/Infrastructure/Services/PlaidErrorMapper.cs`
  - **Details**: Service mapping Plaid error codes to user-friendly REST responses:
    | Plaid Code | HTTP Status | User Message |
    |---|---|---|
    | ITEM_LOGIN_REQUIRED | 401 | "Bank credentials expired. Please reconnect your account." |
    | RATE_LIMIT_EXCEEDED | 429 | "Rate limited. Retrying automatically..." |
    | INVALID_REQUEST | 400 | "Invalid request to bank. Please try again." |
    | SERVER_ERROR, INTERNAL_SERVER_ERROR | 503 | "Bank API temporarily unavailable. Retrying..." |
    | INVALID_CREDENTIALS | 401 | "Invalid bank credentials" |
    | PRODUCT_NOT_READY | 503 | "Bank data not ready yet. Will retry automatically." |
    | ITEM_ERROR (generic) | 500 | "Bank sync error. Please try again." |
  - **Key Features**:
    - Never expose internal error details to client
    - Log full technical errors internally (with correlation_id)
    - Mapping used in ErrorHandlingMiddleware (T501) for both API responses and webhook handlers
  - **Success Criteria**: All Plaid error codes have mappings, messages are user-friendly, never leaks sensitive data

- [ ] T307 [P] [US2] Create transaction sync coordinator (hybrid webhook + polling) in `backend/src/Modules/BankSync/Application/Services/TransactionSyncCoordinator.cs`
  - **Details**: Service that:
    - Handles webhook-triggered syncs (immediate)
    - Handles scheduled syncs (every 2 hours)
    - Prevents duplicate syncs (if sync already in progress for account, queue next)
    - Respects rate limits (Plaid: 20 items/min)
  - **Success Criteria**: Coordinator compiles, orchestrates both webhook and polling workflows

- [ ] T308 [US2] Create REST endpoint: POST /accounts/{accountId}/sync (manual trigger) in `backend/src/Modules/BankSync/API/Controllers/BankSyncController.cs`
  - **Details**: Endpoint:
    1. Verify authenticated user owns account
    2. Check if sync already in progress (return 409 Conflict if so)
    3. Enqueue manual sync job via IBackgroundJobClient
    4. Return 202 Accepted with jobId + "Sync queued"
  - **Success Criteria**: Endpoint enqueues job, returns 202

- [ ] T309 [US2] Create REST endpoint: GET /accounts/{accountId}/sync-status (poll sync progress) in `backend/src/Modules/BankSync/API/Controllers/BankSyncController.cs`
  - **Details**: Endpoint:
    1. Query SyncJob table for most recent job for account
    2. Return: status (pending/in_progress/success/failed), transaction_count_fetched, error_message, last_sync_timestamp
    3. If status = in_progress, return estimated time remaining
  - **Success Criteria**: Endpoint returns sync status correctly

- [ ] T309-A [US2] Create REST endpoint: DELETE /accounts/{accountId} (unlink/disconnect account) in `backend/src/Modules/BankSync/API/Controllers/BankSyncController.cs`
  - **Details**: Endpoint implementation:
    1. Verify authenticated user owns account (match user_id from JWT)
    2. Fetch BankAccount by accountId
    3. Soft-delete: set is_active = false, deleted_at = now, deleted_by_user_id = userId
    4. Soft-delete all Transactions for this account (set is_active = false, same timestamp)
    5. Leave EncryptedCredential intact (for audit/recovery if needed)
    6. Return 204 No Content on success
    7. Return 404 if account not found or already deleted
    8. Return 403 Forbidden if user doesn't own account
  - **Key Features**:
    - Soft-delete preserves audit trail (can query deleted accounts via is_active flag)
    - Idempotent: calling twice with same accountId returns 204 both times
  - **Success Criteria**: Account marked inactive, transactions marked inactive, 204 returned, idempotent

- [ ] T310 [US2] Create domain event: AccountSyncStartedEvent in `backend/src/Modules/BankSync/Domain/Events/AccountSyncStartedEvent.cs`
  - **Details**: Event published when sync starts. Include: accountId, correlationId, startedAt
  - **Success Criteria**: Event compiles, can be published

- [ ] T311 [US2] Create domain event: AccountSyncCompletedEvent in `backend/src/Modules/BankSync/Domain/Events/AccountSyncCompletedEvent.cs`
  - **Details**: Event published when sync completes (success or failure). Include: accountId, status, transactionCountFetched, errorMessage
  - **Success Criteria**: Event compiles, can be published

- [ ] T312 [US2] Create event handler: Update BankAccount status when sync completes in `backend/src/Modules/BankSync/Application/EventHandlers/SyncCompletionEventHandler.cs`
  - **Details**: When AccountSyncCompletedEvent published:
    - Update BankAccount.sync_status → active (if success) or failed (if failed)
    - Update BankAccount.last_sync_timestamp (if success)
    - Persist changes
  - **Success Criteria**: Handler executes, updates BankAccount correctly

- [ ] T316 [US2] Create frontend service: extend BankSyncService with sync methods in `frontend/src/app/modules/bank-sync/services/bank-sync.service.ts`
  - **Details**: Add methods:
    - triggerSync(accountId): POST /accounts/{id}/sync
    - getSyncStatus(accountId): GET /accounts/{id}/sync-status (poll every 2 seconds while status != success/failed)
    - stopPolling(): cancel polling
  - **Success Criteria**: Methods compile, polling logic correct

- [ ] T317 [US2] Create frontend component: SyncStatusComponent in `frontend/src/app/modules/bank-sync/components/sync-status/sync-status.component.ts` and `.html`
  - **Details**: Component:
    1. Accept accountId as input
    2. Display status badge: Syncing (animated spinner), Success (checkmark), Failed (error icon with message)
    3. Show "Last synced X minutes ago" + transaction count fetched
    4. Show "Sync Now" button
    5. On button click: call triggerSync(), poll status until completion
    6. If failed: show error message, retry button
  - **Success Criteria**: Component renders, polling works, status updates in real-time

- [ ] T318 [US2] Update AccountsListComponent to show sync status + "Sync Now" button in `frontend/src/app/modules/bank-sync/pages/accounts-list/accounts-list.component.html`
  - **Details**: Add column to table: SyncStatus | LastSyncTime. Add "Sync Now" button in each row. Import SyncStatusComponent
  - **Success Criteria**: Component renders sync status, button functional

- [ ] T319 [US2] Create E2E test: Manual sync trigger + status polling in `frontend/tests/integration/bank-sync/manual-sync-flow.e2e.spec.ts`
  - **Details**: E2E test:
    1. Navigate to /accounts
    2. Click "Sync Now" button
    3. Verify status shows "Syncing..."
    4. Wait for status to change to "Success"
    5. Verify "Last synced X seconds ago" appears
  - **Success Criteria**: Test passes with mock backend sync

- [ ] T320 [US2] Create Hangfire dashboard view (optional - for ops monitoring) in `backend/src/Modules/BankSync/Infrastructure/Jobs/HangfireDashboardSetup.cs`
  - **Details**: Enable Hangfire dashboard at /hangfire (with admin authentication check)
  - **Success Criteria**: Dashboard accessible, shows scheduled jobs + execution history

**Phase 4 Checkpoint**:
- ✅ Automatic sync runs every 2 hours
- ✅ Webhooks trigger real-time sync
- ✅ User can manually trigger sync
- ✅ Sync status visible to user (Syncing... → Success/Failed)
- ✅ Failed syncs show error message + retry logic
- ✅ Sync metadata logged (count, duration, error)
- ✅ Unit + integration tests written first (TDD), all passing
- **Ready for Phase 5**: Multiple accounts can sync in parallel

---

## Phase 5: User Story 3 (P3) - Multi-Account Aggregation & Money Flow Statistics

**Objective**: Aggregate balances and transactions across multiple accounts, show unified dashboard with statistics.
**Duration**: 4-5 days
**Dependencies**: Phase 1 + Phase 2 + Phase 3 (US1) + Phase 4 (US2) complete
**TDD Order**: Test skeletons first → service implementations → endpoint implementations

**Delivered**:
- Dashboard shows total balance across all accounts (grouped by currency)
- Monthly inflow/outflow statistics over 6 months
- Top spending categories
- Account-by-account breakdown
- Handles multi-currency correctly (no currency conversion)

### Phase 5 Tasks

<!-- TDD: Write unit + integration tests FIRST, then implement to make them pass -->

- [ ] T412 [P] [US3] Create unit tests for AggregationService in `backend/tests/Unit/BankSync/Application/AggregationServiceTests.cs`
  - **Details**: Tests:
    - Aggregate 3 accounts (2 EUR, 1 USD) → correct totals by currency
    - Handle NULL balances → exclude from sum
    - Empty account list → return empty dict
    - available_balance aggregation works separately from current_balance
  - **Success Criteria**: All tests compile, ready to pass once T401 implemented

- [ ] T413 [P] [US3] Create unit tests for MoneyFlowStatisticsService in `backend/tests/Unit/BankSync/Application/MoneyFlowStatisticsTests.cs`
  - **Details**: Tests:
    - Calculate 6 months of flow (inflow, outflow, net)
    - Correct grouping by month
    - Pending transactions excluded from stats
    - Handles multi-currency (separate stat per currency)
    - Debit/credit classification via transaction_type field
  - **Success Criteria**: All tests compile, ready to pass once T402 implemented

- [ ] T414 [P] [US3] Create integration tests: Dashboard aggregation queries in `backend/tests/Integration/BankSync/DashboardAggregationTests.cs`
  - **Details**: Integration test with real DB:
    - Create 3 accounts (EUR, USD, EUR)
    - Create 50 transactions with varying transaction_type and merchant_category
    - Query aggregated balance
    - Verify: EUR total = 10000, USD total = 1000
    - Query monthly flow → verify 6 months data
    - Query top categories → verify sorted by spend DESC
  - **Success Criteria**: Tests compile, ready to pass once T401–T408 implemented; queries efficient (< 100ms)

<!-- Implementation: Make the tests above pass -->

- [ ] T401 [P] [US3] Create AggregationService (balance sum + currency grouping) in `backend/src/Modules/BankSync/Application/Services/AggregationService.cs`
  - **Details**: Service with methods:
    - GetAggregatedBalanceAsync(userId): Sum current_balance by currency, return dict { Currency → Total }
    - GetAggregatedAvailableBalance(userId): Same for available_balance
    - GetAccountCountByType(userId): Count accounts by type (checking, savings, credit)
  - **Key Features**:
    - Groups by currency (EUR, USD, GBP, UAH, etc.)
    - Handles accounts with NULL balances
    - Efficient DB queries (single aggregation query, not N+1)
  - **Success Criteria**: T412 tests pass, queries are efficient (1 query), values sum correctly

- [ ] T402 [P] [US3] Create MoneyFlowStatisticsService (monthly inflow/outflow) in `backend/src/Modules/BankSync/Application/Services/MoneyFlowStatisticsService.cs`
  - **Details**: Service with method:
    - GetMonthlyFlowAsync(userId, months=6): Returns array of {month, inflow, outflow, net} for last 6 months
    - Inflow = sum of transactions where transaction_type = 'credit'
    - Outflow = sum of transactions where transaction_type = 'debit'
    - Net = inflow - outflow
    - Group by currency-month combo
    - Exclude pending transactions (is_pending = true)
  - **Success Criteria**: T413 tests pass, calculations correct, queries efficient

- [ ] T403 [P] [US3] Create MerchantCategoryStatisticsService (spending by category) in `backend/src/Modules/BankSync/Application/Services/MerchantCategoryStatisticsService.cs`
  - **Details**: Service with method:
    - GetTopCategoriesAsync(userId, limit=10): Returns top spending categories (from merchant_category field)
    - Calculate total spend per category
    - Return sorted by spend DESC
    - Include % of total spend
  - **Success Criteria**: Service compiles, categories sorted by spend, percentages sum to 100%

- [ ] T404 [US3] Create DashboardQueryService (unified dashboard queries) in `backend/src/Modules/BankSync/Application/Services/DashboardQueryService.cs`
  - **Details**: Service that aggregates all dashboard data:
    - Get aggregated balance
    - Get monthly flow stats
    - Get top categories
    - Get account count by type
    - Get last sync timestamp across all accounts
    - Return single dashboard DTO
  - **Success Criteria**: Service compiles, returns single DTO with all data

- [ ] T405 [P] [US3] Create GetAggregatedBalanceQuery + handler in `backend/src/Modules/BankSync/Application/Queries/GetAggregatedBalanceQuery.cs` and `GetAggregatedBalanceQueryHandler.cs`
  - **Details**: Query handler:
    1. Get userId from context
    2. Call AggregationService.GetAggregatedBalanceAsync()
    3. Return { balances: { EUR: 5000, USD: 1000 }, totalAccountCount: 3 }
  - **Success Criteria**: Query compiles, handler executes, returns correct totals

- [ ] T406 [P] [US3] Create GetMoneyFlowStatisticsQuery + handler in `backend/src/Modules/BankSync/Application/Queries/GetMoneyFlowStatisticsQuery.cs` and `GetMoneyFlowStatisticsQueryHandler.cs`
  - **Details**: Query handler:
    1. Get userId from context
    2. Call MoneyFlowStatisticsService.GetMonthlyFlowAsync(userId, months=6)
    3. Return array of monthly stats
  - **Success Criteria**: Query compiles, returns 6 months of data

- [ ] T407 [P] [US3] Create GetTopCategoriesQuery + handler in `backend/src/Modules/BankSync/Application/Queries/GetTopCategoriesQuery.cs` and `GetTopCategoriesQueryHandler.cs`
  - **Details**: Query handler:
    1. Get userId from context
    2. Call MerchantCategoryStatisticsService.GetTopCategoriesAsync(userId, limit=10)
    3. Return array of { category, totalSpend, percentOfTotal }
  - **Success Criteria**: Query compiles, returns top 10 categories

- [ ] T408 [US3] Create REST endpoint: GET /dashboard/aggregated (unified dashboard) in `backend/src/Modules/BankSync/API/Controllers/BankSyncController.cs`
  - **Details**: Endpoint:
    1. Get userId from JWT
    2. Call DashboardQueryService.GetDashboardDataAsync(userId)
    3. Return all dashboard data:
       ```json
       {
         "aggregatedBalance": { "EUR": 5000, "USD": 1000 },
         "aggregatedAvailableBalance": { "EUR": 4800, "USD": 950 },
         "accountCount": 3,
         "accountsByType": { "checking": 2, "savings": 1 },
         "monthlyFlow": [{ "month": "2026-03", "inflow": 3000, "outflow": 2500, "net": 500 }],
         "topCategories": [{ "category": "Groceries", "totalSpend": 500, "percentOfTotal": 10 }],
         "lastSyncTimestamp": "2026-03-21T12:30:00Z"
       }
       ```
  - **Success Criteria**: T414 integration test passes, endpoint returns 200 with complete dashboard data

- [ ] T409 [US3] Create TransferDetectionService (detect internal transfers) in `backend/src/Modules/BankSync/Application/Services/TransferDetectionService.cs`
  - **Details**: Service to identify internal transfers (same user, both accounts):
    - CompareTransactionsForTransferAsync(debitTxn, creditTxn): returns true if likely same transfer
    - Logic: same amount, date within 1 day, transaction_type = 'transfer', description similar
  - **Success Criteria**: Service compiles, can be used in aggregation to avoid double-counting

- [ ] T410 [US3] Create REST endpoint: GET /dashboard/transfers (internal transfer view) in `backend/src/Modules/BankSync/API/Controllers/BankSyncController.cs`
  - **Details**: Endpoint that shows linked internal transfers (debit + credit pairs)
  - **Success Criteria**: Endpoint returns transfer pairs

- [ ] T411 [US3] Create query cache for aggregation queries (optional - Phase 5) in `backend/src/Modules/Shared/Caching/QueryCacheService.cs`
  - **Details**: Decorator to cache aggregation query results (5-minute TTL). Invalidate on new transaction sync
  - **Success Criteria**: Cache works, invalidates on sync

- [ ] T415 [US3] Create frontend DashboardComponent (main feature display) in `frontend/src/app/modules/bank-sync/pages/dashboard/dashboard.component.ts` and `.html`
  - **Details**: Component:
    1. Fetch dashboard data on load via BankSyncService
    2. Display: Total balance card (grouped by currency), account count
    3. Display: Monthly flow chart (6 months, inflow/outflow trend)
    4. Display: Top categories pie chart
    5. Display: Account breakdown table (bank, type, balance, currency)
    6. Refresh interval every 5 minutes
  - **Success Criteria**: Component renders all elements, data shows correctly

- [ ] T416 [US3] Create frontend service: extend BankSyncService with aggregation methods in `frontend/src/app/modules/bank-sync/services/bank-sync.service.ts`
  - **Details**: Add methods:
    - getDashboardData(): GET /dashboard/aggregated (returns all dashboard data in one call)
  - **Success Criteria**: Method compiles, HTTP call correct, dashboard data mapped to local model

- [ ] T417 [US3] Create frontend chart component: MoneyFlowChart (line/bar chart) in `frontend/src/app/modules/bank-sync/components/money-flow-chart/money-flow-chart.component.ts` and `.html`
  - **Details**: Component using ngx-charts or Chart.js:
    - Accept monthlyFlowData as input
    - Render bar chart with months on X-axis, inflow (green) + outflow (red) on Y-axis
    - Show tooltip with exact values on hover
  - **Success Criteria**: Chart renders correctly, data points accurate

- [ ] T418 [US3] Create frontend chart component: CategoryBreakdownChart (pie chart) in `frontend/src/app/modules/bank-sync/components/category-breakdown-chart/category-breakdown-chart.component.ts` and `.html`
  - **Details**: Component using ngx-charts:
    - Accept topCategoriesData as input
    - Render pie chart showing % spend per category
    - Show labels with category name + % on each slice
  - **Success Criteria**: Chart renders, percentages sum to 100%

- [ ] T419 [US3] Create E2E test: Dashboard shows aggregated data in `frontend/tests/integration/bank-sync/dashboard-aggregation-flow.e2e.spec.ts`
  - **Details**: E2E test:
    1. Navigate to /dashboard
    2. Verify title "Financial Overview"
    3. Verify aggregated balance displayed
    4. Verify monthly flow chart rendered
    5. Verify top categories list shown
  - **Success Criteria**: Test passes with mock data

- [ ] T420 [US3] Create documentation: Multi-currency handling guide in `docs/MULTI_CURRENCY_GUIDE.md`
  - **Details**: Document explaining how system handles multiple currencies:
    - No automatic conversion (EUR + USD not summed)
    - Currency totals shown separately
    - Exchange rates deferred to future feature
  - **Success Criteria**: Documentation clear, examples provided

**Phase 5 Checkpoint**:
- ✅ Aggregated balance shown (grouped by currency, both current and available)
- ✅ Monthly flow statistics for 6 months (using transaction_type field)
- ✅ Top spending categories identified (using merchant_category field)
- ✅ Account breakdown visible
- ✅ Charts render correctly
- ✅ Multi-currency handled properly
- ✅ Unit + integration tests written first (TDD), all passing
- **Ready for Phase 6**: Polish + cross-cutting concerns

---

## Phase 6: Polish & Cross-Cutting Concerns

**Objective**: Error handling, security hardening, audit logging, performance optimization, documentation, final testing.
**Duration**: 4-5 days
**Dependencies**: Phase 1-5 complete
**Parallelizable**: All tasks can be done in parallel

### Phase 6 Tasks

- [ ] T501 [P] Implement comprehensive error handling + user-friendly messages in `backend/src/Modules/BankSync/API/Middleware/ErrorHandlingMiddleware.cs` and `backend/src/Modules/Shared/Exceptions/BankSyncException.cs`
  - **Details**: Error handler that:
    - Catches specific exceptions (PlaidApiException, DbUpdateException, etc.)
    - Delegates to PlaidErrorMapper (T306-A) for Plaid-specific error codes
    - Returns appropriate HTTP status + user-friendly message
    - Logs technical details (never sent to client)
    - No stack traces in production
  - **Example Responses**:
    - Plaid credential invalid → 400 "Bank credentials expired. Please reconnect your account."
    - DB connection lost → 503 "Database unavailable. Try again in 1 minute."
    - Rate limit → 429 "Too many requests. Please wait before trying again."
  - **Success Criteria**: All exception paths tested, messages user-friendly

- [ ] T502 [P] Implement security: Rate limiting on API endpoints in `backend/src/Modules/Shared/Security/RateLimitingMiddleware.cs`
  - **Details**: Middleware that rate-limits:
    - Anonymous users: 10 req/min
    - Authenticated users: 100 req/min per endpoint
    - Plaid webhook endpoint: exempt from rate limit
    - Return 429 with Retry-After header
  - **Success Criteria**: Rate limiting blocks requests over limit, allows legit traffic

- [ ] T503 [P] Implement security: Validate JWT token on every request in `backend/src/Modules/Shared/Security/JwtAuthenticationMiddleware.cs`
  - **Details**: Middleware:
    - Extract Bearer token from Authorization header
    - Validate signature using JWT secret
    - Reject expired tokens
    - Reject invalid signatures
    - Attach user ID to HttpContext
  - **Success Criteria**: Middleware validates correctly, rejects invalid tokens

- [ ] T504 [P] Implement CORS for frontend origin in `backend/src/Startup/Program.cs`
  - **Details**: Configure CORS to allow:
    - Origin: http://localhost:4200 (dev), https://finance-sentry.com (prod)
    - Methods: GET, POST, PUT, DELETE
    - Headers: Authorization, Content-Type
    - Credentials: true
  - **Success Criteria**: Preflight requests handled, frontend can call API

- [ ] T505 [P] Implement database query performance monitoring in `backend/src/Modules/Shared/Performance/EFQueryLoggerInterceptor.cs`
  - **Details**: EF Core query interceptor that:
    - Logs slow queries (> 100ms)
    - Counts database round-trips per request
    - Warns on N+1 query patterns
  - **Success Criteria**: Slow queries logged, N+1 patterns detected

- [ ] T506 [P] Add API response pagination best practices in `backend/src/Modules/Shared/API/PaginationExtensions.cs`
  - **Details**: Extension methods:
    - ApplyPagination(query, offset, limit): apply pagination to IQueryable
    - CreatePaginatedResponse(items, totalCount): wrap in pagination metadata
    - Validate: offset >= 0, limit <= 100
  - **Success Criteria**: All paginated endpoints use extension, validation works

- [ ] T507 [P] Create API documentation (OpenAPI/Swagger) in `backend/src/Startup/Program.cs` and `docs/SWAGGER.md`
  - **Details**: Configure Swagger:
    - Document all endpoints: /accounts/connect, /accounts/link, /accounts, /accounts/{id}/transactions, /dashboard/aggregated, /dashboard/transfers
    - Include request/response examples
    - Document authentication: Bearer JWT
    - Document error codes
  - **Success Criteria**: Swagger UI renders at /swagger, all endpoints documented

- [ ] T508 [P] Create postman collection for manual API testing in `docs/Bank-Sync-API.postman_collection.json`
  - **Details**: Postman collection with:
    - All endpoints preconfigured
    - Environment variables for baseUrl, token, accountId
    - Example requests + responses
    - Tests for status codes
  - **Success Criteria**: Postman collection importable, requests work

- [ ] T509 [P] Add input validation to all REST endpoints in `backend/src/Modules/BankSync/API/Validators/`
  - **Details**: FluentValidation rules:
    - publicToken: required, max 100 chars
    - startDate: valid ISO date, <= today
    - endDate: valid ISO date, >= startDate
    - offset: >= 0
    - limit: 1-100
  - **Success Criteria**: All endpoints validate input, return 400 with errors

- [ ] T510 [P] Create database backup & recovery strategy documentation in `docs/DATABASE_BACKUP.md`
  - **Details**: Document:
    - Backup frequency (daily, weekly)
    - Recovery procedure
    - Encryption key backup
    - Point-in-time recovery process
  - **Success Criteria**: Documentation complete, recovery tested

- [ ] T511 [P] Implement structured logging to cloud sink (Application Insights / CloudWatch) in `backend/src/Startup/Program.cs`
  - **Details**: Configure Serilog to send logs to:
    - Development: console + file
    - Production: Application Insights / CloudWatch
    - Include custom properties: correlation_id, user_id, operation (never plaintext tokens)
  - **Success Criteria**: Logs appear in cloud sink, structured fields searchable

- [ ] T512 [P] Create health check endpoint: GET /health in `backend/src/Modules/Shared/Health/HealthCheckController.cs`
  - **Details**: Endpoint that checks:
    - Database connectivity (query SELECT 1)
    - Plaid API connectivity (make dummy call, don't count requests)
    - Return: { status: "healthy" | "degraded" | "unhealthy", checks: { database, plaid } }
  - **Success Criteria**: Endpoint responds 200 healthy, 503 unhealthy

- [ ] T513 [P] Create database maintenance job: Backup encrypted credentials in `backend/src/Modules/Shared/Jobs/CredentialBackupJob.cs`
  - **Details**: Background job (runs weekly):
    - Extract all encrypted credentials
    - Create encrypted backup file
    - Store in secure location (S3 with encryption)
    - Log completion (never log plaintext)
  - **Success Criteria**: Job runs, backup created, can be restored

- [ ] T514 Create security audit: Penetration test checklist in `docs/SECURITY_AUDIT.md`
  - **Details**: Checklist:
    - SQL injection: parameterized queries ✓
    - XSS: sanitized output ✓
    - CSRF: token validation ✓
    - Credential exposure: encrypted storage ✓
    - Rate limiting: implemented ✓
  - **Success Criteria**: Checklist completed, vulnerabilities documented

- [ ] T515 Create frontend security: Angular Content Security Policy in `frontend/src/index.html`
  - **Details**: Add CSP headers:
    - script-src 'self' cdn.plaid.com
    - style-src 'self' 'unsafe-inline'
    - img-src 'self' data:
  - **Success Criteria**: CSP headers set, browser console no violations

- [ ] T516 Create frontend performance: Tree-shaking + code splitting in `frontend/angular.json`
  - **Details**: Configure Angular build:
    - Production: minification, tree-shaking, bundlebudgets
    - Code-splitting: lazy-load bank-sync module
    - Compress: enable gzip
  - **Success Criteria**: Build succeeds, bundle size < 500KB (gzipped)

- [ ] T517 [P] Create end-to-end performance test in `backend/tests/Performance/E2EPerformanceTest.cs`
  - **Details**: Test that simulates:
    - Connect 50 accounts (measure time, validates SC-008)
    - Sync 500 transactions each (measure time)
    - Query aggregated balance (measure time)
    - Assert: sync < 5 min, query < 1 sec across 50 accounts
  - **Success Criteria**: Test passes, performance SLAs met (SC-006, SC-008 validated)

- [ ] T518 [P] Create load testing script (k6 / Artillery) in `tests/load/bank-sync-load-test.js`
  - **Details**: Script that:
    - Simulates 100 concurrent users
    - Each user: connect account → sync → query dashboard
    - Measure response times, error rates
    - Assert: p99 latency < 5 sec, error rate < 1%
  - **Success Criteria**: Script runs, results captured

- [ ] T519 [P] Update README with setup + deployment instructions in `README.md`
  - **Details**: Add:
    - Feature overview
    - Local development setup (clone → docker-compose up)
    - Running tests (dotnet test)
    - Deployment steps
    - Environment variables
  - **Success Criteria**: Instructions clear, tested by new developer

- [ ] T520 Create QA Test Plan document in `docs/QA_TEST_PLAN.md`
  - **Details**: Document:
    - Manual test cases per user story (US1, US2, US3)
    - Edge cases to test (credential expiry, bank outage, etc.)
    - Regression test checklist
    - Browser/device compatibility matrix
  - **Success Criteria**: Test plan comprehensive, covers all scenarios

- [ ] T521 [P] Implement feature flag for gradual rollout in `backend/src/Modules/Shared/FeatureFlags/FeatureFlagService.cs`
  - **Details**: Service using environment variable or LaunchDarkly:
    - Feature: "BANK_SYNC_ENABLED"
    - Rollout: 10% → 50% → 100% of users
    - API returns 503 or hides UI if disabled
  - **Success Criteria**: Feature can be toggled via config, no code changes needed

- [ ] T522 [P] Create data migration guide: How to export/import bank data in `docs/DATA_EXPORT_GUIDE.md`
  - **Details**: Explain:
    - Export format: JSON with encrypted credentials masked
    - Import format: restore from backup
    - GDPR compliance: how to delete user data
  - **Success Criteria**: Guide clear, tested

- [ ] T523 [P] Create operations runbook: Troubleshooting common issues in `docs/OPERATIONS_RUNBOOK.md`
  - **Details**: Document solutions for:
    - Sync failing: check Plaid API health, check network
    - High error rate: check database connections, check rate limits
    - Slow queries: identify N+1 patterns, add index
  - **Success Criteria**: Runbook useful, covers top 10 issues

<!-- CONSTITUTION V COMPLIANCE: Audit logging — "Audit logs record all data access" (MUST) -->

- [ ] T524 [P] Create audit_log DB table and EF Core migration in `backend/src/Migrations/M002_AuditLogSchema.cs` and `backend/src/Modules/Shared/Persistence/AuditLogDbContext.cs`
  - **Details**:
    - New table `audit_logs` with columns: audit_id (UUID PK), user_id (UUID, indexed), action (VARCHAR 50: READ_ACCOUNT, READ_TRANSACTIONS, WRITE_ACCOUNT, DELETE_ACCOUNT, CREDENTIAL_ACCESS, SYNC_TRIGGERED), resource_type (VARCHAR 50), resource_id (UUID), ip_address (VARCHAR 45), user_agent (TEXT), performed_at (TIMESTAMPTZ NOT NULL), correlation_id (VARCHAR 64)
    - Add EF Core entity `AuditLog` to AuditLogDbContext
    - Migration creates table + indexes on (user_id, performed_at DESC) and (resource_type, resource_id)
    - Never log sensitive data (no tokens, no account numbers beyond last 4)
  - **Success Criteria**: Migration applies cleanly, rollback succeeds, table exists in PostgreSQL

- [ ] T525 [P] Implement AuditLogService recording all data access events in `backend/src/Modules/Shared/AuditLog/IAuditLogService.cs` and `backend/src/Modules/Shared/AuditLog/AuditLogService.cs`
  - **Details**:
    - Interface: `Task LogAsync(string userId, string action, string resourceType, string resourceId, string correlationId)`
    - Implementation writes a row to audit_logs table asynchronously (fire-and-forget; never blocks request path)
    - Register in DependencyInjection.cs
    - Inject into BankSyncController and call on every endpoint:
      - GET /accounts → log READ_ACCOUNT per account returned
      - GET /accounts/{id}/transactions → log READ_TRANSACTIONS
      - POST /accounts/link → log WRITE_ACCOUNT
      - DELETE /accounts/{id} → log DELETE_ACCOUNT
      - POST /accounts/{id}/sync → log SYNC_TRIGGERED
      - Any credential decrypt (CredentialEncryptionService) → log CREDENTIAL_ACCESS
    - Never log plaintext credentials, tokens, or full account numbers
  - **Success Criteria**: AuditLog rows created for every data access, service registered in DI, no performance impact (async write)

- [ ] T526 [P] Create integration tests for audit logging in `backend/tests/Integration/Shared/AuditLogServiceTests.cs`
  - **Details**: Integration tests using testcontainers PostgreSQL:
    - Test: Call GET /accounts → verify audit_logs row inserted with action=READ_ACCOUNT, correct user_id and resource_id
    - Test: Call DELETE /accounts/{id} → verify audit_logs row with action=DELETE_ACCOUNT
    - Test: Call POST /accounts/link → verify WRITE_ACCOUNT and CREDENTIAL_ACCESS rows inserted
    - Test: Verify no sensitive data in audit_logs (no tokens, no plaintext passwords)
    - Test: Audit log write failure does not break the main request (graceful degradation)
  - **Success Criteria**: All tests pass, audit rows present in DB, sensitive data absent from audit_logs

**Phase 6 Checkpoint**:
- ✅ Error handling comprehensive, user-friendly
- ✅ Security hardened (rate limiting, JWT, CORS, CSP)
- ✅ **Audit logging implemented** (Constitution V compliant — all data access recorded)
- ✅ Performance validated (50-account load test passing)
- ✅ Monitoring configured (Application Insights / CloudWatch)
- ✅ API documented (Swagger + Postman)
- ✅ Operations runbook complete
- **Production-Ready**: Full feature deployed and monitored
