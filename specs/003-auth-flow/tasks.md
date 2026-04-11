# Tasks: Auth Flow (003-auth-flow)

**Input**: Design documents from `specs/003-auth-flow/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/auth-api.md ✅, quickstart.md ✅

**Tests**: Per constitution v1.2.0 — contract tests (mandatory for all new/modified endpoints), unit tests (>80% coverage). No E2E tests requested.

**Organization**: Tasks grouped by user story. Each story is independently implementable and testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Parallelizable — different files, no dependency on an incomplete sibling task
- **[Story]**: US1–US5 map to spec.md user stories

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Scaffold the new Auth module project and wire it into the solution before any feature work begins.

- [X] T001 Create `backend/src/FinanceSentry.Modules.Auth/FinanceSentry.Modules.Auth.csproj` with references to `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore`, `Microsoft.IdentityModel.Tokens`, `System.IdentityModel.Tokens.Jwt`, MediatR; add project reference from `FinanceSentry.API.csproj`
- [X] T002 [P] Create folder skeleton for Auth module: `API/Controllers/`, `Application/Commands/`, `Application/Interfaces/`, `Application/DTOs/`, `Domain/Entities/`, `Infrastructure/Persistence/Migrations/`, `Infrastructure/Services/` under `backend/src/FinanceSentry.Modules.Auth/`
- [X] T003 [P] Create `frontend/src/app/modules/auth/` folder skeleton: `pages/login/`, `pages/register/`, `services/`, `guards/`, `interceptors/`, `models/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain model, persistence, token service, middleware, and shared TypeScript interfaces — everything user stories build on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 Create `ApplicationUser.cs` extending `IdentityUser` (no custom fields in v1) in `backend/src/FinanceSentry.Modules.Auth/Domain/Entities/ApplicationUser.cs`
- [X] T005 [P] Create `AuthDbContext.cs` extending `IdentityDbContext<ApplicationUser>`, accepting `DbContextOptions<AuthDbContext>`, using the shared `DefaultConnection` connection string in `backend/src/FinanceSentry.Modules.Auth/Infrastructure/Persistence/AuthDbContext.cs`
- [X] T006 [P] Create `ITokenService.cs` interface (`string GenerateToken(ApplicationUser user)` returning a signed HS256 JWT) in `backend/src/FinanceSentry.Modules.Auth/Application/Interfaces/ITokenService.cs`
- [X] T007 [P] Create `AuthRequest.cs` (Email, Password) and `AuthResponse.cs` (Token, ExpiresAt, UserId) DTOs in `backend/src/FinanceSentry.Modules.Auth/Application/DTOs/`
- [X] T008 Create `JwtTokenService.cs` implementing `ITokenService` — reads `JWT_SECRET` and `JWT_EXPIRY_MINUTES` from `IConfiguration`, generates HS256 JWT with `sub`, `email`, `exp`, `iat` claims in `backend/src/FinanceSentry.Modules.Auth/Infrastructure/Services/JwtTokenService.cs`
- [X] T009 Run EF migration: add `M005_IdentitySchema` to `AuthDbContext` via `dotnet ef migrations add M005_IdentitySchema --project FinanceSentry.Modules.Auth --context AuthDbContext` — verify migration files created under `Infrastructure/Persistence/Migrations/`
- [X] T010 Register Auth module in `backend/src/FinanceSentry.API/Program.cs`: `AddIdentity<ApplicationUser, IdentityRole>`, `AddEntityFrameworkStores<AuthDbContext>`, `AuthDbContext` with Npgsql, `ITokenService`→`JwtTokenService` singleton, MediatR handlers from Auth assembly, Auth controller in `AddControllers`
- [X] T011 Update `backend/src/FinanceSentry.Modules.BankSync/API/Middleware/JwtAuthenticationMiddleware.cs` to populate `HttpContext.User` with a `ClaimsPrincipal` (set `sub`, `email` claims from validated JWT payload) so controllers can call `User.FindFirst(ClaimTypes.NameIdentifier)`
- [X] T012 [P] Create `auth.models.ts` with TypeScript interfaces `AuthRequest { email, password }` and `AuthResponse { token, expiresAt, userId }` in `frontend/src/app/modules/auth/models/auth.models.ts`

**Checkpoint**: Foundation complete — all user story phases can begin.

---

## Phase 3: User Story 1 — Login (Priority: P1) 🎯 MVP

**Goal**: Backend login endpoint + frontend login page + basic token storage. After this phase a registered user can authenticate and land on the accounts page with real data.

**Independent Test**: POST `http://localhost:5000/api/v1/auth/login` with valid credentials → 200 + JWT; open `http://localhost:4200/login`, submit credentials → redirect to `/accounts` with data loading.

