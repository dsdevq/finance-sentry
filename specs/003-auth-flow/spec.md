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

A logged-in user closes the browser tab and reopens the app. They should still be authenticated without having to log in again, even if the short-lived access token has expired — the app silently refreshes it using the stored refresh token.

**Why this priority**: Without persistence, every page refresh forces re-login, making the app unusable. Depends on token storage from P1.

**Independent Test**: Log in, close and reopen the browser, navigate to a protected page — user should see their data without being redirected to login, even after the access token has expired.

**Acceptance Scenarios**:

1. **Given** the user is logged in, **When** they refresh the page or reopen the browser, **Then** they remain authenticated and protected pages are accessible.
2. **Given** the user's access token has expired but the refresh token is still valid, **When** they make an API call, **Then** the app transparently obtains a new access token and retries the request without any user interaction.
3. **Given** the user's refresh token has expired (after 30 days), **When** they reopen the app, **Then** they are redirected to the login page.
4. **Given** the user clicks "Log out", **When** the action completes, **Then** the stored access token and refresh token cookie are both cleared and subsequent navigation to protected pages redirects to login.

---

### User Story 4 - Automatic Token Attachment (Priority: P4)

Every API request made by the app automatically includes the stored Bearer token in the Authorization header. The user sees this as seamless data loading — they never have to think about authentication headers.

**Why this priority**: Without this, every API call returns 401 even after login. Depends on token storage (P3) being in place.

**Independent Test**: Log in, navigate to the accounts page — the accounts list loads successfully, confirming the interceptor is attaching the token to outgoing requests.

**Acceptance Scenarios**:

1. **Given** the user is logged in, **When** any page makes an API call, **Then** the request includes a valid Authorization header and the server returns data.
2. **Given** the user's access token is expired but refresh token is valid, **When** an API call returns 401, **Then** the interceptor automatically refreshes the access token and retries the original request exactly once.
3. **Given** the user is not logged in, **When** the app attempts an API call, **Then** the user is redirected to the login page.

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
- What happens if the refresh token is valid but the server rejects it (e.g., server-side revocation)?
- What happens if the user opens the app in two tabs and both simultaneously attempt a token refresh?
- What happens if local storage is unavailable (e.g., private browsing with storage restrictions)?
- What happens when the user submits the login form multiple times rapidly before the first response arrives?
- What happens if a refresh attempt itself fails with a network error — should the user be logged out or shown a retry option?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a login page where users can authenticate with email and password.
- **FR-002**: The system MUST provide a register page where new users can create an account with email and password.
- **FR-003**: On successful login or registration, the system MUST store the returned JWT access token so it persists across page refreshes, and the refresh token MUST be stored in an httpOnly cookie set by the server.
- **FR-004**: The system MUST automatically attach the stored JWT access token as a Bearer token on every outgoing API request while the token remains valid.
- **FR-005**: The system MUST redirect unauthenticated users to the login page when they attempt to access any protected route.
- **FR-006**: After a successful login following a redirect, the system MUST return the user to the originally requested page.
- **FR-007**: The system MUST display clear, user-friendly error messages for failed login attempts, registration failures, and network errors.
- **FR-008**: The system MUST validate required fields client-side before submitting credentials to the server.
- **FR-009**: The system MUST provide a logout action that clears the stored access token, invalidates the refresh token server-side, and redirects to the login page.
- **FR-010**: Password fields MUST mask input by default, with an option to reveal the password.
- **FR-011**: The system MUST enforce minimum password requirements during registration and communicate them clearly to the user.
- **FR-012**: If an API call returns 401 and a valid refresh token cookie exists, the system MUST automatically call the refresh endpoint to obtain a new access token and retry the original request exactly once. If the refresh also fails, the user is redirected to login.
- **FR-013**: A user who is already authenticated MUST be redirected away from the login and register pages.
- **FR-014**: The server MUST issue a new refresh token on every successful refresh (token rotation), immediately invalidating the previous one.
- **FR-015**: Refresh tokens MUST expire after 30 days of inactivity. Each successful refresh resets the 30-day window.
- **FR-016**: The server MUST store refresh tokens (or their hashes) to support server-side validation and rotation. A used or expired refresh token MUST be rejected.

