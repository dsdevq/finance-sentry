# Feature Specification: Google Sign-In via Identity Services

**Feature Branch**: `004-adopt-oauth`
**Created**: 2026-04-15
**Revised**: 2026-04-18
**Status**: Draft
**Input**: Switch Google OAuth from server-side Authorization Code flow to Google Identity Services (GSI) client-side credential flow.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Google Sign-In / Sign-Up (Priority: P1)

A user clicks "Continue with Google" on the login or register page. A Google account picker appears (no full-page redirect). The user selects their account, and within moments they are authenticated and land on the accounts page — whether they are a returning user or a first-timer.

**Why this priority**: The core deliverable. One interaction covers both sign-in and sign-up, reducing friction to a single click for anyone with a Google account.

**Independent Test**: Click "Continue with Google" on the login page with a Google account that has never used Finance Sentry — a new account is created and the user lands on the accounts page. Repeat with the same account — the user is signed back into the existing account.

**Acceptance Scenarios**:

1. **Given** a user is on the login or register page, **When** they click "Continue with Google" and select their Google account, **Then** they are authenticated and redirected to the accounts page within 5 seconds.
2. **Given** a first-time user with no Finance Sentry account, **When** they complete Google sign-in, **Then** a new Finance Sentry account is created using their Google profile email and display name — no registration form required.
3. **Given** a returning user who previously signed in with Google, **When** they click "Continue with Google" and select the same account, **Then** they are signed back into their existing account with all data intact.
4. **Given** a user previously registered with email/password using the same Gmail address, **When** they sign in with Google, **Then** their existing account is matched by email and they see their existing data — no duplicate account is created.
5. **Given** a user dismisses the Google account picker without selecting an account, **When** they are returned to the login page, **Then** a non-alarming message confirms sign-in was cancelled and no error state is shown.

---

### User Story 2 - Email/Password Auth Preserved (Priority: P2)

Existing email/password registration and login flows remain fully functional alongside Google Sign-In. A user can choose either method independently.

**Why this priority**: The existing auth flow (003-auth-flow) must not regress. Users who registered with email/password must never be forced to switch to Google.

**Independent Test**: Register a new account with email/password, log out, log back in with email/password — full flow works identically to before this feature was added.

**Acceptance Scenarios**:

1. **Given** a user registered with email/password, **When** they log in via the email/password form, **Then** they are authenticated and redirected to the accounts page as before.
2. **Given** the login page is displayed, **Then** both the email/password form and the "Continue with Google" button are visible and functional.
3. **Given** a user who signed up with Google only (no password), **When** they attempt email/password login with their Google email, **Then** they see a clear message indicating their account uses Google sign-in and directing them to use that method.

---

### User Story 3 - One Tap Sign-In (Priority: P3)

A returning user who is already signed into Google in the browser sees a One Tap prompt — a non-intrusive overlay showing their Google account. Clicking it signs them in instantly without any interaction with the login page.

**Why this priority**: Significant UX improvement for returning users. Optional for v1 but included naturally by the GSI library.

**Independent Test**: Clear Finance Sentry session, navigate to any protected page, verify One Tap appears and completes sign-in on a single click.

**Acceptance Scenarios**:

1. **Given** a logged-out user who is signed into Google in the browser, **When** they navigate to the login page, **Then** a One Tap overlay appears showing their Google account.
2. **Given** the One Tap overlay is shown, **When** the user clicks it, **Then** they are authenticated and redirected to the accounts page without any other interaction.
3. **Given** the user dismisses the One Tap overlay, **When** they return to the login page, **Then** the standard "Continue with Google" button remains available as a fallback.

---

### Edge Cases