### Contract Tests for US1

- [X] T013 [P] [US1] Contract test — `POST /auth/login` happy path (200 + `AuthResponse` schema), wrong password (401 + `INVALID_CREDENTIALS`), missing fields (400 + `VALIDATION_ERROR`) in `backend/tests/FinanceSentry.Tests.Integration/Auth/LoginContractTests.cs`

### Implementation for US1

- [X] T014 [US1] Create `LoginCommand.cs` (Email, Password) and `LoginCommandHandler.cs` (uses `UserManager<ApplicationUser>` + `ITokenService`; returns `AuthResponse` or throws on bad credentials) in `backend/src/FinanceSentry.Modules.Auth/Application/Commands/`
- [X] T015 [US1] Create `AuthController.cs` with `POST /api/v1/auth/login` action (dispatches `LoginCommand` via MediatR; returns 200 with `AuthResponse` or 401 with `INVALID_CREDENTIALS`) in `backend/src/FinanceSentry.Modules.Auth/API/Controllers/AuthController.cs`; add `/api/v1/auth` route prefix to `JwtAuthenticationMiddleware` exempt list in `Program.cs`
- [X] T016 [P] [US1] Create `login.component.ts` (standalone, reactive form — email + password fields, submit handler calls `AuthService.login()`, error display), `login.component.html`, `login.component.scss` in `frontend/src/app/modules/auth/pages/login/`
- [X] T017 [US1] Create `auth.service.ts` with `login(req: AuthRequest): Observable<AuthResponse>`, `storeToken(token: string): void` (localStorage key `fs_auth_token`), `getToken(): string | null` in `frontend/src/app/modules/auth/services/auth.service.ts`
- [X] T018 [US1] Add `/login` route (lazy-load auth module) to `frontend/src/app/app.routes.ts`; on successful login `AuthService` navigates to `/accounts`

**Checkpoint**: User can log in via the UI; accounts page shows data instead of 401 errors.

---

## Phase 4: User Story 2 — Register (Priority: P2)

**Goal**: Backend register endpoint + frontend register page. New users can create an account and are immediately logged in.

**Independent Test**: POST `http://localhost:5000/api/v1/auth/register` with fresh email → 201 + JWT; open `http://localhost:4200/register`, submit new credentials → authenticated and redirected to `/accounts`.

### Contract Tests for US2

- [X] T019 [P] [US2] Contract test — `POST /auth/register` happy path (201 + `AuthResponse` schema), duplicate email (400 + `DUPLICATE_EMAIL`), missing fields (400 + `VALIDATION_ERROR`) in `backend/tests/FinanceSentry.Tests.Integration/Auth/RegisterContractTests.cs`

### Implementation for US2

- [X] T020 [US2] Create `RegisterCommand.cs` (Email, Password) and `RegisterCommandHandler.cs` (uses `UserManager.CreateAsync`; on success generates token via `ITokenService`; returns `AuthResponse` or throws on duplicate email) in `backend/src/FinanceSentry.Modules.Auth/Application/Commands/`
- [X] T021 [US2] Add `POST /api/v1/auth/register` action to `AuthController.cs` (dispatches `RegisterCommand`; returns 201 with `AuthResponse` or 400 with `DUPLICATE_EMAIL`) in `backend/src/FinanceSentry.Modules.Auth/API/Controllers/AuthController.cs`
- [X] T022 [P] [US2] Create `register.component.ts` (standalone, reactive form — email + password + confirm-password fields, submit handler calls `AuthService.register()`, error display), `register.component.html`, `register.component.scss` in `frontend/src/app/modules/auth/pages/register/`
- [X] T023 [US2] Add `register(req: AuthRequest): Observable<AuthResponse>` to `auth.service.ts`; on success store token and navigate to `/accounts` in `frontend/src/app/modules/auth/services/auth.service.ts`
- [X] T024 [US2] Add `/register` route to `frontend/src/app/app.routes.ts`

**Checkpoint**: New users can register and land on the accounts page authenticated.

---

## Phase 5: User Story 3 — Session Persistence (Priority: P3)

**Goal**: Token survives page refresh and browser restart; expiry is checked on load; logout clears the session.

**Independent Test**: Log in → close browser tab → reopen `http://localhost:4200/accounts` → still authenticated and data loads. Log out → navigate to `/accounts` → redirected to `/login`.

### Implementation for US3

