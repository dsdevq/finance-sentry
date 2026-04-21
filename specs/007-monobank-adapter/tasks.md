# Tasks: Monobank Bank Provider Adapter

**Input**: Design documents from `specs/007-monobank-adapter/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

**Tests**: Constitution mandates:
- Contract tests for external API integrations (Monobank API): MANDATORY
- Contract tests for new REST endpoints: MANDATORY (`POST /accounts/monobank/connect`)
- Unit tests for business logic: MANDATORY (>80% coverage)

---

## Phase 1: Setup (No-op)

No new project or dependency setup required. Monobank API is called via plain `HttpClient` — no new NuGet packages.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain interfaces, data model changes, and migration. MUST complete before any user story.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T001 Create `IBankProvider` and `IBankProviderFactory` interfaces in `backend/src/FinanceSentry.Modules.BankSync/Domain/Interfaces/IBankProvider.cs`
- [X] T002 Create `MonobankCredential` domain entity in `backend/src/FinanceSentry.Modules.BankSync/Domain/MonobankCredential.cs`
- [X] T003 Modify `BankAccount` entity: rename `PlaidItemId` → `ExternalAccountId`; add `Provider` (string, default `"plaid"`); add nullable `MonobankCredentialId` (Guid?) and navigation property in `backend/src/FinanceSentry.Modules.BankSync/Domain/BankAccount.cs`
- [X] T004 Add `IMonobankCredentialRepository` to `backend/src/FinanceSentry.Modules.BankSync/Domain/Repositories/IRepositories.cs`
- [X] T005 Update `BankSyncDbContext`: add `DbSet<MonobankCredential>`, configure `MonobankCredentials` table (indexes, FK, encrypted column constraints), rename `PlaidItemId` → `ExternalAccountId` in fluent config in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Persistence/BankSyncDbContext.cs`
- [X] T006 Add EF Core migration `M002_MonobankProvider` covering: rename column, add `Provider` + `MonobankCredentialId` columns to `BankAccounts`, create `MonobankCredentials` table, FK, indexes in `backend/src/FinanceSentry.Modules.BankSync/Migrations/`
- [X] T007 Implement `MonobankCredentialRepository` in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Persistence/Repositories/Repositories.cs`
- [X] T008 Modify `PlaidAdapter` to implement `IBankProvider` (add `ProviderName`, `GetAccountsAsync`, `SyncTransactionsAsync`, `DisconnectAsync` delegating to existing methods) in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Plaid/PlaidAdapter.cs`
- [X] T009 Implement `BankProviderFactory` in `backend/src/FinanceSentry.Modules.BankSync/Application/Services/BankProviderFactory.cs` that resolves `IBankProvider` by `BankAccount.Provider` string
- [X] T010 Register `IBankProviderFactory`, `BankProviderFactory`, `IMonobankCredentialRepository`, `MonobankCredentialRepository` in `backend/src/FinanceSentry.API/Program.cs`

**Checkpoint**: Domain interfaces exist; DB schema updated; PlaidAdapter is IBankProvider-compliant; factory resolves Plaid.

---

## Phase 3: User Story 1 — Connect Monobank Account (Priority: P1) 🎯 MVP

**Goal**: User submits Monobank personal token → system validates, stores credential, creates account rows, shows accounts with balances.

**Independent Test**: POST a valid token to `/api/v1/accounts/monobank/connect`, confirm 201 response with account list and `provider: "monobank"`. Check DB: `MonobankCredentials` has 1 row; `BankAccounts` has N rows with `Provider = 'monobank'`.

### Contract Tests for User Story 1

- [ ] T011 [P] [US1] Write contract test for `POST /api/v1/accounts/monobank/connect` (201, 400 invalid token, 409 duplicate, 429 rate limit) in `backend/tests/FinanceSentry.Tests/Monobank/ConnectMonobankContractTests.cs`
- [ ] T012 [P] [US1] Write contract test for Monobank `GET /personal/client-info` response mapping (happy path + 401 error) using mocked HTTP handler in `backend/tests/FinanceSentry.Tests/Monobank/MonobankClientInfoContractTests.cs`

### Implementation for User Story 1

