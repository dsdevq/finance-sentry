# Feature Specification: Google OAuth Sign-In

**Feature Branch**: `004-adopt-oauth`
**Created**: 2026-04-15
**Status**: Draft
**Input**: User description: "I want you to implement OAuth sign in and sign up. Want to be able to login with my gmail. Make it clean, using best practices and so on. Take into account the existing sign in signup as I want to keep it as well."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Google Sign-In for Existing Users (Priority: P1)

A registered user (whether originally registered via email/password or Google) clicks "Continue with Google" on the login page, is redirected to Google's consent screen, and on approval lands on the accounts page authenticated. No additional form-filling required.

**Why this priority**: Google sign-in is the primary new capability. Existing users should be able to link their Google account to their existing Finance Sentry account so they aren't forced to create a duplicate account.

**Independent Test**: Create an account with email/password, then log out, click "Continue with Google" using the same Gmail address — user lands on accounts page with existing data intact.

**Acceptance Scenarios**:

1. **Given** a user is on the login page, **When** they click "Continue with Google" and approve the Google consent screen, **Then** they are authenticated and redirected to the accounts page.
2. **Given** a user previously registered with email/password using the same Gmail address, **When** they sign in with Google, **Then** their existing account is matched and they see their existing data (accounts are merged/linked, not duplicated).
3. **Given** a user clicks "Continue with Google" but cancels or denies the Google consent screen, **When** they are returned to the app, **Then** they see the login page with a clear message that sign-in was cancelled and no error state is shown.
4. **Given** a user is already logged in, **When** they navigate to `/login`, **Then** they are redirected to the accounts page.

---

### User Story 2 - Google Sign-Up for New Users (Priority: P2)

A first-time user clicks "Continue with Google" on the register page (or login page), approves Google consent, and their Finance Sentry account is automatically created using their Google profile data (name, email). They land on the accounts page ready to use the app.

**Why this priority**: New users should be able to onboard with one click rather than filling out a registration form, reducing friction and drop-off.

**Independent Test**: Use a Gmail account that has never registered — click "Continue with Google", approve, and verify a new Finance Sentry account is created and the user lands on the accounts page.

**Acceptance Scenarios**:

1. **Given** a first-time user with no Finance Sentry account, **When** they click "Continue with Google" and approve consent, **Then** a new account is created using their Google profile data and they are signed in and redirected to the accounts page.
2. **Given** a new user signs up via Google, **When** their account is created, **Then** no password is required and their display name and email are pre-filled from their Google profile.
3. **Given** a new user signs up via Google, **When** they later visit the login page, **Then** "Continue with Google" signs them back in without needing a password.

---

### User Story 3 - Email/Password Auth Preserved (Priority: P3)

Existing email/password registration and login flows remain fully functional alongside Google OAuth. A user can choose either method.

**Why this priority**: The existing auth flow (003-auth-flow) must not be broken. Users who registered with email/password should never be forced to use Google.

**Independent Test**: Register a new account with email/password, log out, log back in with email/password — full flow works identically to before this feature was added.

**Acceptance Scenarios**:

1. **Given** a user registered with email/password, **When** they log in via the email/password form, **Then** they are authenticated and redirected to the accounts page as before.
2. **Given** the login page, **When** displayed, **Then** both the email/password form and the "Continue with Google" button are visible and usable.
3. **Given** a user who signed up with Google, **When** they try to log in with email/password using the same email, **Then** they see a clear message indicating their account uses Google sign-in and they should use that method.

---

### Edge Cases