- What happens when Google's account picker is dismissed without selecting an account? → User stays on login page, a soft informational message is shown, no error state.
- What happens if the Google credential returned by the browser fails server-side verification? → User sees a generic "Google sign-in failed, please try again" message; no partial session is created.
- What happens if a Google email matches an existing email/password account? → Accounts are linked automatically by email on first Google sign-in; no duplicate is created.
- What happens if the same Google `sub` ID is presented but with a different email (rare Google account change)? → Match by `sub` first; email is updated on the user record if it has changed.
- What happens if Google's Identity Services script fails to load (ad-blockers, network issues)? → The button renders as a standard styled button; clicking it falls back gracefully or shows an error.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The login page MUST display a "Continue with Google" button using Google's official branded appearance.
- **FR-002**: The register page MUST display a "Continue with Google" button using Google's official branded appearance.
- **FR-003**: Clicking "Continue with Google" MUST trigger Google's account picker in the browser without a full-page redirect; the user selects their account and the app receives a signed credential directly.
- **FR-004**: The app MUST send the Google-issued credential to the backend for server-side verification before creating a session.
- **FR-005**: On successful credential verification, the backend MUST either create a new user account or authenticate the existing user (matched by Google account ID or email).
- **FR-006**: The system MUST link a Google sign-in to an existing email/password account when the Google email matches the registered email, preventing duplicate accounts.
- **FR-007**: The system MUST issue the same JWT access and refresh tokens for Google-authenticated users as for email/password users, so all downstream behaviour is identical.
- **FR-008**: A user who signed up via Google and has no password MUST NOT be able to log in via the email/password form; they MUST see guidance to use "Continue with Google" instead.
- **FR-009**: The existing email/password registration and login flows MUST continue to work without modification.
- **FR-010**: The system MUST store the Google account identifier against the user record to support reliable future sign-ins independent of email changes.
- **FR-011**: The One Tap sign-in overlay SHOULD appear for users who are already signed into Google in the browser, completing authentication on a single user interaction.

### Key Entities

- **User**: Existing entity — gains an optional Google account identifier field and a flag indicating whether a password is set.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can complete Google sign-in (from clicking the button to landing on the accounts page) in under 10 seconds, assuming they are already signed into Google in the browser.
- **SC-002**: An existing email/password user can sign in via Google without losing any data — 0% data loss on account linking.
- **SC-003**: The email/password login and register flows continue to work at the same success rate as before — no regression.
- **SC-004**: Google sign-in failure (network error, credential rejection, dismissal) always results in a user-visible message within 3 seconds; no blank screen or unhandled error.
- **SC-005**: 100% of successful Google sign-ins that result in a new account produce a valid, usable Finance Sentry account on the first attempt.
- **SC-006**: One Tap sign-in completes authentication in a single click for eligible returning users.

## Assumptions

- Google Identity Services (`accounts.google.com/gsi/client`) script is loaded by the frontend.
- Google Cloud Console is configured with a valid OAuth 2.0 client ID; the client secret is not required by this flow (the backend verifies the credential using Google's public keys, not by exchanging a secret).
- The backend verifies the Google-issued credential against Google's public key endpoint — no secret-based token exchange is needed.
- Account linking is automatic by email match (no manual linking UI required for v1).
- One Tap is enabled by default but users can dismiss it; it does not block access to the standard button.
- No other OAuth providers (GitHub, Apple, Microsoft) are in scope for this feature.

## Notes

- [DECISION] Auth mechanism: Replace server-side Authorization Code flow with Google Identity Services (GSI) client-side credential flow. The frontend receives a signed ID token directly from Google; the backend verifies it cryptographically. This eliminates CSRF state management (OAuthState entity), the code exchange step, and the backend-to-Google HTTP calls for token exchange.
- [DECISION] One Tap included: GSI provides One Tap at zero additional cost. It is in scope for this feature as it directly improves returning-user UX.
- [DECISION] Account linking: When a Google email matches an existing email/password account, automatically link them on first Google sign-in. No manual linking step required for v1.
- [DECISION] No-password users: Users who signed up via Google have no password hash stored. Attempting email/password login returns a specific error guiding them to Google.
- [DECISION] Client secret not required: The GSI credential flow uses Google's public keys for verification — no client secret is needed in the backend, simplifying secrets management.
- [OUT OF SCOPE] Server-side Authorization Code flow — replaced entirely by this feature.
- [OUT OF SCOPE] Other OAuth providers (GitHub, Apple, Microsoft) — deferred to a future feature.
- [OUT OF SCOPE] Allowing users to unlink their Google account from profile settings — deferred.