- [X] T013 [P] [US1] Create `MonobankException` in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Monobank/MonobankException.cs`
- [X] T014 [P] [US1] Create `MonobankAdapterModels.cs` with `MonobankAccountInfo` and `MonobankClientInfo` records in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Monobank/MonobankAdapterModels.cs`
- [X] T015 [P] [US1] Create `IMonobankAdapter` interface (connect, getAccounts, getStatements) in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Monobank/IMonobankAdapter.cs`
- [X] T016 [US1] Implement `MonobankHttpClient` covering `GET /personal/client-info` and `GET /personal/statement/{account}/{from}/{to}` with `X-Token` auth, ISO 4217 numeric→alphabetic currency mapping, amount÷100 conversion, and HTTP 429/401 error handling in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Monobank/MonobankHttpClient.cs`
- [X] T017 [US1] Implement `MonobankAdapter.ConnectAsync` and `MonobankAdapter.GetAccountsAsync` (validate token, map client-info accounts to `MonobankAccountInfo`, implement `IBankProvider.GetAccountsAsync`) in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Monobank/MonobankAdapter.cs`
- [X] T018 [US1] Create `ConnectMonobankAccountCommand` MediatR command + handler: validate token via `IMonobankAdapter.ConnectAsync`, create/update `MonobankCredential`, create `BankAccount` rows with `Provider="monobank"`, return account list in `backend/src/FinanceSentry.Modules.BankSync/Application/Commands/ConnectMonobankAccountCommand.cs`
- [X] T019 [US1] Add `POST /api/v1/accounts/monobank/connect` endpoint to `BankSyncController` dispatching `ConnectMonobankAccountCommand`; return 201/400/409/429 per contract in `backend/src/FinanceSentry.Modules.BankSync/API/Controllers/BankSyncController.cs`
- [X] T020 [US1] Register `IMonobankAdapter`, `MonobankAdapter`, `MonobankHttpClient` (via `AddHttpClient`) in `backend/src/FinanceSentry.API/Program.cs`; add `Monobank:BaseUrl` config key (default `https://api.monobank.ua`)
- [X] T021 [US1] Add `connectMonobank(token: string)` method to frontend `BankSyncService` calling `POST /api/v1/accounts/monobank/connect` in `frontend/src/app/modules/bank-sync/services/bank-sync.service.ts`
- [X] T022 [US1] Extend `ConnectAccountComponent` with provider selection (Plaid / Monobank) and Monobank token input form; Monobank flow calls `connectMonobank()` and navigates to accounts list on success in `frontend/src/app/modules/bank-sync/pages/connect-account/connect-account.component.ts` and `.html`

**Checkpoint**: User can connect a Monobank account; accounts appear in the list with balance and `provider: monobank` label.

---

## Phase 4: User Story 2 — View Monobank Transactions (Priority: P2)

**Goal**: After initial connect, user sees 90 days of transaction history for their Monobank account — same transaction list UI as Plaid accounts.

**Independent Test**: After connecting, navigate to the transaction list for the Monobank account. Verify transactions appear with amount (decimal, not kopecks), correct UAH currency code, description, and date. Verify no duplicate transactions on re-import.

### Contract Tests for User Story 2

- [ ] T023 [P] [US2] Write contract test for Monobank `GET /personal/statement/{account}/{from}/{to}` response mapping (amount÷100, currency numeric→alphabetic, Unix timestamp→DateTime UTC) using mocked HTTP handler in `backend/tests/FinanceSentry.Tests/Monobank/MonobankStatementContractTests.cs`

### Implementation for User Story 2

- [X] T024 [P] [US2] Add `MonobankTransaction` record to `MonobankAdapterModels.cs` with all statement entry fields in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Monobank/MonobankAdapterModels.cs`
- [X] T025 [US2] Implement `MonobankAdapter.SyncTransactionsAsync`: fetch statement window `[since, now]`; for initial sync (since=null) paginate three 31-day windows covering 90 days; map `MonobankTransaction` → `TransactionCandidate`; return candidates + next sync timestamp in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Monobank/MonobankAdapter.cs`
- [X] T026 [US2] Trigger initial 90-day import after connect: in `ConnectMonobankAccountCommand` handler, enqueue a Hangfire sync job for each created `BankAccount` in `backend/src/FinanceSentry.Modules.BankSync/Application/Commands/ConnectMonobankAccountCommand.cs`
- [X] T027 [US2] Update `TransactionSyncCoordinator` to resolve the correct `IBankProvider` via `IBankProviderFactory` based on `BankAccount.Provider` instead of directly injecting `IPlaidAdapter` in `backend/src/FinanceSentry.Modules.BankSync/Application/Services/ScheduledSyncService.cs`
- [X] T028 [US2] Add `provider` field to the accounts list API response DTO and map it from `BankAccount.Provider` in `backend/src/FinanceSentry.Modules.BankSync/Application/Queries/GetAccountsQuery.cs`
- [X] T029 [US2] Add provider badge to frontend accounts list component (shows "Monobank" or "Plaid" label) in `frontend/src/app/modules/bank-sync/pages/accounts-list/accounts-list.component.html`

**Checkpoint**: Monobank transactions are visible in the transaction list; no duplicates on re-import; Plaid transactions unaffected.

---

## Phase 5: User Story 3 — Sync On Demand and Automatically (Priority: P3)

**Goal**: Manual sync and scheduled sync both work for Monobank accounts. Rate limits respected. Failed syncs due to invalid tokens surface as error status.

**Independent Test**: Trigger manual sync on a connected Monobank account; verify new transactions appear and `MonobankCredential.LastSyncAt` advances. Observe a rate-limit retry in logs. Revoke token and trigger sync; verify account status becomes `"failed"` with `LastSyncError = "MONOBANK_TOKEN_INVALID"`.

### Implementation for User Story 3

