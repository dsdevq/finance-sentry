# Tasks: Google OAuth Sign-In (004-adopt-oauth)

**Input**: `specs/004-adopt-oauth/spec.md`
**Prerequisites**: spec.md ✅
**Branch**: `004-adopt-oauth`

**Tests**: Contract tests for all new/modified endpoints (mandatory). Unit tests for `GoogleOAuthService`. No E2E tests.

**Strategy**: Authorization Code flow (server-side). Backend handles the entire OAuth dance — Angular never touches Google tokens. After a successful callback the backend redirects the frontend to `/auth/callback?token=<jwt>&userId=<id>&expiresAt=<iso>` so Angular can store the token exactly as it does after email/password login. The `fs_refresh_token` httpOnly cookie is set by the backend during the redirect, same as the email/password flow.

---

## Phase 1: Domain & Persistence

**Purpose**: Extend `ApplicationUser` with `GoogleId` and add the short-lived `OAuthState` entity used to prevent CSRF during the OAuth redirect round-trip.

- [x] **T001** Add `public string? GoogleId { get; set; }` property to `ApplicationUser.cs` in `backend/src/FinanceSentry.Modules.Auth/Domain/Entities/ApplicationUser.cs`
- [x] **T002** [P] Create `OAuthState.cs` entity (`Guid Id`, `string State`, `DateTimeOffset ExpiresAt`, `bool IsUsed`, `DateTimeOffset CreatedAt`) in `backend/src/FinanceSentry.Modules.Auth/Domain/Entities/OAuthState.cs`
- [x] **T003** Add `DbSet<OAuthState> OAuthStates` to `AuthDbContext`; configure `OAuthState` with a unique index on `State` and index on `ExpiresAt` (for cleanup) in `backend/src/FinanceSentry.Modules.Auth/Infrastructure/Persistence/AuthDbContext.cs`
- [x] **T004** Run `dotnet ef migrations add M008_GoogleOAuth --project FinanceSentry.Modules.Auth --context AuthDbContext` from `backend/` — verify migration files created in `Infrastructure/Persistence/Migrations/`

---

## Phase 2: Google OAuth Service

**Purpose**: Encapsulate all HTTP communication with Google's OAuth endpoints behind an interface so it can be mocked in tests.

- [x] **T005** Create `GoogleOAuthOptions.cs` POCO (`string ClientId`, `string ClientSecret`, `string RedirectUri`, `string FrontendUrl`) bound from `appsettings.json` section `"GoogleOAuth"` in `backend/src/FinanceSentry.Modules.Auth/Infrastructure/Services/GoogleOAuthOptions.cs`
- [x] **T006** [P] Create `IGoogleOAuthService.cs` interface with:
  - `string GetAuthorizationUrl(string state)` — builds Google consent screen URL with `scope=openid email profile`
  - `Task<GoogleUserInfo> ExchangeCodeAsync(string code)` — exchanges auth code for Google tokens, calls userinfo endpoint
  - `GoogleUserInfo` record: `string Sub`, `string Email`, `string? Name`
  In `backend/src/FinanceSentry.Modules.Auth/Application/Interfaces/IGoogleOAuthService.cs`
- [x] **T007** Create `GoogleOAuthService.cs` implementing `IGoogleOAuthService` using `IHttpClientFactory` (named client `"google"`):
  - `GetAuthorizationUrl`: builds `https://accounts.google.com/o/oauth2/v2/auth` URL with client_id, redirect_uri, scope, response_type=code, state, access_type=offline, prompt=consent
  - `ExchangeCodeAsync`: POSTs to `https://oauth2.googleapis.com/token`, then GETs `https://www.googleapis.com/oauth2/v3/userinfo` with the access token
  In `backend/src/FinanceSentry.Modules.Auth/Infrastructure/Services/GoogleOAuthService.cs`
- [x] **T008** In `backend/src/FinanceSentry.API/Program.cs`:
  - Bind `GoogleOAuthOptions` from config section `"GoogleOAuth"`
  - Register named `HttpClient` `"google"` with base address `https://accounts.google.com`
  - Register `IGoogleOAuthService → GoogleOAuthService` as scoped

---

## Phase 3: Backend Commands & Endpoints

**Purpose**: Two new endpoints (`GET /auth/google/login` initiates the flow; `GET /auth/google/callback` completes it) plus a guard in `LoginCommandHandler` for Google-only accounts.

