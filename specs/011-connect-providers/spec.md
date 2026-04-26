# Feature Specification: Connect Bank, Brokerage, and Crypto Providers

**Feature Branch**: `011-connect-providers`
**Created**: 2026-04-25
**Status**: Draft
**Input**: User description: "I want you to implement connect feature of banks(plaid, monobank, etc), ibkr and binance. Design has been implemented by stitch"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Connect a US bank via Plaid Link (Priority: P1)

A signed-in user opens the connect-account flow, picks **Plaid** as the provider, completes Plaid's hosted authentication overlay (selecting their bank, signing in, granting transaction-history consent), and lands back in the app with the new account visible in the accounts list and the initial 12-month transaction sync running in the background.

**Why this priority**: Plaid is the largest user base (US banks), the existing backend (`POST /accounts/connect`, `POST /accounts/link`, `BankAccountConnectedEvent`) already supports it end-to-end, and it is the canonical "happy path" — landing it first proves the unified connect UI works against a real third-party flow.

**Independent Test**: Can be fully tested by signing in, opening the connect flow, choosing Plaid, completing a Plaid sandbox login, and verifying that (a) the connected account shows up in the accounts list with `syncStatus = 'syncing'`, (b) within ~30 s it transitions to `active` with a non-zero transaction count, (c) closing/cancelling Plaid mid-flow returns the user to the provider picker without creating any account.

**Acceptance Scenarios**:

1. **Given** a signed-in user on the connect-account screen, **When** they pick "Plaid" and complete the Plaid Link sandbox flow with a supported test institution, **Then** the new account appears in the accounts list with the institution name and last-4, the screen shows a success state, and the user can navigate to the dashboard with the new account included in totals.
2. **Given** a signed-in user mid-Plaid-flow, **When** they cancel or close the Plaid overlay, **Then** the connect screen returns to the provider picker with no account created and no error toast (cancellation is not an error).
3. **Given** a signed-in user who already has a Plaid account connected, **When** they try to connect the same bank again, **Then** they see a clear "Already connected" message and remain on the connect screen without a duplicate account being created.

---

### User Story 2 - Connect Monobank with a personal API token (Priority: P1)

A signed-in user opens the connect-account flow, picks **Monobank**, pastes their personal API token into a single text field, submits, and lands back in the accounts list with one row per Monobank card/account they own — each row showing the masked PAN, account type, currency, and current balance.

**Why this priority**: Monobank is the largest non-US user base for this app and the fastest-onboarding flow because there is no third-party overlay to maintain — just one form field. The backend (`POST /accounts/monobank/connect`, returning `ConnectMonobankResult` with the account list) already exists. Shipping it alongside Plaid validates the connect-flow's "non-Plaid" branch.

**Independent Test**: Can be fully tested by signing in, opening the connect flow, choosing Monobank, pasting a valid token, and verifying that one Monobank account per linked card appears in the accounts list with the right currency and balance. Pasting an invalid token shows an inline "Invalid Monobank token" error without leaving the form.

**Acceptance Scenarios**:

1. **Given** a signed-in user on the connect screen, **When** they pick "Monobank", paste a valid token, and submit, **Then** all of their Monobank cards appear in the accounts list and the connect screen shows a success state with the count of accounts added.
2. **Given** a signed-in user with an invalid token, **When** they submit, **Then** they see an inline form error "Invalid Monobank token" and the form remains editable so they can correct and retry without leaving the screen.
3. **Given** a signed-in user who already connected a Monobank token, **When** they try to connect a second token under the same user, **Then** they see "Monobank account already connected" and the form is blocked from submitting until they disconnect the existing one.

---

### User Story 3 - Connect Binance with read-only API key + secret (Priority: P2)

A signed-in user opens the connect-account flow, picks **Binance**, pastes the API key and API secret generated from Binance with read-only scope, submits, and sees their crypto holdings appear under a new "Crypto — Binance" section with each asset's quantity and USD value.