- [X] T025 [US3] Add `isAuthenticated(): boolean` (checks token exists and `exp` claim > `Date.now()`), `isTokenExpired(): boolean`, `logout(): void` (removes `fs_auth_token` from localStorage, navigates to `/login`) to `auth.service.ts` in `frontend/src/app/modules/auth/services/auth.service.ts`
- [X] T026 [US3] In `auth.service.ts` constructor, read token from localStorage on instantiation — if present and expired, call `logout()` immediately; expose `currentUser$: BehaviorSubject<AuthResponse | null>` derived from stored token in `frontend/src/app/modules/auth/services/auth.service.ts`

**Checkpoint**: Session survives refresh; expired tokens are cleared on load; logout works end to end.

---

## Phase 6: User Story 4 — Automatic Token Attachment (Priority: P4)

**Goal**: Every API request carries `Authorization: Bearer <token>`; 401 responses trigger logout and redirect to `/login`; BankSync controllers source userId from JWT claims instead of query params.

**Independent Test**: Log in, navigate to `/accounts` — network tab shows `Authorization` header on every request; accounts list loads. Log out and call `/api/v1/accounts` directly — 401 returned.

### Contract Tests for US4

- [X] T027 [P] [US4] Contract test — `GET /accounts` with no token (401 + `UNAUTHORIZED`), `GET /accounts` with valid Bearer token (200, userId from claim, no `?userId` query param) in `backend/tests/FinanceSentry.Tests.Integration/Auth/AccountsAuthContractTests.cs`

### Implementation for US4

- [X] T028 [US4] Create `auth.interceptor.ts` (`HttpInterceptorFn`): read token from `AuthService.getToken()`; if present, clone request with `Authorization: Bearer <token>` header; on 401 response call `AuthService.logout()` in `frontend/src/app/modules/auth/interceptors/auth.interceptor.ts`
- [X] T029 [US4] Register `authInterceptor` via `withInterceptors([authInterceptor])` in `frontend/src/app/app.config.ts`
- [X] T030 [US4] Remove `[FromQuery] Guid userId` from `AccountsController.cs`; extract userId via `User.FindFirst(ClaimTypes.NameIdentifier)?.Value` and parse to `Guid` in `backend/src/FinanceSentry.Modules.BankSync/API/Controllers/AccountsController.cs`
- [X] T031 [US4] Remove `userId` query param from all remaining BankSync controllers that accept it (`SyncController.cs`, `DashboardController.cs`, any others) — read from JWT claim consistently in `backend/src/FinanceSentry.Modules.BankSync/API/Controllers/`

**Checkpoint**: All API calls succeed post-login; no userId in query strings anywhere.

---

## Phase 7: User Story 5 — Route Guards (Priority: P5)

**Goal**: Unauthenticated users cannot access protected pages; after login they are returned to the originally requested URL; authenticated users are bounced away from `/login` and `/register`.

**Independent Test**: Without token, paste `http://localhost:4200/accounts` in browser → redirected to `/login?returnUrl=%2Faccounts`; log in → land on `/accounts`. While logged in, navigate to `/login` → redirected to `/accounts`.

### Implementation for US5

- [X] T032 [P] [US5] Create `auth.guard.ts` (`CanActivateFn`): if `AuthService.isAuthenticated()` return `true`; else redirect to `/login` with `returnUrl` query param set to the attempted URL in `frontend/src/app/modules/auth/guards/auth.guard.ts`
- [X] T033 [P] [US5] Create `guest.guard.ts` (`CanActivateFn`): if `AuthService.isAuthenticated()` redirect to `/accounts`; else return `true` in `frontend/src/app/modules/auth/guards/guest.guard.ts`
- [X] T034 [US5] Create `auth.routes.ts` defining `/login` and `/register` child routes (both protected by `guestGuard`) in `frontend/src/app/modules/auth/auth.routes.ts`
- [X] T035 [US5] Apply `authGuard` to `/accounts` and `/dashboard` routes; apply `guestGuard` to `/login` and `/register` routes in `frontend/src/app/app.routes.ts`
- [X] T036 [US5] In `login.component.ts`, read `returnUrl` query param and pass to `AuthService` or navigate directly after successful login; update `auth.guard.ts` to encode the return URL in `frontend/src/app/modules/auth/pages/login/login.component.ts` and `frontend/src/app/modules/auth/guards/auth.guard.ts`

**Checkpoint**: Full auth boundary enforced — no protected page accessible without a valid session.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Version bumps, tags, and final validation.