### Contract Tests (write before implementation)

- [x] **T009** [P] Contract tests in `backend/tests/FinanceSentry.Tests.Integration/Auth/GoogleOAuthContractTests.cs`:
  - `GET /auth/google/login` → 302 with `Location` header starting with `https://accounts.google.com/o/oauth2/v2/auth`
  - `GET /auth/google/callback?error=access_denied&state=<valid>` → 302 to `{FrontendUrl}/auth/callback?error=cancelled`
  - `GET /auth/google/callback` with missing/invalid `state` → 400

### Implementation

- [x] **T010** Create `InitiateGoogleLoginQuery.cs` (no input fields) and `InitiateGoogleLoginQueryHandler.cs`:
  - Generates a cryptographically random `state` string (`Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))`)
  - Saves `OAuthState` record with `ExpiresAt = UtcNow + 10 minutes`
  - Returns the Google authorization URL from `IGoogleOAuthService.GetAuthorizationUrl(state)`
  In `backend/src/FinanceSentry.Modules.Auth/Application/Queries/`
- [x] **T011** Create `HandleGoogleCallbackCommand.cs` (`string Code`, `string State`) and `HandleGoogleCallbackCommandHandler.cs`:
  - Validates `OAuthState` exists, is not expired, is not used — throws `BadRequestException("INVALID_OAUTH_STATE")` if not
  - Marks state as used (`IsUsed = true`)
  - Calls `IGoogleOAuthService.ExchangeCodeAsync(Code)` → `GoogleUserInfo`
  - Finds existing user by `GoogleId == googleInfo.Sub` first, then by `NormalizedEmail` if no GoogleId match — if email match found, sets `GoogleId` and saves
  - If no user exists, creates new `ApplicationUser` with `Email`, `UserName = Email`, `EmailConfirmed = true` using `UserManager.CreateAsync` (no password)
  - Issues JWT via `ITokenService.GenerateToken(user)` and refresh token via `IRefreshTokenService.IssueAsync(user.Id)`
  - Returns `(AuthResult authResult, string rawRefreshToken)`
  In `backend/src/FinanceSentry.Modules.Auth/Application/Commands/`
- [x] **T012** Update `LoginCommandHandler.cs`: after finding the user by email but before checking password, if `user.PasswordHash == null` throw `UnauthorizedException("GOOGLE_ACCOUNT_ONLY")` so the caller knows to show "Use Google sign-in instead" in `backend/src/FinanceSentry.Modules.Auth/Application/Commands/LoginCommandHandler.cs`
- [x] **T013** Add two actions to `AuthController.cs`:
  - `GET /api/v1/auth/google/login` — dispatches `InitiateGoogleLoginQuery`, returns `Redirect(authorizationUrl)`; add `/api/v1/auth/google` to `JwtAuthenticationMiddleware` exempt prefixes
  - `GET /api/v1/auth/google/callback` — if `error` query param present, redirect to `{FrontendUrl}/auth/callback?error=cancelled`; else dispatch `HandleGoogleCallbackCommand`, set `fs_refresh_token` httpOnly cookie, redirect to `{FrontendUrl}/auth/callback?token=<jwt>&userId=<id>&expiresAt=<iso>`
  In `backend/src/FinanceSentry.Modules.Auth/API/Controllers/AuthController.cs`

---

## Phase 4: Frontend

**Purpose**: Angular callback handler, Google button on login/register pages, and a clear error message for Google-only accounts trying to use the password form.

- [x] **T014** Add to `auth.service.ts`:
  - `googleLogin(): void` — `window.location.href = '/api/v1/auth/google/login'` (full page navigation, not Angular router)
  - `handleOAuthCallback(token: string, userId: string, expiresAt: string): void` — stores token in localStorage, updates `currentUser$`, navigates to `/accounts`
  In `frontend/src/app/modules/auth/services/auth.service.ts`
- [x] **T015** Create `OAuthCallbackComponent` (standalone, `ChangeDetectionStrategy.OnPush`, selector `fns-oauth-callback`) in `frontend/src/app/modules/auth/pages/oauth-callback/`:
  - On `ngOnInit` reads `token`, `userId`, `expiresAt`, `error` from `ActivatedRoute.queryParams`
  - If `error === 'cancelled'` navigates to `/login` with `{ queryParams: { info: 'google_cancelled' } }`
  - If `error` present (any other value) navigates to `/login` with `{ queryParams: { error: 'google_failed' } }`
  - If `token` present calls `authService.handleOAuthCallback(...)` → navigates to `/accounts`
  - Shows a brief "Signing you in…" message while processing (no blank screen)