- What happens when Google returns an email that is already linked to a different Google account (e.g., account takeover attempt)? → Each Google `sub` ID is unique; match by Google account ID first, email second.
- What happens if Google's servers are unavailable during sign-in? → User sees an error message and can fall back to email/password.
- What happens if the user revokes Finance Sentry's Google permissions after signing in? → On next sign-in attempt via Google, they are prompted to re-approve. Existing session remains valid until it expires naturally.
- What happens if a user signs up via Google with an email that has no Google account linked but an email/password account exists? → Accounts are linked by email (with a confirmation step if needed to prevent hijack).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The login page MUST display a "Continue with Google" button alongside the existing email/password form.
- **FR-002**: The register page MUST display a "Continue with Google" button alongside the existing registration form.
- **FR-003**: Clicking "Continue with Google" MUST initiate a Google OAuth 2.0 authorization flow and redirect the user to Google's consent screen.
- **FR-004**: On successful Google authorization, the system MUST either create a new user account (if no matching account exists) or authenticate the existing user (if a matching account exists by Google ID or email).
- **FR-005**: The system MUST link a Google sign-in to an existing email/password account when the Google email matches the registered email, preventing duplicate accounts.
- **FR-006**: The system MUST issue the same JWT access and refresh tokens for Google-authenticated users as it does for email/password users, so all downstream app behaviour is identical.
- **FR-007**: A user who signed up via Google and has no password MUST NOT be able to log in via the email/password form; they MUST see guidance to use "Continue with Google" instead.
- **FR-008**: The existing email/password registration and login flows MUST continue to work without modification.
- **FR-009**: If the user cancels or denies the Google consent screen, the system MUST return them to the login page with a non-alarming informational message.
- **FR-010**: The system MUST store the Google account identifier (`sub`) against the user record to support future sign-ins without relying solely on email matching.

### Key Entities

- **User**: Existing entity — gains optional `googleId` field and a flag indicating whether a password is set (`hasPassword`).
- **OAuthState**: Short-lived server-side state token used to prevent CSRF during the OAuth redirect flow. Tied to a session/nonce, expires after one use.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new user can complete Google sign-up (from clicking the button to landing on the accounts page) in under 30 seconds, assuming they are already signed into Google in the browser.
- **SC-002**: An existing email/password user can link their Google account and sign in via Google without losing any existing data (0% data loss).
- **SC-003**: The email/password login and register flows continue to work at the same success rate as before this feature — no regression.
- **SC-004**: Google sign-in failure (network error, denial, cancelled) results in a user-visible message within 3 seconds of returning to the app; no blank screen or unhandled error.
- **SC-005**: 100% of Google sign-in attempts that result in account creation produce a valid, usable Finance Sentry account on first attempt.

## Assumptions

- The app has a publicly reachable redirect URI that Google can call back to (or `localhost` for development, registered in Google Cloud Console).
- Google OAuth 2.0 with the Authorization Code flow (server-side) is used — not implicit flow, which is deprecated.
- A single Google Cloud project with an OAuth 2.0 client ID and secret will be configured (credentials stored as environment variables, not hardcoded).
- Users have a Google/Gmail account they are willing to use; no other OAuth providers (GitHub, Apple, etc.) are in scope for this feature.
- Account linking is automatic by email match (no manual linking UI required for v1).
- The Google "Continue with Google" button follows Google's branding guidelines (logo + text).

## Notes

- [DECISION] Auth strategy: Keep the existing email/password auth (003-auth-flow) fully intact. Add Google OAuth as an additional sign-in method. Both produce the same JWT tokens. This is the "unified auth" pattern — one user table, multiple credential types.
- [DECISION] Account linking: When a Google email matches an existing email/password account, automatically link them (add `googleId` to the existing record) on first Google sign-in. No manual linking step required for v1.
- [DECISION] No-password users: Users who only signed up via Google have no password hash stored. Attempting email/password login with their email returns a specific error guiding them to use Google.
- [OUT OF SCOPE] Other OAuth providers (GitHub, Apple, Microsoft) — deferred to a future feature.
- [OUT OF SCOPE] Allowing users to unlink their Google account from the profile settings page — deferred.
- [OUT OF SCOPE] Google One Tap sign-in widget — standard redirect flow only for v1.