- [X] T037 Bump backend version `0.1.0 → 0.2.0` in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj` `<Version>` field (MINOR — new auth endpoints)
- [X] T038 [P] Bump frontend version `0.1.0 → 0.2.0` in `frontend/package.json` `"version"` field (MINOR — new auth module, interceptor, guards)
- [X] T039 Run `quickstart.md` validation: execute curl commands for login, register, and authenticated account fetch; verify frontend auth flow end to end in browser

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — **blocks all user story phases**
- **Phase 3–7 (User Stories)**: All depend on Phase 2 completion; can proceed in priority order (P1→P5)
- **Phase 8 (Polish)**: Depends on all user story phases being complete

### User Story Dependencies

- **US1 (Login)**: Starts after Phase 2 — no story dependencies
- **US2 (Register)**: Starts after US1 — shares `AuthController.cs` and `auth.service.ts`; extends them rather than replacing
- **US3 (Session Persistence)**: Starts after US1 — extends `auth.service.ts` with expiry/logout logic
- **US4 (Token Attachment)**: Starts after US3 — depends on `getToken()` and `logout()` being in place
- **US5 (Route Guards)**: Starts after US4 — depends on `isAuthenticated()` from US3 and interceptor from US4

### Within Each Phase

- Models/DTOs before services
- Services before controllers/components
- Contract tests written before implementation (TDD)
- Story complete before moving to next priority

### Parallel Opportunities

- T002 + T003 (folder scaffolding) can run in parallel
- T005, T006, T007 (Phase 2) can run in parallel after T004
- T013 (contract test) can be written in parallel with T014–T015 implementation
- T016 (login component) can be built in parallel with T014–T015 (backend)
- T019 (register contract test) in parallel with T020–T021
- T022 (register component) in parallel with T020–T021
- T032 + T033 (guards) can be built in parallel

---

## Parallel Example: User Story 1

```
# Backend and frontend can proceed in parallel after Phase 2:
Backend:  T013 (contract test) → T014 (command/handler) → T015 (controller action)
Frontend: T016 (login component) → T017 (auth.service login) → T018 (route)