**Why this priority**: Binance covers the crypto-holdings use case, the backend (`POST /api/v1/crypto/binance/connect`) is implemented, and the form is two text fields plus a clear "use a read-only key" hint — same UI shape as Monobank with a second field. P2 because the user base is smaller than banking and the data shape (holdings, no transactions) overlaps less with the existing accounts list.

**Independent Test**: Can be fully tested by signing in, opening the connect flow, choosing Binance, entering valid Binance testnet API credentials, and verifying that the holdings page shows each non-dust balance with a USD value computed from the latest spot price.

**Acceptance Scenarios**:

1. **Given** a signed-in user with valid Binance read-only credentials, **When** they submit the form, **Then** they land on the Binance holdings view showing all non-dust balances with USD value totals.
2. **Given** a signed-in user, **When** they paste credentials that Binance rejects (wrong key, IP-restricted, write-scope-only), **Then** they see "Binance rejected the provided credentials" and the form remains editable.
3. **Given** a signed-in user who already has a Binance account connected, **When** they try to connect again, **Then** they see "Binance account already connected" and are offered a "Disconnect existing" action that removes the prior connection so they can reconnect with a new key.

---

### User Story 4 - Connect Interactive Brokers (IBKR) via gateway credentials (Priority: P2)

A signed-in user opens the connect-account flow, picks **Interactive Brokers**, enters their IBKR username and password, submits, and sees their brokerage positions (stocks, ETFs, options) listed under a new "Brokerage — IBKR" section with quantity, instrument type, and USD value per position.

**Why this priority**: IBKR completes the "all asset classes in one app" story, the backend (`POST /api/v1/brokerage/ibkr/connect`) is implemented, and the form mirrors Binance (username + password). P2 because IBKR has a smaller user base than banks/crypto and the gateway dependency makes it the most fragile integration to demo.

**Independent Test**: Can be fully tested by signing in, opening the connect flow, choosing IBKR, entering valid paper-trading credentials, and verifying that the brokerage holdings view shows the test account's positions with non-zero USD values.

**Acceptance Scenarios**:

1. **Given** a signed-in user with valid IBKR paper-trading credentials, **When** they submit the form, **Then** they land on the IBKR holdings view with their open positions listed.
2. **Given** a signed-in user, **When** the IBKR gateway rejects their credentials, **Then** they see "IB Gateway rejected the provided credentials" with a hint that the gateway requires the user to confirm a 2FA push notification.
3. **Given** a signed-in user who already has an IBKR account connected, **When** they try to connect again, **Then** they see "IBKR account already connected" and are offered a "Disconnect existing" action.

---

### User Story 5 - Disconnect any connected provider (Priority: P3)

A signed-in user opens any connected provider's detail view (or the accounts list for banking), selects "Disconnect", confirms, and the provider's data (credentials + cached holdings/accounts) is removed from their account so they can re-connect with a fresh credential later.

**Why this priority**: Disconnect is a hygiene feature — users need it eventually (e.g., after rotating an API key) but the connect flow ships without it because the existing accounts can be cleaned up via direct API calls in the meantime. P3 because the absence of a disconnect button doesn't block a user from using the connect feature.

**Independent Test**: Connect any provider, click "Disconnect" on its detail view, confirm in the dialog, and verify that (a) the provider no longer appears in the accounts/holdings list, (b) attempting to reconnect with a new credential succeeds without an "already connected" error.

**Acceptance Scenarios**:

1. **Given** a signed-in user with a connected Binance account, **When** they click "Disconnect" and confirm, **Then** the Binance section disappears from the holdings view and the provider picker on the connect screen shows Binance as available again.
2. **Given** a signed-in user, **When** they cancel the disconnect confirmation dialog, **Then** the provider remains connected and no data is removed.

---

### Edge Cases

