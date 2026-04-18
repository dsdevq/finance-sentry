# Implementation Plan: Google OAuth Sign-In

**Branch**: `004-adopt-oauth` | **Date**: 2026-04-18 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/004-adopt-oauth/spec.md`

---

## Summary

Add Google OAuth 2.0 Authorization Code (server-side) sign-in alongside the existing email/password flow. The backend owns the full OAuth dance — Angular redirects to `/api/v1/auth/google/login`, Google calls back to `/api/v1/auth/google/callback`, and the backend issues the same JWT + httpOnly refresh cookie as email/password login, then redirects to `/auth/callback?token=<jwt>`. Existing auth (003-auth-flow) is fully preserved.

---

## Technical Context

**Language/Version**: C# 13 / .NET 9 (backend); TypeScript 5.x strict / Angular 20+ (frontend)
**Primary Dependencies**: ASP.NET Core 9, EF Core 9 + Npgsql, MediatR 12, ASP.NET Identity (backend); Angular 20 standalone components, RxJS (frontend). No new packages needed — `IHttpClientFactory` handles Google HTTP calls.
**Storage**: PostgreSQL 14 via `AuthDbContext` — two schema changes: `GoogleId` column on `AspNetUsers`, new `OAuthStates` table (migrations M007 + M008).
**Testing**: xUnit + `WebApplicationFactory` for backend contract/integration tests; Vitest for frontend unit tests.
**Target Platform**: Docker Compose (dev), Linux container (prod)
**Project Type**: Web service (ASP.NET) + SPA (Angular)
**Performance Goals**: Standard auth latency — Google roundtrip adds ~200–500ms to sign-in; not a bottleneck for a personal app.
**Constraints**: Google credentials must NOT appear in version-controlled files. Redirect URI must be registered in Google Cloud Console.
**Scale/Scope**: Single user; low volume. OAuthState cleanup via Hangfire deferred to future task.

---

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Modular Monolith | ✅ PASS | `IGoogleOAuthService` interface defined; concrete in Infrastructure; never referenced directly from business logic |
| II. Code Quality | ✅ PASS | ESLint enforced on all Angular files; zero-warning .NET build |
| III. Multi-Source Integration | N/A | Not a financial data integration |
| IV. AI Analytics | N/A | Not in scope |
| V. Security-First | ✅ PASS | Credentials in env vars only; state CSRF protection; httpOnly cookie; no tokens in logs; `GoogleId` match before email match |
| Branching | ✅ PASS | Single feature branch `004-adopt-oauth` |
| Versioning | ✅ PASS | Backend `0.2.0 → 0.3.0` (new endpoints); Frontend `0.3.0 → 0.4.0` (new components) — T020 |
| Contract Tests | ✅ PASS | `GoogleOAuthContractTests.cs` covers both new endpoints — T009 |
| Test-First | ✅ PASS | T009 (contract tests) written before T010–T013 (implementation) per task order |

**Constitution version referenced**: 1.2.1

---

## Project Structure

### Documentation (this feature)

```text
specs/004-adopt-oauth/
├── plan.md          # This file
├── spec.md          # Feature specification
├── research.md      # Phase 0 — OAuth flow + account linking decisions
├── data-model.md    # Phase 1 — ApplicationUser + OAuthState schema
├── quickstart.md    # Phase 1 — Local testing guide
├── contracts/
│   └── google-oauth.md  # REST contract for /auth/google/login + /callback
└── tasks.md         # Implementation tasks (21 tasks across 5 phases)
```

### Source Code (affected paths)

```text
backend/
  src/
    FinanceSentry.Modules.Auth/
      Domain/Entities/
        ApplicationUser.cs              # + GoogleId : string?
        OAuthState.cs                   # NEW
      Application/
        Interfaces/
          IGoogleOAuthService.cs        # NEW
        Queries/
          InitiateGoogleLoginQuery.cs   # NEW
          InitiateGoogleLoginQueryHandler.cs  # NEW
        Commands/
          HandleGoogleCallbackCommand.cs      # NEW
          HandleGoogleCallbackCommandHandler.cs  # NEW
          LoginCommandHandler.cs        # MODIFIED (+GOOGLE_ACCOUNT_ONLY guard)
      Infrastructure/
        Persistence/
          AuthDbContext.cs              # + OAuthStates DbSet
          Migrations/
            M007_GoogleId.*            # NEW
            M008_GoogleOAuth.*         # NEW
        Services/
          GoogleOAuthOptions.cs        # NEW
          GoogleOAuthService.cs        # NEW
      API/Controllers/
        AuthController.cs              # + /google/login + /google/callback actions
    FinanceSentry.API/
      Program.cs                       # + GoogleOAuth DI registrations
      appsettings.json                 # + "GoogleOAuth" section (empty defaults)

  tests/
    FinanceSentry.Tests.Integration/Auth/
      GoogleOAuthContractTests.cs      # NEW

docker/
  docker-compose.dev.yml               # + GOOGLEOAUTH__ env vars

frontend/src/app/modules/auth/
  services/auth.service.ts             # + googleLogin() + handleOAuthCallback()
  pages/
    oauth-callback/                    # NEW — OAuthCallbackComponent
    login/                             # MODIFIED — Google button + error messages
    register/                          # MODIFIED — Google button
  auth.routes.ts                       # + /auth/google/callback route
```

**Structure Decision**: Web application (backend + frontend). All OAuth backend logic lives in `FinanceSentry.Modules.Auth` following the existing modular monolith pattern.

---

## Phase 0: Research

See [research.md](research.md). All decisions resolved:
- Authorization Code flow (server-side) — no new packages
- CSRF via server-side `OAuthState` entity
- Account linking: `GoogleId` match first, email fallback
- Frontend: query-param callback pattern reusing existing `AuthService` token storage

---

## Phase 1: Design & Contracts

- **Data model**: [data-model.md](data-model.md) — `ApplicationUser` + `GoogleId`, new `OAuthState` entity
- **REST contracts**: [contracts/google-oauth.md](contracts/google-oauth.md) — GET /auth/google/login + /callback
- **Quickstart**: [quickstart.md](quickstart.md) — local setup, env vars, test flows
