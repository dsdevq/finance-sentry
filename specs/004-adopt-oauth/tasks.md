# Tasks: Google Sign-In via Identity Services (004-adopt-oauth)

**Input**: `specs/004-adopt-oauth/spec.md`, `plan.md`, `research.md`, `contracts/google-oauth.md`
**Prerequisites**: spec.md ✅ · plan.md ✅ · research.md ✅
**Branch**: `004-adopt-oauth`

**Tests**: Contract tests for `POST /auth/google/verify` (mandatory per constitution). No E2E tests.

**Strategy**: GSI client-side credential flow. Browser calls `google.accounts.id.initialize()` → user picks account → JS callback receives signed ID token → frontend POSTs `{ credential }` to `POST /api/v1/auth/google/verify` → backend verifies with `Google.Apis.Auth` → issues JWT. No redirects, no CSRF state, no client secret needed. One Tap included via `google.accounts.id.prompt()`.

**Note**: `/api/v1/auth` prefix is already fully exempt from `JwtAuthenticationMiddleware` — no middleware changes needed for the new endpoint.

---

## Phase 1: Cleanup — Remove Old Server-Side OAuth Code

**Purpose**: Delete all files and code from the Authorization Code flow implementation before building the GSI replacement.

- [x] T001 Delete `backend/src/FinanceSentry.Modules.Auth/Domain/Entities/OAuthState.cs`
- [x] T002 [P] Delete `backend/src/FinanceSentry.Modules.Auth/Application/Interfaces/IGoogleOAuthService.cs`
- [x] T003 [P] Delete `backend/src/FinanceSentry.Modules.Auth/Infrastructure/Services/GoogleOAuthOptions.cs` and `GoogleOAuthService.cs`
- [x] T004 [P] Delete `backend/src/FinanceSentry.Modules.Auth/Application/Queries/InitiateGoogleLoginQuery.cs` and `InitiateGoogleLoginQueryHandler.cs`
- [x] T005 [P] Delete `backend/src/FinanceSentry.Modules.Auth/Application/Commands/HandleGoogleCallbackCommand.cs` and `HandleGoogleCallbackCommandHandler.cs`
- [x] T006 [P] Delete `backend/tests/FinanceSentry.Tests.Integration/Auth/GoogleOAuthContractTests.cs`
- [x] T007 [P] Delete `frontend/src/app/modules/auth/pages/oauth-callback/oauth-callback.component.ts` and `oauth-callback.component.html`
- [x] T008 Update `frontend/src/app/app.routes.ts` — remove the `/auth/callback` route that loaded `OAuthCallbackComponent`

**Checkpoint**: All server-side OAuth files deleted. Build will fail until Phase 2 resolves references.

---

## Phase 2: Foundational — New Infrastructure

**Purpose**: Add GSI dependencies, new domain interface, updated DB context, migration, and new infrastructure service. Must be complete before US1 implementation.