# T013, T016 can start simultaneously — different files, no shared dependency
```

---

## Implementation Strategy

### MVP (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: US1 Login
4. **STOP and VALIDATE**: curl login; open browser; confirm accounts page loads with data
5. If validated — continue to US2

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. US1 Login → first working auth flow (MVP)
3. US2 Register → new user onboarding works
4. US3 Session Persistence → session survives refresh
5. US4 Token Attachment → all API calls authenticated; BankSync breaking change closed
6. US5 Route Guards → full security boundary enforced
7. Polish → version bumps, tags, final validation

---

## Phase 9: Foundational — Refresh Token Domain

**Purpose**: Add `RefreshToken` entity, persistence, and the service layer that all refresh-flow endpoints depend on.

**⚠️ CRITICAL**: Phases 10–12 cannot begin until this phase is complete.

- [X] T040 Create `RefreshToken.cs` entity (`Id`, `UserId`, `TokenHash`, `ExpiresAt`, `CreatedAt`, `IsRevoked`) in `backend/src/FinanceSentry.Modules.Auth/Domain/Entities/RefreshToken.cs`
- [X] T041 [P] Add `DbSet<RefreshToken> RefreshTokens` to `AuthDbContext` and configure the entity (index on `TokenHash`, index on `UserId`) in `backend/src/FinanceSentry.Modules.Auth/Infrastructure/Persistence/AuthDbContext.cs`
- [X] T042 Run `dotnet ef migrations add M006_RefreshTokens --project FinanceSentry.Modules.Auth --context AuthDbContext` and verify migration files in `Infrastructure/Persistence/Migrations/`
- [X] T043 [P] Create `IRefreshTokenService.cs` interface: `IssueAsync(string userId) → (rawToken, RefreshToken)`, `ValidateAsync(string rawToken) → RefreshToken?`, `RotateAsync(RefreshToken existing) → (rawToken, RefreshToken)`, `RevokeAsync(string userId)` in `backend/src/FinanceSentry.Modules.Auth/Application/Interfaces/IRefreshTokenService.cs`
- [X] T044 Create `RefreshTokenService.cs` implementing `IRefreshTokenService`: SHA-256 hash storage, 30-day rolling expiry, rotation invalidates previous token, `RevokeAsync` marks all user tokens revoked in `backend/src/FinanceSentry.Modules.Auth/Infrastructure/Services/RefreshTokenService.cs`
- [X] T045 Register `IRefreshTokenService → RefreshTokenService` as scoped in `backend/src/FinanceSentry.API/Program.cs`

---

## Phase 10: US3 — Session Persistence (Backend Refresh & Logout Endpoints)

**Goal**: `POST /auth/refresh` issues new access + refresh tokens; `POST /auth/logout` revokes the refresh token server-side.

**Independent Test**: `POST /api/v1/auth/refresh` with valid httpOnly cookie → 200 + new JWT + new cookie; `POST /api/v1/auth/logout` → 204 + cookie cleared.

### Contract Tests for Phase 10

- [X] T046 [P] Contract test — `POST /auth/refresh` happy path (200 + `AuthResponse` schema + Set-Cookie), invalid/expired cookie (401 + `INVALID_REFRESH_TOKEN`) in `backend/tests/FinanceSentry.Tests.Integration/Auth/RefreshContractTests.cs`
- [X] T047 [P] Contract test — `POST /auth/logout` with valid token → 204 and cookie revoked in `backend/tests/FinanceSentry.Tests.Integration/Auth/LogoutContractTests.cs`

### Implementation for Phase 10

- [X] T048 Create `RefreshCommand.cs` (reads `fs_refresh_token` cookie value) and `RefreshCommandHandler.cs` (validates token, rotates, returns `AuthResponse` + new raw token) in `backend/src/FinanceSentry.Modules.Auth/Application/Commands/`
- [X] T049 Create `LogoutCommand.cs` (UserId) and `LogoutCommandHandler.cs` (calls `IRefreshTokenService.RevokeAsync`) in `backend/src/FinanceSentry.Modules.Auth/Application/Commands/`
- [X] T050 Add `POST /auth/refresh` to `AuthController.cs`: read `fs_refresh_token` cookie, dispatch `RefreshCommand`, set new httpOnly `fs_refresh_token` cookie, return 200 with `AuthResponse`; return 401 on failure in `backend/src/FinanceSentry.Modules.Auth/API/Controllers/AuthController.cs`
- [X] T051 Add `POST /auth/logout` to `AuthController.cs`: dispatch `LogoutCommand`, delete `fs_refresh_token` cookie, return 204 in `backend/src/FinanceSentry.Modules.Auth/API/Controllers/AuthController.cs`

---

## Phase 11: US1/US2 — Login & Register Issue Refresh Token Cookie

**Goal**: Successful login and register responses also set the `fs_refresh_token` httpOnly cookie.

- [X] T052 Update `LoginCommandHandler.cs` to also call `IRefreshTokenService.IssueAsync()` and return the raw refresh token alongside `AuthResponse` in `backend/src/FinanceSentry.Modules.Auth/Application/Commands/LoginCommandHandler.cs`; create `AuthResult.cs` DTO wrapping `AuthResponse` + `RawRefreshToken` in `Application/DTOs/`
- [X] T053 Update `RegisterCommandHandler.cs` same as T052 in `backend/src/FinanceSentry.Modules.Auth/Application/Commands/RegisterCommandHandler.cs`
- [X] T054 Update `AuthController.cs` Login and Register actions to read `RawRefreshToken` from `AuthResult` and set `fs_refresh_token` as an httpOnly, SameSite=Strict, Secure cookie via `Response.Cookies.Append` in `backend/src/FinanceSentry.Modules.Auth/API/Controllers/AuthController.cs`

---

## Phase 12: Frontend — Interceptor Refresh & Service Updates

**Goal**: On 401, the interceptor silently refreshes the access token and retries once. Logout calls the server endpoint.

- [ ] T055 Add `refresh(): Observable<AuthResponse>` to `auth.service.ts` that calls `POST /api/v1/auth/refresh` (no body — browser sends the httpOnly cookie automatically); on success stores the new access token in `frontend/src/app/modules/auth/services/auth.service.ts`
- [ ] T056 Update `logout(): void` in `auth.service.ts` to call `POST /api/v1/auth/logout` before clearing localStorage and navigating to `/login` in `frontend/src/app/modules/auth/services/auth.service.ts`
- [ ] T057 Update `auth.interceptor.ts`: on `HttpErrorResponse` 401, call `authService.refresh()`, clone and retry the original request with the new token exactly once; if refresh fails (error or another 401) call `authService.logout()` instead of logging out immediately in `frontend/src/app/modules/auth/interceptors/auth.interceptor.ts`

---

## Notes

- [P] tasks = different files, no dependency on an incomplete sibling
- `AuthController.cs` is extended across US1 (login action) and US2 (register action) — implement login action in T015, add register action in T021
- `auth.service.ts` is extended across US1, US2, and US3 — each phase adds methods, does not replace the file
- `app.routes.ts` is touched in T018 (US1), T024 (US2), and T035 (US5) — coordinate to avoid conflicts
- BankSync breaking change (T030–T031) must land in the same PR as the interceptor (T028–T029) so the app never enters a broken state mid-feature
- After merge to `003-auth-flow` parent branch, create tags `backend-v0.2.0` and `frontend-v0.2.0` per constitution