- **Network failure mid-connect**: User submits credentials, network drops, no server response received. UI must show a transient retry banner ("Connection lost — please retry") without losing the form input or marking the account connected.
- **Plaid Link script blocked or fails to load**: User picks Plaid, the Plaid SDK fails to load (ad blocker, CSP, offline). UI shows "Plaid is unavailable. Please disable any ad blocker and refresh." with a retry button — does not silently hang.
- **Backend returns 422 for non-credential reason**: e.g., Binance returns 422 because the user's IP is not in the API key's allow-list. UI shows the actual error message from the backend (not a generic "Invalid credentials"), so the user can fix the underlying problem.
- **Provider already connected by a different user**: For Plaid only — the same Plaid item could theoretically belong to multiple users in the system. Backend dedupes by external account id; frontend shows "This account is already connected to another user" with no further action.
- **Sync still running when user navigates away**: User connects Plaid, the initial 12-month sync is running, user navigates to dashboard. Dashboard shows the new account with `syncStatus = 'syncing'` and a spinner; refreshes when the sync completes (existing `BankAccountConnectedEvent` flow).
- **User cancels the credential form**: User opens any provider form, types partial credentials, clicks "Cancel" or browser back. UI returns to provider picker, form input is discarded, no API call is made.
- **Token paste with surrounding whitespace**: Common when copying from a confirmation email. Form auto-trims input before submitting.
- **Paste of a wrong-format token**: e.g., user pastes a Binance API key into the Monobank field. Form rejects on submit with "This doesn't look like a Monobank token" rather than a server round-trip.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to start the connect flow from a single, discoverable entry point in the app (a "Connect account" button on the accounts list and on an empty-state placeholder when the user has zero accounts).
- **FR-002**: The connect flow MUST present a provider picker listing all four supported providers (Plaid, Monobank, Binance, IBKR) with each provider's name, logo/icon, one-line description of what data is fetched, and a clear status badge when the provider is already connected ("Connected" / "Available").
- **FR-003**: Selecting Plaid MUST open the Plaid Link hosted overlay; the rest of the connect UI MUST be hidden behind the overlay until Plaid completes or is dismissed.
- **FR-004**: Selecting Monobank MUST present a single password-style input for the API token with a help link explaining how to generate one and a "Connect" submit button that is disabled until the field is non-empty.
- **FR-005**: Selecting Binance MUST present two password-style inputs (API key, API secret) with a help link explaining how to generate read-only keys and a "Connect" submit button that is disabled until both fields are non-empty.
- **FR-006**: Selecting IBKR MUST present a username field and a password-style input for the password with a hint that the user may need to confirm a 2FA push notification on their phone after submitting, and a "Connect" submit button disabled until both fields are non-empty.
- **FR-007**: For every provider except Plaid, on submit the form MUST show a loading state on the button, disable all inputs, and remain on the same screen until the backend responds.
- **FR-008**: On a successful connect, the UI MUST show a success confirmation that names the provider and the count of accounts/holdings added, and MUST automatically route the user to the relevant data view (banking → accounts list; crypto/brokerage → holdings list) within 2 seconds.
- **FR-009**: On a credential rejection (HTTP 422), the UI MUST show the error message returned by the backend inline next to the offending field (or as a form-level banner if the error is not field-specific) and keep the form editable.
- **FR-010**: On an "already connected" conflict (HTTP 409), the UI MUST show a clear message and offer a "Disconnect existing" action that, when clicked-and-confirmed, calls the backend disconnect endpoint and re-enables the form for a fresh credential.
- **FR-011**: On a validation error (HTTP 400 with `errorCode: VALIDATION_ERROR`), the UI MUST show the validation messages from the backend's `details` array next to the relevant fields.
- **FR-012**: On a network failure or 5xx, the UI MUST show a retry banner with a "Try again" button that re-submits the same payload; credential inputs MUST NOT be lost.
- **FR-013**: All credential inputs MUST be `type="password"` (or equivalent), MUST NOT autofill from the browser's password manager, and MUST NOT be persisted to client-side storage at any point in the flow.
- **FR-014**: The connect flow MUST be reachable only by signed-in users; an unauthenticated user landing on the route MUST be redirected to login.
- **FR-015**: The system MUST enforce one connected account per provider per user (the backend already enforces this via 409 conflict on duplicate connect; the UI MUST surface this as documented in FR-010).
- **FR-016**: Users MUST be able to disconnect any connected provider from a per-provider detail view, with a confirmation dialog that names the provider and warns that holdings/transaction data fetched from that provider will be removed.
- **FR-017**: The visual design MUST follow the Stitch-generated screens for the connect flow and per-provider forms, using the existing `@dsdevq-common/ui` component library wherever components match (inputs, buttons, alerts, cards) and adding new shared components only when no existing component fits.
- **FR-018**: The flow MUST be fully usable on mobile viewports (≥ 360px width) with no horizontal scroll, no input cut-off, and the Plaid overlay sized to fit the viewport.