- [x] T009 Add `Google.Apis.Auth` NuGet package to `backend/src/FinanceSentry.Modules.Auth/FinanceSentry.Modules.Auth.csproj`
- [x] T010 [P] Add `@types/google.accounts` dev dependency to `frontend/package.json`; add `<script src="https://accounts.google.com/gsi/client" async></script>` to `frontend/src/index.html`
- [x] T011 [P] Simplify config: update `backend/src/FinanceSentry.API/appsettings.json` — keep only `"GoogleOAuth": { "ClientId": "" }`; update `docker/docker-compose.dev.yml` — remove `GOOGLEOAUTH__CLIENTSECRET`, `GOOGLEOAUTH__REDIRECTURI`, `GOOGLEOAUTH__FRONTENDURL` env vars; add `googleClientId` field to `frontend/src/environments/environment.ts` using the value from `GOOGLE_CLIENT_ID`
- [x] T012 Create `backend/src/FinanceSentry.Modules.Auth/Application/Interfaces/IGoogleCredentialVerifier.cs` — define `record GoogleUserInfo(string GoogleId, string Email, string? DisplayName)` and `interface IGoogleCredentialVerifier { Task<GoogleUserInfo> VerifyAsync(string credential); }`
- [x] T013 [P] Create `backend/src/FinanceSentry.Modules.Auth/Application/Commands/VerifyGoogleCredentialCommand.cs` — `record VerifyGoogleCredentialCommand(string Credential) : IRequest<AuthResult>`
- [x] T014 Update `backend/src/FinanceSentry.Modules.Auth/Infrastructure/Persistence/AuthDbContext.cs` — remove `public DbSet<OAuthState> OAuthStates => Set<OAuthState>();` and remove `OAuthState` entity configuration from `OnModelCreating`
- [x] T015 Create EF Core migration `M009_DropOAuthStates` in `backend/src/FinanceSentry.Modules.Auth/Infrastructure/Persistence/Migrations/` — `Up()` calls `migrationBuilder.DropIndex("IX_OAuthStates_State", "OAuthStates")`, `migrationBuilder.DropIndex("IX_OAuthStates_ExpiresAt", "OAuthStates")`, `migrationBuilder.DropTable("OAuthStates")`; `Down()` recreates the table with all columns and indexes exactly as in `M008_GoogleOAuth.cs`; update `AuthDbContextModelSnapshot.cs` to remove `OAuthStates` table
- [x] T016 Create `backend/src/FinanceSentry.Modules.Auth/Infrastructure/Services/GoogleCredentialVerifier.cs` — implements `IGoogleCredentialVerifier`; uses `GoogleJsonWebSignature.ValidateAsync(credential, new ValidationSettings { Audience = [clientId] })`; returns `GoogleUserInfo(payload.Subject, payload.Email, payload.Name)`; throws `InvalidOperationException("INVALID_GOOGLE_CREDENTIAL")` on validation failure; reads `clientId` from injected `IOptions<GoogleOAuthOptions>`; reuse `GoogleOAuthOptions` class but strip it to only `ClientId` property
- [x] T017 Update `backend/src/FinanceSentry.Modules.Auth/Infrastructure/Services/GoogleOAuthOptions.cs` — strip to only `public string ClientId { get; set; } = string.Empty;` (remove `ClientSecret`, `RedirectUri`, `FrontendUrl`)
- [x] T018 Update `backend/src/FinanceSentry.API/Program.cs` — remove `AddHttpClient("google")`, `AddScoped<IGoogleOAuthService, GoogleOAuthService>()`; keep `Configure<GoogleOAuthOptions>(...)` (now only binds ClientId); add `builder.Services.AddScoped<IGoogleCredentialVerifier, GoogleCredentialVerifier>()`
- [x] T019 Update `backend/src/FinanceSentry.Modules.Auth/API/Controllers/AuthController.cs` — remove `GoogleLogin()` and `GoogleCallback()` actions; keep the `GOOGLE_ACCOUNT_ONLY` catch in `Login()`; add `[HttpPost("google/verify")] public async Task<IActionResult> GoogleVerify([FromBody] VerifyGoogleCredentialRequest req)` action that dispatches `VerifyGoogleCredentialCommand` and returns `Ok(new AuthResponse(...))` on success or `BadRequest` on `InvalidOperationException`; add `record VerifyGoogleCredentialRequest(string Credential)` DTO (or place in Application/DTOs)
- [x] T020 Update `backend/tests/FinanceSentry.Tests.Integration/Auth/LoginContractTests.cs` (specifically `AuthApiFactory`) — remove `GoogleOAuth:ClientSecret`, `GoogleOAuth:RedirectUri`, `GoogleOAuth:FrontendUrl` settings; register a mock `IGoogleCredentialVerifier` that returns a fixed `GoogleUserInfo` for a known test credential value
- [x] T021 Update `frontend/src/app/modules/auth/services/auth.service.ts` — remove `googleLogin()` and `handleOAuthCallback()` methods; add `public verifyGoogleCredential(credential: string): Observable<AuthResponse>` that POSTs `{ credential }` to `${this.apiUrl}/google/verify` and pipes `tap(res => this.storeToken(res.token))`

**Checkpoint**: Build should pass. DB migration pending apply. No frontend Google button yet.

---

## Phase 3: User Story 1 — Google Sign-In / Sign-Up (Priority: P1) 🎯 MVP

**Goal**: Users can click "Continue with Google", pick their Google account, and be signed in or auto-registered.

**Independent Test**: Click "Continue with Google" on the login page with a new Gmail account → new Finance Sentry account created, lands on `/accounts`. Repeat with same account → signs back in to same account.

### Tests (TDD — write first, verify they fail, then implement T023)

- [x] T022 [P] [US1] Create `backend/tests/FinanceSentry.Tests.Integration/Auth/GoogleVerifyContractTests.cs` — 6 contract tests using mocked `IGoogleCredentialVerifier` via `AuthApiFactory`:
  1. `POST /google/verify` with valid credential → `200 OK` + `{ token, userId, expiresAt }`
  2. `POST /google/verify` with empty body → `400 Bad Request`
  3. `POST /google/verify` where verifier throws `InvalidOperationException` → `400 Bad Request`
  4. Valid credential for a new user (no existing account) → `200 OK`, user created in DB
  5. Valid credential for existing Google user → `200 OK`, same user returned
  6. Valid credential where email matches existing email/password account → `200 OK`, accounts linked (GoogleId set)

### Implementation

