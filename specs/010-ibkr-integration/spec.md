# Feature Specification: Interactive Brokers Account Integration

**Feature Branch**: `010-ibkr-integration`
**Created**: 2026-04-22
**Status**: Draft
**Input**: User description: "make ibkr integration"

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Connect IBKR Brokerage Account (Priority: P1)

A user who holds investments in an Interactive Brokers account can link that account to Finance Sentry by providing their IBKR credentials. The system validates the credentials with Interactive Brokers, fetches the current portfolio snapshot, and stores it securely. After a successful connection, the user's IBKR holdings are visible within Finance Sentry.

**Why this priority**: Connecting the account is the prerequisite for all other value. Without a live link there is nothing to display or sync. This is the core MVP of the feature.

**Independent Test**: Can be fully tested by providing valid IBKR credentials → the endpoint returns the number of positions imported and a `connectedAt` timestamp. Delivers immediate value: the user can see their brokerage portfolio in Finance Sentry.

**Acceptance Scenarios**:

1. **Given** an authenticated Finance Sentry user with a valid IBKR account, **When** they submit their IBKR credentials, **Then** the system validates the credentials with Interactive Brokers, stores them securely, imports the current portfolio positions, and returns a 201 response with holdings count and connection time.
2. **Given** an authenticated Finance Sentry user, **When** they submit invalid or expired IBKR credentials, **Then** the system returns a 422 response with error code `INVALID_CREDENTIALS`.
3. **Given** a Finance Sentry user who has already linked an IBKR account, **When** they attempt to connect again, **Then** the system returns a 409 response with error code `ALREADY_CONNECTED`.
4. **Given** a request with missing or empty credential fields, **When** the endpoint receives it, **Then** the system returns a 400 response with error code `VALIDATION_ERROR`.
5. **Given** an unauthenticated request, **When** the endpoint receives it, **Then** the system returns a 401 response.

---

### User Story 2 — View Brokerage Holdings & Total Value (Priority: P2)

After connecting, the user can query their current IBKR portfolio: a list of positions (stocks, ETFs, bonds, and other instruments) each with quantity, market price, and USD value, plus the portfolio's aggregate USD value. This data also appears in the overall wealth summary under a dedicated `"brokerage"` category.

**Why this priority**: Viewing holdings is the primary ongoing value of the integration. Once connected, this is what users return to repeatedly.

**Independent Test**: After a successful connect, `GET /api/v1/brokerage/holdings` returns the positions list with at least one entry and a non-zero `totalUsdValue`. The wealth summary endpoint includes a `"brokerage"` category with a matching total.

**Acceptance Scenarios**:

1. **Given** a connected IBKR account with positions, **When** the user requests their holdings, **Then** the system returns a 200 response with provider `"ibkr"`, a list of positions (each with symbol, instrument type, quantity, USD value), a `totalUsdValue`, `syncedAt` timestamp, and an `isStale` flag.
2. **Given** a Finance Sentry user with no IBKR account connected, **When** the user requests holdings, **Then** the system returns a 200 response with an empty positions list and zero total.
3. **Given** holdings that were last synced more than 1 hour ago, **When** the user requests holdings, **Then** the `isStale` flag is `true`.
4. **Given** a connected IBKR account, **When** the wealth summary endpoint is called, **Then** the response includes a `"brokerage"` category with the total USD value matching the sum of all IBKR positions.
5. **Given** an unauthenticated request, **When** the holdings endpoint receives it, **Then** the system returns a 401 response.

---

### User Story 3 — Automatic Periodic Holdings Sync (Priority: P3)

The system automatically re-fetches IBKR portfolio positions on a regular schedule for all connected users, keeping the displayed data fresh without requiring manual action.

**Why this priority**: Manual sync is acceptable at launch but stale data erodes trust over time. Automatic sync is the expected behavior for a financial aggregator.

**Independent Test**: Triggering the sync job manually updates `syncedAt` timestamps on all holdings for all users with active IBKR connections; any previous errors are cleared on success.

**Acceptance Scenarios**:

1. **Given** multiple Finance Sentry users each with active IBKR connections, **When** the sync job runs, **Then** each user's holdings are refreshed and `syncedAt` is updated for all.
2. **Given** one user's IBKR credentials have expired while another user's are valid, **When** the sync job runs, **Then** the valid user's holdings are updated successfully and the failed user's sync error is recorded without stopping the job for others.

---

### User Story 4 — Disconnect IBKR Account (Priority: P4)

The user can remove their IBKR integration from Finance Sentry. On disconnect, stored credentials and all cached position data are deleted, and future syncs stop for that user.

**Why this priority**: Disconnect is a trust and control requirement. Users must be able to revoke access; without it the feature cannot be considered complete.

**Independent Test**: After connecting, calling `DELETE /api/v1/brokerage/ibkr/disconnect` returns 204; a subsequent holdings query returns an empty list; a subsequent connect call succeeds.

**Acceptance Scenarios**:

1. **Given** a user with an active IBKR connection, **When** they call the disconnect endpoint, **Then** the system returns 204, removes their credentials and cached positions, and stops syncing.
2. **Given** a user with no IBKR connection, **When** they call the disconnect endpoint, **Then** the system returns 404 with error code `NOT_FOUND`.
3. **Given** an unauthenticated request, **When** the disconnect endpoint receives it, **Then** the system returns a 401 response.

---

### Edge Cases

