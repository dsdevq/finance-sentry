# Feature Specification: Binance Integration

**Feature Branch**: `009-binance-integration`  
**Created**: 2026-04-21  
**Status**: Draft  
**Input**: User description: "integrate binance"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Connect Binance Account (Priority: P1)

A user links their Binance account to Finance Sentry by entering a Binance API key and secret. Once connected, the system fetches their spot wallet balances and stores them. The user immediately sees their crypto holdings (each asset's quantity and current USD value) alongside their bank accounts in the aggregated view.

**Why this priority**: Connecting and displaying balances is the minimum viable integration — no other Binance feature works until credentials are stored and the first sync runs.

**Independent Test**: Can be fully tested by submitting valid Binance API credentials and verifying that held asset balances appear in the user's portfolio summary.

**Acceptance Scenarios**:

1. **Given** a user with no Binance account linked, **When** they submit a valid API key and secret, **Then** the system stores the credentials securely, performs an initial balance sync, and returns the list of non-zero asset holdings.
2. **Given** a user submits an invalid or expired API key, **When** the system attempts to verify credentials, **Then** the connection is rejected and the user receives a clear error explaining the credential is invalid.
3. **Given** a user already has a Binance account linked, **When** they attempt to register again with new credentials, **Then** the system returns an error indicating a Binance account is already connected.

---

### User Story 2 - View Crypto Holdings & Portfolio Value (Priority: P2)

After connecting, the user can view their Binance spot wallet: each held asset (e.g., BTC, ETH, USDT), its quantity, and its current market value in USD. The data reflects the most recently synced state and shows when the last sync occurred.

**Why this priority**: Displaying holdings is the core value delivered to the user — raw stored balances with no display are not useful.

**Independent Test**: Can be tested by reading the holdings endpoint after a successful sync and verifying balances match what Binance reports for the test account.

**Acceptance Scenarios**:

1. **Given** a connected Binance account with a recent sync, **When** the user requests their crypto holdings, **Then** the response lists each non-zero asset (above dust threshold) with its quantity and USD value, plus the sync timestamp.
2. **Given** a connected Binance account where all asset balances are zero, **When** the user requests holdings, **Then** the system returns an empty holdings list (not an error).
3. **Given** Binance API is temporarily unreachable during a sync, **When** a sync is attempted, **Then** the system retains the last known balances and records the failure with a timestamp; stale data is still returned with an indicator of the last successful sync time.

---

### User Story 3 - Automatic Periodic Sync (Priority: P3)

The system automatically re-syncs Binance balances on a regular schedule without user intervention, ensuring the portfolio view stays reasonably current.

**Why this priority**: Manual-only sync would leave the data stale; automated background sync is essential for accurate portfolio tracking over time.

**Independent Test**: Can be tested by verifying that balance data timestamps are updated automatically at the configured sync interval without any user-initiated action.

**Acceptance Scenarios**:

1. **Given** a connected Binance account, **When** the scheduled sync interval elapses, **Then** the system re-fetches balances from Binance and updates stored values.
2. **Given** the scheduled sync fails due to a transient Binance API error, **When** the sync runs again at the next interval, **Then** the system retries and updates balances if Binance is available.
3. **Given** a user disconnects their Binance account, **When** the next scheduled sync runs, **Then** the system skips the sync for that account and does not attempt to use revoked credentials.

---

### User Story 4 - Disconnect Binance Account (Priority: P4)

A user can revoke their Binance integration, removing stored credentials and associated balance data from Finance Sentry.

**Why this priority**: Credential lifecycle management is a security baseline — users must be able to remove access without contacting support.

**Independent Test**: Can be tested by disconnecting a linked account and verifying that credentials are purged and no further sync jobs are scheduled.

**Acceptance Scenarios**:

1. **Given** a connected Binance account, **When** the user requests disconnection, **Then** the system deletes stored credentials and clears cached balances for that account.
2. **Given** a sync job is in progress when disconnection is requested, **When** the disconnect completes, **Then** no further sync jobs are scheduled and in-flight results are discarded.

---

### Edge Cases

- What happens when Binance API rate limits are hit during sync? System must back off and retry on the next scheduled cycle without flooding.
- How does the system handle assets that exist on Binance but have no known USD market price? Those assets should be listed with quantity but zero or null USD value.
- What happens if the user's Binance API key permissions are insufficient (e.g., account info not enabled)? Connection should be rejected with a descriptive error.
- How does the system behave when Binance reports a large number of near-zero dust balances? Balances below the configured dust threshold are excluded from the holdings list.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow a user to register a Binance integration by providing an API key and secret.
- **FR-002**: System MUST validate Binance credentials at registration time by performing a test call to Binance before accepting them.
- **FR-003**: System MUST store Binance API credentials encrypted at rest; secrets MUST never be stored or logged in plain text.
- **FR-004**: System MUST fetch spot wallet balances for all non-dust assets upon initial connection and on each scheduled sync.
- **FR-005**: System MUST expose an endpoint that returns the user's current Binance holdings (asset symbol, quantity, USD value, last sync timestamp).
- **FR-006**: System MUST schedule automatic balance syncs at a configurable interval (default: every 15 minutes).
- **FR-007**: System MUST handle Binance API failures gracefully — preserve last known balances, record the failure with timestamp, and retry on the next scheduled cycle.
- **FR-008**: System MUST allow the user to disconnect their Binance account, removing credentials and halting all future sync jobs for that account.
- **FR-009**: System MUST enforce user data isolation — one user's Binance credentials and balances MUST never be accessible to another user.
- **FR-010**: System MUST expose the Binance adapter behind a domain-defined interface, consistent with the existing adapter pattern used for other financial integrations.
- **FR-011**: System MUST filter out dust balances below a configurable threshold from the holdings view to reduce noise.
- **FR-012**: System MUST limit each user to one connected Binance account.

### Key Entities

- **BinanceCredentials**: Stores the API key and encrypted secret scoped to a user; includes active/revoked status and registration timestamp.
- **CryptoHolding**: Represents a snapshot of a user's holding in a single crypto asset — asset symbol, quantity, approximate USD value, and the sync timestamp.
- **CryptoSyncJob**: Tracks the status and outcome of each sync attempt — account reference, start time, end time, success/failure indicator, and error detail if failed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user with valid Binance credentials can see their crypto holdings within 30 seconds of completing the connection flow.
- **SC-002**: Automated syncs complete and update stored balances within 60 seconds of the scheduled trigger, under normal Binance API availability.
- **SC-003**: 100% of stored Binance API secrets are encrypted at rest — no plain-text secrets exist in the database at any point.
- **SC-004**: The system retries failed syncs automatically; no manual intervention is required to resume syncing after a transient Binance API outage.
- **SC-005**: Disconnecting a Binance account removes all associated credentials and stops future syncs within 5 seconds of the request.
- **SC-006**: The holdings endpoint returns data in under 500ms for users with up to 50 distinct assets.

## Assumptions

- ~~Only Binance Spot wallet balances are in scope; Futures, Margin, Earn, and Staking accounts are excluded from the initial integration.~~ **Updated 2026-04-26**: Spot **+ Funding wallet + Simple Earn (flexible & locked)** are aggregated per asset. Futures (USD-M, Coin-M), Cross/Isolated Margin, and Options remain out of scope. See `research.md` Decision 10.
- The user creates a read-only API key on Binance (no trading permissions required); the system enforces read-only access and never executes trades.
- USD is the single reference currency for value display; multi-currency display is deferred to a future feature.
- Asset USD prices are fetched from Binance's own ticker endpoint at sync time; a dedicated pricing service is out of scope.
- The user is authenticated with Finance Sentry before accessing any Binance-related endpoints (existing auth flow covers this).
- A single user may link at most one Binance account in this version.
- The sync interval (default: 15 minutes) is configured at the application level, not adjustable per user.
- This is a backend-only feature; no new frontend pages are introduced (existing wealth aggregation view will surface the data).

## Notes

- [DECISION 2026-04-26] Wallet-aggregation scope expansion: original Spot-only scope produced incomplete portfolios for users with funds in Earn — the most common case. Adapter now fans out to Spot + Funding + Simple Earn (flexible & locked) and aggregates per asset. Futures/Margin/Options still deferred. Earn endpoints require the API key's "Read" permission to include Earn data; missing permission is logged as a warning per source and the sync continues with whatever sources succeeded.
- [DECISION] Polling-only sync: Binance does not provide push webhooks for balance changes; scheduled polling is the only viable sync mechanism.
- [DECISION] Adapter interface: The Binance integration MUST implement a domain-defined `ICryptoExchangeAdapter` (or equivalent) interface per Constitution Principle I, consistent with the `IBankProvider` pattern used for bank integrations.
- [OUT OF SCOPE] Trade history: Fetching and storing order/trade history is excluded from this feature to keep scope manageable; can be added in a follow-up.
- [OUT OF SCOPE] Price history and charting: Historical USD value of holdings over time is out of scope.
- [OUT OF SCOPE] Multi-exchange: Only Binance is targeted; a generic multi-exchange framework is not built here.