- [x] T023 [US1] Create `backend/src/FinanceSentry.Modules.Auth/Application/Commands/VerifyGoogleCredentialCommandHandler.cs` — handles `VerifyGoogleCredentialCommand`; calls `IGoogleCredentialVerifier.VerifyAsync(command.Credential)`; finds user by `GoogleId` first, then by `Email`; if found by email but `GoogleId` is null, sets `GoogleId` (linking); if not found, creates user via `UserManager` with no password; issues JWT + refresh token via existing `ITokenService` + `IRefreshTokenService`; returns `AuthResult`
- [x] T024 [US1] Update `frontend/src/app/modules/auth/pages/login/login.component.ts` — import `NgZone`; add `@ViewChild('googleBtn') private readonly googleBtnRef!: ElementRef`; in `ngOnInit` call `google.accounts.id.initialize({ client_id: environment.googleClientId, callback: (r) => this.zone.run(() => this.onGoogleCredential(r)) })` then `google.accounts.id.renderButton(this.googleBtnRef.nativeElement, { type: 'standard', shape: 'rectangular', theme: 'outline', text: 'continue_with', size: 'large', width: 368 })` then `google.accounts.id.prompt()`; add `private onGoogleCredential(response: { credential: string }): void` that calls `this.authService.verifyGoogleCredential(response.credential).subscribe(...)` to store token and navigate; add `ngOnDestroy` that calls `google.accounts.id.cancel()`
- [x] T025 [US1] Update `frontend/src/app/modules/auth/pages/login/login.component.html` — replace `<button type="button" class="google-btn" (click)="googleLogin()">...</button>` with `<div #googleBtn class="google-btn-container"></div>`; update SCSS to remove `.google-btn` + `.google-icon` styles; add `.google-btn-container { width: 100%; margin-bottom: 1.5rem; display: flex; justify-content: center; }`
- [x] T026 [US1] Update `frontend/src/app/modules/auth/pages/register/register.component.ts` — same GSI pattern as `login.component.ts` (T024): `@ViewChild`, `ngOnInit` initialize + renderButton + prompt, `onGoogleCredential` callback, `ngOnDestroy` cancel
- [x] T027 [US1] Update `frontend/src/app/modules/auth/pages/register/register.component.html` and `.scss` — same `<div #googleBtn>` pattern as T025

**Checkpoint**: Full Google sign-in / sign-up flow works. Email/password flow unchanged.

---

## Phase 4: User Story 2 — Email/Password Preserved (Priority: P2)

**Goal**: Verify no regression in email/password auth and clean up version numbers.

**Independent Test**: Register with email/password, log out, log back in — full flow works. Google-only user attempting email/password login sees `GOOGLE_ACCOUNT_ONLY` error.

- [x] T028 [P] [US2] Bump `frontend/package.json` version `0.4.0` → `0.5.0`
- [x] T029 [P] [US2] Bump `backend/src/FinanceSentry.API/FinanceSentry.API.csproj` version `0.3.0` → `0.4.0` (MINOR bump: endpoint added + endpoints removed)

**Checkpoint**: Version numbers updated. All user stories functional.

---

## Phase 5: Polish

- [x] T030 Run `npx eslint --fix` on all modified Angular TypeScript files (`auth.service.ts`, `login.component.ts`, `register.component.ts`) from `frontend/` directory; fix any remaining errors

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Cleanup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Can start in parallel with Phase 1 for files not being deleted
- **Phase 3 (US1)**: BLOCKED on Phase 2 completion
- **Phase 4 (US2)**: BLOCKED on Phase 3 completion (version bump after feature is working)
- **Phase 5 (Polish)**: After Phase 3

### User Story Dependencies

- **US1 (P1)**: Core delivery — depends on Phases 1 + 2
- **US2 (P2)**: GOOGLE_ACCOUNT_ONLY guard already implemented (LoginCommandHandler unchanged); only version bumps needed
- **US3 (P3 — One Tap)**: Satisfied by T024/T026 (`google.accounts.id.prompt()` call in ngOnInit)

### Parallel Opportunities

- T002–T007 (deletions): all parallel within Phase 1
- T009–T013 (new deps + interface): parallel within Phase 2 (different files)
- T022 (contract tests): parallel with T023 (write tests first, implement after)
- T028–T029 (version bumps): parallel

---

## Implementation Strategy

### MVP (Phase 1 → 2 → 3)

1. Phase 1: Delete old OAuth code
2. Phase 2: Build new GSI infrastructure
3. Phase 3: Wire up the verify endpoint + frontend GSI button
4. **Validate**: Click "Continue with Google" → Google account picker → backend verifies → JWT issued → lands on `/accounts`

### Incremental Delivery

- After T021: Backend verify endpoint fully functional (testable with `curl -d '{"credential":"..."}'`)
- After T023: Handler complete — all 6 contract tests pass
- After T025: Login page shows official Google button, sign-in works
- After T027: Register page same

---

## Notes

- [P] = parallelizable (different files, no shared state)
- [USn] = maps to user story n from spec.md
- `JwtAuthenticationMiddleware` already exempts all of `/api/v1/auth` — no changes needed
- `GOOGLE_ACCOUNT_ONLY` guard in `LoginCommandHandler` is already committed and stays unchanged
- M008 migration stays in history; M009 is the inverse (drop table only)
- `AuthDbContextModelSnapshot.cs` must be updated alongside M009 or EF will error on migration scaffolding

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