- [X] T030 [US3] Add retry-with-backoff for HTTP 429 in `MonobankHttpClient` (3 attempts: immediate, +60s, +120s; throws `MonobankException` on third failure) in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Monobank/MonobankHttpClient.cs`
- [X] T031 [US3] Update `MonobankCredential.LastSyncAt` after each successful sync in `ScheduledSyncService.SyncMonobankAsync` in `backend/src/FinanceSentry.Modules.BankSync/Application/Services/ScheduledSyncService.cs`
- [X] T032 [US3] Update `ScheduledSyncJob` to iterate all active `BankAccount` rows (including provider=monobank) and resolve `IBankProvider` per account via factory — handled by `ScheduledSyncService` provider branching
- [X] T033 [US3] Map Monobank 401 response to `MarkReauthRequired()` / `MarkFailed("MONOBANK_TOKEN_INVALID")` in `ScheduledSyncService` error handling in `backend/src/FinanceSentry.Modules.BankSync/Application/Services/ScheduledSyncService.cs`

**Checkpoint**: Manual and scheduled sync both work for Monobank accounts. Token errors surface as visible account error state.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T034 [P] Write unit tests for `MonobankAdapter` (connect happy path, invalid token, duplicate connect, SyncTransactions amount mapping) in `backend/tests/FinanceSentry.Tests/Monobank/MonobankAdapterTests.cs`
- [ ] T035 [P] Write unit test for `BankProviderFactory` (resolves plaid for "plaid", monobank for "monobank", throws for unknown) in `backend/tests/FinanceSentry.Tests/Monobank/BankProviderFactoryTests.cs`
- [X] T036 Bump backend minor version in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj` (no `<Version>` tag present — N/A)
- [X] T037 Bump frontend minor version in `frontend/package.json` → 0.6.0

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 2)**: No dependencies — start immediately. BLOCKS all user stories.
- **US1 (Phase 3)**: Depends on Phase 2 completion.
- **US2 (Phase 4)**: Depends on Phase 3 (T017 — `MonobankAdapter` file exists to extend; T026 — command exists to modify).
- **US3 (Phase 5)**: Depends on Phase 4 (T027 — `TransactionSyncCoordinator` updated; T025 — `SyncTransactionsAsync` implemented).
- **Polish (Phase 6)**: Depends on Phase 5.

### Within Phase 3

- T011, T012, T013, T014, T015 are fully parallel (different files, no dependencies)
- T016 depends on T013, T014, T015 (`MonobankHttpClient` needs models + exception)
- T017 depends on T016 (`MonobankAdapter` needs `MonobankHttpClient`)
- T018 depends on T015, T017 (`ConnectMonobankAccountCommand` needs adapter interface + impl)
- T019 depends on T018 (endpoint needs command)
- T020 depends on T019 (DI registration needs all concrete types)
- T021 depends on T020 (frontend service calls the registered endpoint)
- T022 depends on T021 (component uses service)

### Parallel Opportunities

```
# Phase 2 — these can run in parallel:
T001 [IBankProvider interfaces]
T002 [MonobankCredential entity]
T003 [BankAccount modifications]
T004 [IMonobankCredentialRepository]

# Phase 2 — sequential after above:
T005 → T006 → T007 → T008 → T009 → T010

# Phase 3 — contract tests + infrastructure parallel:
T011, T012, T013, T014, T015 (all parallel)
→ T016 → T017 → T018 → T019 → T020 → T021 → T022 (sequential)

# Phase 4 — parallel start:
T023, T024 (parallel)
→ T025 → T026 → T027 → T028 → T029 (sequential)

# Phase 6 — fully parallel:
T034, T035, T036, T037
```

---

## Implementation Strategy

### MVP (User Story 1 Only)

1. Complete Phase 2: Foundational
2. Complete Phase 3: US1 (connect + accounts visible)
3. **STOP and VALIDATE**: Token → accounts appear with balance. No transactions yet (acceptable MVP).
4. Demo/validate before proceeding to US2.

### Incremental Delivery

1. Phase 2 → Foundation ready
2. Phase 3 → Connect Monobank → MVP (accounts visible)
3. Phase 4 → Transactions visible → meaningful data
4. Phase 5 → Ongoing sync → production-ready
5. Phase 6 → Tests + versioning → shippable

---

## Notes

- `ExternalAccountId` replaces `PlaidItemId` — all references in application code must be updated (grep for `PlaidItemId` before closing Phase 2)
- Monobank amounts: `int64` kopecks → divide by 100 to get `decimal`; conversion MUST happen in `MonobankHttpClient`, never in domain
- Currency: ISO 4217 numeric (`int`) → alphabetic (`string`); lookup table in `MonobankHttpClient`
- 90-day import: 3 × 31-day windows; 60-second delay between same-account calls (enforced by rate-limit retry, not `Thread.Sleep`)
- `BankAccount.PlaidItemId` unique index becomes `ExternalAccountId` unique index — no data loss, just rename
- Frontend ESLint gate: run `npx eslint` on every modified `.ts` file before committing
