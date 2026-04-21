# Feature Specification: Monobank Bank Provider Adapter

**Feature Branch**: `007-monobank-adapter`  
**Created**: 2026-04-19  
**Status**: Draft  
**Input**: User description: "I want to create a monobank adapter, since it is not available through plaid"

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Connect Monobank Account (Priority: P1)

A user who banks with Monobank (Ukraine) wants to connect their account to Finance Sentry so their balance and transaction history appear alongside any other connected accounts. They obtain a personal API token from Monobank's self-service portal and enter it in Finance Sentry. The system validates the token, fetches their account details, and begins importing their transaction history.

**Why this priority**: This is the core value — without account connection there is nothing else to show. All other stories depend on it.

**Independent Test**: Can be tested end-to-end by entering a valid Monobank token, confirming accounts appear in the accounts list with correct names and balances, and verifying transactions are visible.

**Acceptance Scenarios**:

1. **Given** a user has a valid Monobank personal API token, **When** they submit it in the Connect Account flow, **Then** their Monobank accounts appear in the accounts list with the correct name, account type, and current balance.
2. **Given** a user submits an invalid or expired token, **When** the system attempts to validate it, **Then** a clear error message is shown and no account is created.
3. **Given** a user already connected this Monobank token, **When** they attempt to connect it again, **Then** the system rejects the duplicate and shows an informative message.

---

### User Story 2 — View Monobank Transactions (Priority: P2)

After connecting their Monobank account, the user can browse their transaction history in Finance Sentry. Transactions show the merchant name, amount, category, and date — consistent with how Plaid-sourced transactions are displayed.

**Why this priority**: Transactions are the primary data source for the wealth and spending analytics the application aims to provide. Balances alone are insufficient for the "WEALTH" dashboard vision.

**Independent Test**: Can be tested independently by navigating to the Monobank account's transaction list and verifying that imported transactions match the data visible in the Monobank mobile app.

**Acceptance Scenarios**:

1. **Given** a connected Monobank account with transaction history, **When** the user opens the transaction list for that account, **Then** transactions appear with correct amount, description, date, and category.
2. **Given** a transaction imported from Monobank in UAH, **When** displayed in the transaction list, **Then** the original UAH amount is shown with the correct currency code.
3. **Given** a transaction that was already imported, **When** a sync runs again, **Then** the same transaction is not duplicated.

---

### User Story 3 — Sync Monobank Data on Demand and Automatically (Priority: P3)

The user can trigger a manual sync for a connected Monobank account at any time, and the system also refreshes the account automatically on a schedule. New transactions and balance changes are picked up without user intervention.

**Why this priority**: Keeps data current without requiring the user to reconnect or manually manage their token. Less critical than initial connect/view but important for ongoing usability.

**Independent Test**: Can be tested by triggering a manual sync after making a real Monobank transaction and verifying it appears in Finance Sentry within the expected time.

**Acceptance Scenarios**:

1. **Given** a connected Monobank account, **When** the user triggers a manual sync, **Then** the latest transactions and balance are fetched and reflected within 30 seconds.
2. **Given** a connected Monobank account, **When** the scheduled sync runs, **Then** any new transactions since the last sync are imported without user action.
3. **Given** a sync fails due to a revoked or expired token, **When** the failure occurs, **Then** the account is marked with an error status and the user can see the problem without investigating manually.

---

### Edge Cases

- What happens when the Monobank API rate limit is hit (client info: once per 60 seconds; statements: throttled per account)?
- How does the system handle a token that was valid during connection but revoked later?
- What happens if Monobank returns a statement window with no transactions?
- How does the system behave when Monobank is temporarily unavailable?
- What if the user has multiple Monobank cards/accounts under the same token — are all imported?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow a user to connect a Monobank account by submitting a personal API token.
- **FR-002**: System MUST validate the token against Monobank before saving it, and reject invalid or expired tokens with a clear error.
- **FR-003**: System MUST fetch and store all accounts (cards) associated with the submitted token.
- **FR-004**: System MUST import transaction history for each Monobank account, covering at least the last 90 days on initial connect.
- **FR-005**: System MUST deduplicate transactions so re-running a sync never creates duplicate records.
- **FR-006**: System MUST display Monobank accounts and transactions in the same UI views as Plaid-sourced accounts, without separate screens.
- **FR-007**: System MUST store the Monobank token encrypted at rest, using the same encryption mechanism applied to Plaid credentials.
- **FR-008**: System MUST support manual sync triggered by the user for any connected Monobank account.
- **FR-009**: System MUST support scheduled automatic sync for Monobank accounts on the same cadence as Plaid accounts.
- **FR-010**: System MUST mark an account with an error status when a sync fails due to an invalid, expired, or revoked token.
- **FR-011**: System MUST respect Monobank API rate limits and not trigger avoidable rate-limit errors.

### Key Entities

- **MonobankCredential**: A stored, encrypted personal token associated with a user and one or more Monobank accounts. Analogous to a Plaid access token.
- **BankAccount** (existing): Extended to represent Monobank-sourced accounts, identified by provider type (`monobank`) and Monobank's internal account ID.
- **Transaction** (existing): Populated from Monobank statement entries using the same domain model as Plaid transactions.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can connect a Monobank account and see their balance and at least 90 days of transactions within 2 minutes of submitting their token.
- **SC-002**: Monobank transactions appear in the same transaction list as Plaid transactions — no separate screen required.
- **SC-003**: Re-running sync on a Monobank account with no new transactions produces zero duplicate records.
- **SC-004**: A sync triggered manually completes and reflects the updated state within 30 seconds under normal conditions.
- **SC-005**: A revoked token is detected within one sync cycle, and the account error state is visible to the user without requiring manual investigation.

## Assumptions

- The user has an active Monobank account and can obtain a personal API token from `api.monobank.ua` without needing corporate/business registration.
- Transaction history retrieval is limited to 31-day windows per Monobank API constraints; initial import will paginate across multiple windows to cover 90 days.
- All Monobank amounts are in UAH; currency display in the UI will show the original currency (no automatic conversion to a base currency in this feature — deferred to a future multi-currency analytics feature).
- The existing encrypted credential storage mechanism used for Plaid access tokens will be reused for Monobank tokens without changes to the encryption library.
- Monobank webhooks are not used in this feature; polling/scheduled sync is sufficient (webhooks are available via the corporate API only, which requires separate registration).
- A single Monobank token may expose multiple cards/accounts — all will be imported under the same connected credential.

## Notes

- [DECISION] Token storage: Monobank personal tokens are stored encrypted using the same AES-256-GCM mechanism as Plaid access tokens. No new encryption infrastructure is required.
- [DECISION] Sync cadence: Monobank accounts sync on the same schedule as Plaid accounts (existing Hangfire jobs). No separate scheduler is introduced.
- [DECISION] UI unification: Monobank accounts appear in the existing accounts list and transaction views. The provider is surfaced as metadata (e.g. a "Monobank" label) but no separate routing or screens are created.
- [DECISION] Provider abstraction: This feature introduces an `IBankProvider` interface that both `PlaidAdapter` and the new `MonobankAdapter` implement, enabling the connect flow to route to the correct provider.
- [OUT OF SCOPE] Currency conversion: UAH → EUR/USD conversion is out of scope. Deferred to a future wealth aggregation / multi-currency feature.
- [OUT OF SCOPE] Monobank corporate API / webhooks: Personal token API only. Corporate registration and webhook push are deferred.
- [OUT OF SCOPE] Other Ukrainian banks (e.g. PrivatBank): Only Monobank is in scope for this feature.