- What happens when IBKR returns positions in a currency other than USD? All USD values are taken from the position's reported USD market value as provided by the IBKR API; non-USD base-currency accounts are normalized by IBKR before we read them.
- What happens when a position has a zero or unknown price (e.g., illiquid instruments, options expiring worthless)? Those positions are included with a `usdValue` of 0 rather than excluded.
- What happens when the IBKR account has sub-accounts (e.g., an advisor account managing multiple clients)? Only the primary account's positions are imported; sub-account aggregation is out of scope for this feature.
- What happens when sync fails for all credentials during a background run? Each failure is logged individually; the batch continues; no silent dropping of errors.
- What happens when credentials expire between syncs? The next sync attempt records the error and marks the credential as failing; the user sees a stale flag on the holdings view.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow an authenticated Finance Sentry user to connect their IBKR account by providing valid IBKR credentials.
- **FR-002**: System MUST validate the provided credentials against Interactive Brokers before accepting them.
- **FR-003**: System MUST securely store IBKR credentials encrypted at rest; plaintext credentials must never be persisted or appear in logs or responses.
- **FR-004**: System MUST reject a connect request when the user already has an active IBKR connection (409 Conflict, error code `ALREADY_CONNECTED`).
- **FR-005**: System MUST return a meaningful error when credentials are invalid (422 Unprocessable Entity, error code `INVALID_CREDENTIALS`).
- **FR-006**: System MUST fetch the current portfolio positions immediately after a successful connection and return the count and connection timestamp in the 201 response.
- **FR-007**: System MUST allow an authenticated user to retrieve their current IBKR holdings as a list of positions with symbol, instrument type, quantity, and USD value.
- **FR-008**: System MUST compute an aggregate `totalUsdValue` across all IBKR positions and include it in the holdings response.
- **FR-009**: System MUST expose an `isStale` flag in the holdings response indicating whether the data is older than 1 hour.
- **FR-010**: System MUST include IBKR holdings as a `"brokerage"` category in the wealth summary endpoint alongside bank and crypto data.
- **FR-011**: System MUST automatically re-sync all active IBKR connections on a scheduled interval.
- **FR-012**: System MUST continue syncing other users' accounts when one user's sync fails; each failure is logged per-credential without aborting the batch.
- **FR-013**: System MUST allow an authenticated user to disconnect their IBKR account, permanently deleting stored credentials and all cached position data.
- **FR-014**: System MUST return 404 when a disconnect is requested for an account that is not connected (error code `NOT_FOUND`).
- **FR-015**: System MUST enforce authentication on all endpoints; unauthenticated requests receive 401.

### Key Entities

- **IBKRCredential**: Represents a single user's encrypted IBKR authentication material. Key attributes: user identity, encrypted credentials, key version, active flag, last sync timestamp, last sync error, connection timestamp.
- **BrokerageHolding**: A single position in a user's IBKR portfolio. Key attributes: user identity, instrument symbol, instrument type (stock, ETF, bond, option, fund, etc.), quantity, USD value, last synced timestamp, provider (`"ibkr"`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete the IBKR account connection flow in under 30 seconds from submitting credentials to receiving the holdings count.
- **SC-002**: After connection, holdings data is available within 60 seconds for portfolios of up to 500 positions.
- **SC-003**: The wealth summary correctly reflects IBKR total value within 1% of the sum of individual position values (rounding tolerance only).
- **SC-004**: Automatic sync keeps holdings data no more than 20 minutes stale under normal operation (assuming a 15-minute sync interval).
- **SC-005**: A sync failure for one user does not prevent other users' holdings from being updated; 100% of non-failing accounts complete successfully in the same run.
- **SC-006**: After disconnect, no trace of the user's IBKR credentials or position data is retrievable via any Finance Sentry endpoint.

## Assumptions

- Users already have an active Interactive Brokers brokerage account.
- IBKR credential input follows a key/token pattern (similar to Binance); the exact credential format and the IBKR API to use (Client Portal Gateway, Web API, or TWS API) will be resolved during the planning phase.
- Positions are stored and surfaced in USD; non-USD position values are converted using the USD equivalent reported by the IBKR API at sync time.
- Sub-account aggregation (advisor or family-office accounts) is out of scope; only the primary linked account is imported.
- The feature covers read-only access; no trading, order placement, or account modification is within scope.
- No Angular frontend UI for this feature; the backend API is the deliverable, consistent with the Binance integration.
- Instrument types with complex payoff structures (options, futures, structured products) are stored with the USD value reported by IBKR without any additional analytics.

## Notes

- [DECISION] Scope boundary: This feature delivers a backend-only integration following the same module pattern as feature 009 (Binance). No Angular UI is included.
- [DECISION] Wealth summary integration: IBKR holdings will be surfaced in `GET /api/v1/wealth/summary` under a `"brokerage"` category via the cross-module reader contract pattern established in feature 009.
- [DECISION] Credential security: IBKR credentials are encrypted using the existing `ICredentialEncryptionService` (AES-256-GCM), identical to Binance and Plaid integrations.
- [DECISION] IBKR API choice: Deferred to the planning phase. The spec is intentionally API-agnostic; the plan will select the most appropriate IBKR API (Client Portal Gateway REST, Web API OAuth, or TWS API) based on suitability for a self-hosted personal finance tool.
- [OUT OF SCOPE] Trading / order management: Finance Sentry is read-only.
- [OUT OF SCOPE] Sub-account aggregation: Only the primary account linked by the user is synced.
- [OUT OF SCOPE] Frontend UI: Angular screens for IBKR connect/disconnect are deferred to a future feature.
- [OUT OF SCOPE] Options/futures Greeks: Position USD values are stored as-reported; no derivative analytics.