- [x] **T016** Update `LoginComponent`:
  - Add "Continue with Google" button (Google branding: white bg, Google `G` SVG icon from assets, text "Continue with Google") that calls `authService.googleLogin()`
  - Read `queryParams.info === 'google_cancelled'` → show info banner "Google sign-in was cancelled. Try again or use email/password."
  - Read `queryParams.error === 'google_failed'` → show error banner "Google sign-in failed. Please try again."
  - Handle `GOOGLE_ACCOUNT_ONLY` error from login API → show "This account uses Google sign-in. Click 'Continue with Google' instead."
  In `frontend/src/app/modules/auth/pages/login/`
- [x] **T017** [P] Update `RegisterComponent`: add "Continue with Google" button identical to T016; it also calls `authService.googleLogin()` (same endpoint handles both new and existing users) in `frontend/src/app/modules/auth/pages/register/`
- [x] **T018** Add `/auth/google/callback` route (loads `OAuthCallbackComponent`, no auth guard — it IS the auth landing) to `frontend/src/app/modules/auth/auth.routes.ts` and ensure it is included in `app.routes.ts`

---

## Phase 5: Configuration & Polish

**Purpose**: Wire environment variables into Docker, bump versions.

- [x] **T019** Add Google OAuth env vars to `docker/docker-compose.dev.yml` under the `api` service:
  ```
  GOOGLEOAUTH__CLIENTID: "${GOOGLE_CLIENT_ID:-}"
  GOOGLEOAUTH__CLIENTSECRET: "${GOOGLE_CLIENT_SECRET:-}"
  GOOGLEOAUTH__REDIRECTURI: "http://localhost:5000/api/v1/auth/google/callback"
  GOOGLEOAUTH__FRONTENDURL: "http://localhost:4200"
  ```
  And add the same keys (with empty defaults) to `appsettings.json` `"GoogleOAuth"` section in `backend/src/FinanceSentry.API/`
- [x] **T020** [P] Bump backend version `0.2.0 → 0.3.0` in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj`; bump frontend version `0.3.0 → 0.4.0` in `frontend/package.json` (MINOR — new OAuth feature)
- [x] **T021** Manual validation:
  - `GET http://localhost:5000/api/v1/auth/google/login` returns 302 pointing to `https://accounts.google.com/…`
  - Login page at `http://localhost:4200/login` shows the "Continue with Google" button
  - Attempting to login with a Google-only account via password form shows the correct guidance message

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Domain & DB)**: No dependencies — start immediately
- **Phase 2 (Google OAuth Service)**: Depends on Phase 1 (GoogleOAuthOptions must exist for DI registration)
- **Phase 3 (Commands & Endpoints)**: Depends on Phase 1 + 2; T012 (LoginCommandHandler update) is independent of T009–T011
- **Phase 4 (Frontend)**: T014 can start after Phase 3 commands are defined (interfaces known); T015–T018 depend on T014
- **Phase 5 (Polish)**: Depends on all prior phases

### Parallel Opportunities

- T002 + T003 can run with T001 (different files)
- T006 can be written in parallel with T007 (interface before impl, but in same PR)
- T009 (contract tests) can be written in parallel with T010–T011 (TDD)
- T017 (register button) can be written in parallel with T016 (login button)
- T020 (version bumps) is independent of T019 + T021

---

## Notes

- `GoogleId` match takes precedence over email match to prevent account takeover: always check `GoogleId` first, then fall back to email (see spec edge cases)
- The Google `G` SVG icon should be added to `frontend/src/assets/icons/google.svg` (or inlined in the component); use Google's official branding asset
- `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` must be obtained from Google Cloud Console > APIs & Services > Credentials; register `http://localhost:5000/api/v1/auth/google/callback` as an authorized redirect URI
- `OAuthState` cleanup: expired/used states accumulate in DB — acceptable for v1 (personal app, low volume); add a Hangfire job in a future task if needed
- Do NOT add `GOOGLE_CLIENT_ID` or `GOOGLE_CLIENT_SECRET` to version-controlled files — use `.env` file or shell environment variables that `docker-compose.dev.yml` reads from the host