### Key Entities *(include if feature involves data)*

- **Provider**: A connectable third-party data source. Identified by a slug (`plaid` | `monobank` | `binance` | `ibkr`), with display name, logo asset, one-line description, credential-form shape (URL-redirect for Plaid, single-token for Monobank, key-secret for Binance, user-pass for IBKR), and connection status for the current user (connected | available).
- **Connection Form Submission (transient)**: The credentials a user pastes/types during the flow. Lives only in the form's local state; transmitted once to the backend and discarded on success or error. Never logged, cached, or persisted client-side.
- **Connect Result (read-side)**: The backend response describing the just-connected accounts/holdings — used by the success screen to decide what to display and where to route. Already typed by the existing handler results (`ConnectBankAccountResult`, `ConnectMonobankResult`, `ConnectBinanceResult`, `ConnectIBKRResult`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new user with valid credentials can connect any one of the four supported providers in under 90 seconds from clicking the "Connect account" button to seeing their data on screen (including the initial sync for Plaid).
- **SC-002**: Across all four providers, ≥ 95% of submission attempts that succeed at the backend reach the success screen on the frontend (i.e., the UI does not lose successful connections to network glitches or unhandled responses).
- **SC-003**: Of users who reach the connect screen, ≥ 80% complete a connection in their first session (no abandonment due to confusion about provider choice or form fields).
- **SC-004**: Every documented error path (invalid credentials, already connected, validation error, 5xx, network drop, Plaid script load failure) renders a user-facing message that names the failure and offers a single clear next action — verified by manual QA against the design checklist.
- **SC-005**: Zero credential leaks — credentials never appear in browser console output, network panel logs (other than the one outbound POST), localStorage, sessionStorage, IndexedDB, or any cookie. Verified once via DevTools session before sign-off.
- **SC-006**: The connect flow renders correctly on viewports from 360 px to 1920 px wide (smallest mobile to widescreen desktop) with no horizontal scroll and no clipped content. Verified via responsive snapshot tests in Storybook.

## Assumptions

- The existing backend endpoints (`POST /accounts/connect`, `POST /accounts/link`, `POST /accounts/monobank/connect`, `POST /api/v1/crypto/binance/connect`, `POST /api/v1/brokerage/ibkr/connect`, plus the matching disconnect endpoints) are stable and complete; this feature does not change the backend API surface.
- Stitch has produced screens for the provider picker, each per-provider form, success state, error states, and a disconnect confirmation dialog. Implementation translates these into Angular components backed by `@dsdevq-common/ui` primitives.
- The Plaid Link client SDK is already loaded by the existing connect-account integration; this feature reuses that loader rather than introducing a second one.
- Existing `connect-account.component.ts`, `transaction-list.component.ts`, and `sync-status.component.ts` will be replaced or restructured around the new design; their state will move into the existing `ConnectStore` / `TransactionsStore` / `SyncStatusStore` already in place.
- The error-code-to-user-message map (`ERROR_MESSAGES_REGISTRY`) is the single source of localized copy; this feature adds entries for any new error codes the backend already returns but the registry doesn't yet cover.
- The four providers cover the v1 scope. Additional providers (Coinbase, Schwab, Revolut, etc.) are out of scope for this feature and will be added by extending the provider list once the abstraction is proven against these four.
- Mobile native apps are out of scope; this feature targets the web SPA only.

## Notes

- [Add decision records here as clarifications are resolved during /speckit.clarify or /speckit.plan]