### Key Entities

- **User Session**: Represents an authenticated user's active state — includes the JWT access token and any decoded claims (expiry, user identity). Not persisted server-side; derived from the stored token on each page load.
- **Credentials**: Email and password pair submitted at login or registration. Never retained by the client beyond the single request.
- **Access Token (JWT)**: Short-lived credential returned by the server on successful authentication or token refresh. Proves identity for all subsequent API requests. Contains an expiry claim used to determine session validity.
- **Refresh Token**: Long-lived credential (30-day rolling window) stored in an httpOnly cookie. Used exclusively to obtain new access tokens without re-entering credentials. Rotated on every use — each refresh issues a new refresh token and invalidates the previous one.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A returning user can go from landing on the login page to viewing their accounts list in under 30 seconds.
- **SC-002**: A new user can complete account registration and reach the accounts page in under 2 minutes.
- **SC-003**: After login, 100% of API requests succeed (no 401 responses) for a session with a valid, non-expired access token.
- **SC-004**: Navigating to any protected URL without a valid session redirects to login within 1 second, with no partial page content visible.
- **SC-005**: A logged-in user who refreshes the page or reopens the browser remains authenticated without re-entering credentials, for up to 30 days since their last activity.
- **SC-006**: Logout completes and the user is on the login page within 1 second of triggering the action.
- **SC-007**: When an access token expires mid-session, the app transparently refreshes it and continues without any visible interruption to the user.

## Assumptions

- Password minimum requirements (e.g., 8+ characters) are enforced server-side; the frontend surfaces server validation messages for anything beyond basic non-empty checks.
- Token storage: access token uses the browser's local storage; refresh token is set as an httpOnly cookie by the server. If local storage is unavailable (e.g., blocked by browser policy), the access token session will be in-memory only for that tab — no silent fallback is required.
- The backend login and register endpoints exist or will be built as part of this feature's implementation phase. The spec covers the full auth flow end to end.
- Email uniqueness validation is enforced server-side; the frontend relays the error message returned by the API.
- This is a single-user app (Denys); the register page does not need to be publicly discoverable but must exist and function correctly.
- The app is not exposed to the public internet, so local storage for access tokens is an acceptable trade-off.
- The refresh token cookie is scoped to the API domain and sent automatically by the browser on requests to the refresh endpoint.

## Notes

- [DECISION] Token storage: Access token in local storage so the session survives browser restarts. Refresh token in httpOnly cookie (server-set) so it is inaccessible to JavaScript — mitigates XSS risk for the long-lived credential.
- [DECISION] Interceptor scope: The HTTP interceptor attaches the access token only to requests targeting the app's API base URL. Requests to other origins are not modified.
- [DECISION] Token expiry handling: When an API call returns 401, the interceptor attempts a silent refresh using the httpOnly refresh token cookie. If the refresh succeeds, the original request is retried once. If the refresh fails (refresh token expired or invalid), the user is redirected to login.
- [DECISION] Token rotation: Refresh tokens rotate on every use. Each refresh call invalidates the previous refresh token and issues a new one. This limits the damage window if a cookie is ever compromised.
- [DECISION] Refresh token lifetime: 30-day rolling window. Each successful refresh resets the expiry. After 30 days of inactivity the user must log in again.
- [OUT OF SCOPE] Forgot password / email verification: Deferred to a future feature.
- [OUT OF SCOPE] Multi-factor authentication: Out of scope for this iteration.
- [OUT OF SCOPE] OAuth / social login: Deferred to feature 004-adopt-oauth.

## Clarifications

### Session 2026-04-11

- Q: Where should refresh tokens be stored? → A: httpOnly cookie (server-set, inaccessible to JavaScript)
- Q: Should refresh tokens rotate on every use? → A: Yes — each refresh issues a new token and invalidates the old one
- Q: How long should refresh tokens remain valid? → A: 30 days (rolling window, reset on each successful refresh)
