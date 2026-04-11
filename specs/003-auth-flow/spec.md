# Feature Specification: Auth Flow

**Feature Branch**: `003-auth-flow`  
**Created**: 2026-04-11  
**Status**: Draft  
**Input**: User description: "Auth flow — login and register pages, JWT token storage, HTTP interceptor to attach Bearer token to all API requests, route guards to protect authenticated pages."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Login (Priority: P1)

A registered user enters their email and password on the login page and gains access to the app. On success they are redirected to the accounts page. On failure they see a clear error message and can try again.

**Why this priority**: Nothing else in the app is accessible without a valid session. Every other user story depends on this working first.

**Independent Test**: Can be fully tested by submitting valid credentials on the login page and verifying the user lands on the accounts page with all API calls returning data instead of 401 errors.

**Acceptance Scenarios**:

1. **Given** the user is not logged in and visits the login page, **When** they enter correct credentials and submit, **Then** they are redirected to the accounts page and all subsequent pages are accessible.
2. **Given** the user is on the login page, **When** they enter an incorrect password, **Then** a descriptive error message is shown and they remain on the login page.
3. **Given** the user is on the login page, **When** they submit with empty fields, **Then** field-level validation messages appear before the request is sent.
4. **Given** the user is already logged in, **When** they navigate to `/login`, **Then** they are automatically redirected to the accounts page.

---

### User Story 2 - Register (Priority: P2)

A new user can create an account by providing their email and password. On success they are automatically logged in and redirected to the accounts page. On failure (e.g., email already taken) they see a clear error.

**Why this priority**: Required to bootstrap the first user. Depends on login infrastructure (P1) already being in place.

**Independent Test**: Can be tested by submitting a new email/password on the register page and verifying the user is authenticated and lands on the accounts page without needing to log in separately.

**Acceptance Scenarios**:

1. **Given** the user visits the register page and provides a unique email and valid password, **When** they submit, **Then** their account is created, they are logged in, and they are redirected to the accounts page.
2. **Given** the user attempts to register with an email that already exists, **When** they submit, **Then** an error message indicates the email is taken and the form remains editable.
3. **Given** the user provides a password that does not meet the minimum requirements, **When** they submit, **Then** validation feedback is shown before the request is sent.
4. **Given** the register page, **When** navigated to by a user who is already logged in, **Then** they are redirected to the accounts page.

---

### User Story 3 - Session Persistence (Priority: P3)

A logged-in user closes the browser tab and reopens the app. They should still be authenticated without having to log in again.

**Why this priority**: Without persistence, every page refresh forces re-login, making the app unusable. Depends on token storage from P1.

**Independent Test**: Log in, close and reopen the browser, navigate to a protected page — user should see their data without being redirected to login.

**Acceptance Scenarios**:

1. **Given** the user is logged in, **When** they refresh the page or reopen the browser, **Then** they remain authenticated and protected pages are accessible.
2. **Given** the user's stored token has expired, **When** they reopen the app, **Then** they are redirected to the login page.
3. **Given** the user clicks "Log out", **When** the action completes, **Then** the stored token is removed and subsequent navigation to protected pages redirects to login.

---

### User Story 4 - Automatic Token Attachment (Priority: P4)

Every API request made by the app automatically includes the stored Bearer token in the Authorization header. The user sees this as seamless data loading — they never have to think about authentication headers.

**Why this priority**: Without this, every API call returns 401 even after login. Depends on token storage (P3) being in place.

**Independent Test**: Log in, navigate to the accounts page — the accounts list loads successfully, confirming the interceptor is attaching the token to outgoing requests.

**Acceptance Scenarios**:

1. **Given** the user is logged in, **When** any page makes an API call, **Then** the request includes a valid Authorization header and the server returns data.
2. **Given** the user is not logged in, **When** the app attempts an API call, **Then** the request is either blocked before sending or the resulting 401 triggers a redirect to the login page.

---

### User Story 5 - Route Guards (Priority: P5)

Any page that requires authentication redirects unauthenticated users to the login page. After login, users are returned to the page they originally tried to visit.

**Why this priority**: Completes the security boundary. Without guards, the UI is accessible even though the API calls fail. Depends on the full auth flow (P1–P4) being working.

**Independent Test**: Without being logged in, paste the URL for the accounts page directly into the browser — the app should redirect to login. After logging in, the user should land on the accounts page.

**Acceptance Scenarios**:

1. **Given** the user is not logged in, **When** they navigate to any protected route (e.g., `/accounts`, `/dashboard`), **Then** they are redirected to `/login`.
2. **Given** the user was redirected to login from `/accounts`, **When** they successfully log in, **Then** they are redirected back to `/accounts`, not to a generic landing page.
3. **Given** the user is logged in, **When** they navigate to any protected route, **Then** they are not redirected and the page loads normally.

---

### Edge Cases

- What happens when the API is unreachable during login (network error)?
- How does the app handle a token that is syntactically valid but rejected by the server (revoked or signed with a different key)?
- What happens if the user's token expires mid-session while they are actively using the app?
- What happens if local storage is unavailable (e.g., private browsing with storage restrictions)?
- What happens when the user submits the login form multiple times rapidly before the first response arrives?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a login page where users can authenticate with email and password.
- **FR-002**: The system MUST provide a register page where new users can create an account with email and password.
- **FR-003**: On successful login or registration, the system MUST store the returned JWT so it persists across page refreshes.
- **FR-004**: The system MUST automatically attach the stored JWT as a Bearer token on every outgoing API request while the token remains valid.
- **FR-005**: The system MUST redirect unauthenticated users to the login page when they attempt to access any protected route.
- **FR-006**: After a successful login following a redirect, the system MUST return the user to the originally requested page.
- **FR-007**: The system MUST display clear, user-friendly error messages for failed login attempts, registration failures, and network errors.
- **FR-008**: The system MUST validate required fields client-side before submitting credentials to the server.
- **FR-009**: The system MUST provide a logout action that clears the stored token and redirects to the login page.
- **FR-010**: Password fields MUST mask input by default, with an option to reveal the password.
- **FR-011**: The system MUST enforce minimum password requirements during registration and communicate them clearly to the user.
- **FR-012**: If a stored token is expired or invalid, the system MUST treat the user as unauthenticated and redirect to login.
- **FR-013**: A user who is already authenticated MUST be redirected away from the login and register pages.

### Key Entities

- **User Session**: Represents an authenticated user's active state — includes the JWT and any decoded claims (expiry, user identity). Not persisted server-side; derived from the stored token on each page load.
- **Credentials**: Email and password pair submitted at login or registration. Never retained by the client beyond the single request.
- **JWT (JSON Web Token)**: The credential returned by the server on successful authentication. Proves identity for all subsequent API requests. Contains an expiry claim used to determine session validity.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A returning user can go from landing on the login page to viewing their accounts list in under 30 seconds.
- **SC-002**: A new user can complete account registration and reach the accounts page in under 2 minutes.
- **SC-003**: After login, 100% of API requests succeed (no 401 responses) for a session with a valid, non-expired token.
- **SC-004**: Navigating to any protected URL without a valid session redirects to login within 1 second, with no partial page content visible.
- **SC-005**: A logged-in user who refreshes the page or reopens the browser tab remains authenticated without re-entering credentials, as long as their token has not expired.
- **SC-006**: Logout completes and the user is on the login page within 1 second of triggering the action.

## Assumptions

- Password minimum requirements (e.g., 8+ characters) are enforced server-side; the frontend surfaces server validation messages for anything beyond basic non-empty checks.
- Token storage uses the browser's local storage. If storage is unavailable (e.g., blocked by browser policy), the session will be in-memory only for that tab — no silent fallback is required.
- The backend login and register endpoints exist or will be built as part of this feature's implementation phase. The spec covers the full auth flow end to end.
- Email uniqueness validation is enforced server-side; the frontend relays the error message returned by the API.
- This is a single-user app (Denys); the register page does not need to be publicly discoverable but must exist and function correctly.
- The app is not exposed to the public internet, so local storage token storage is an acceptable trade-off.

## Notes

- [DECISION] Token storage: Local storage chosen over session storage so the session survives browser restarts, which suits a daily-use personal finance app. XSS risk is acceptable given the app is not public-facing.
- [DECISION] Interceptor scope: The HTTP interceptor attaches the token only to requests targeting the app's API base URL. Requests to other origins are not modified.
- [DECISION] Token expiry handling: When a stored token is expired or the server returns 401, the user is redirected to login. No automatic token refresh in this iteration.
- [OUT OF SCOPE] Refresh tokens: Token rotation deferred. Expired sessions require re-login.
- [OUT OF SCOPE] Forgot password / email verification: Deferred to a future feature.
- [OUT OF SCOPE] Multi-factor authentication: Out of scope for this iteration.
- [DEFERRED] Auth interceptor error recovery (auto-retry after token refresh): Deferred until refresh tokens are implemented.
