# Implementation Plan: Google Sign-In via Identity Services

**Branch**: `004-adopt-oauth` | **Date**: 2026-04-18 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-adopt-oauth/spec.md`

## Summary

Replace the existing server-side Google OAuth Authorization Code flow with Google Identity Services (GSI) client-side credential verification. The frontend receives a signed Google ID token directly; the backend verifies it cryptographically using `Google.Apis.Auth`. This eliminates the `OAuthState` CSRF nonce table, the backend-to-Google token exchange HTTP call, and the full-page redirect. One Tap sign-in is included as part of the GSI integration. All existing email/password flows and JWT issuance remain unchanged.

## Technical Context

**Language/Version**: C# 13 / .NET 9 (backend) · TypeScript 5.x strict (frontend)
**Primary Dependencies**: ASP.NET Core 9, EF Core 9, MediatR, ASP.NET Core Identity, `Google.Apis.Auth` (new) · Angular 20, RxJS, `@types/google.accounts` (new)
**Storage**: PostgreSQL 14 — `AuthDbContext : IdentityDbContext<ApplicationUser>` — `OAuthStates` table to be DROPPED via new migration
**Testing**: xUnit + `WebApplicationFactory` (backend contract tests) · ESLint (frontend)
**Target Platform**: Docker Compose (dev) — API on port 5050, frontend on port 4200/4201
**Project Type**: Modular monolith (backend) + Angular SPA (frontend)
**Performance Goals**: Google credential verification < 500ms p95 (network call to Google JWKS endpoint is cached by library)
**Constraints**: `Google.Apis.Auth` must validate `aud` == configured client ID; no Google client secret needed by backend
**Scale/Scope**: Single-user app (Denys); no concurrent-user scaling concern for this feature

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Modular Monolith | ✅ PASS | GSI verification stays in `FinanceSentry.Modules.Auth`; no cross-module coupling |
| I. Domain Interface | ✅ PASS | `IGoogleCredentialVerifier` interface defined in Application layer; concrete in Infrastructure |
| II. Code Quality | ✅ PASS | ESLint enforced on every Angular file; StyleCop on backend |
| III. Multi-Source | N/A | Not a financial integration |
| IV. AI Analytics | N/A | Not in scope |
| V. Security-First | ✅ PASS | `Google.Apis.Auth` validates signature, `aud`, `iss`, and `exp`; no token stored; JWT issued per existing pattern |
| Branching | ✅ PASS | Continuing on `004-adopt-oauth`; per-task commits maintained |
| Versioning | ✅ PASS | Backend MINOR bump (new endpoint, removed endpoints) · Frontend MINOR bump (GSI integration) |
| Testing | ✅ PASS | New `POST /auth/google/verify` contract test required; old Google contract tests replaced |

## Project Structure

### Documentation (this feature)

```text
specs/004-adopt-oauth/
├── plan.md              ← this file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── contracts/           ← Phase 1 output
│   └── auth-api.md
└── tasks.md             ← Phase 2 output (/speckit.tasks)
```

### Source Code Changes

```text
backend/
  src/
    FinanceSentry.Modules.Auth/
      Application/
        Commands/
          [DELETE] HandleGoogleCallbackCommand.cs
          [DELETE] HandleGoogleCallbackCommandHandler.cs
          [ADD]    VerifyGoogleCredentialCommand.cs
          [ADD]    VerifyGoogleCredentialCommandHandler.cs
        Interfaces/
          [DELETE] IGoogleOAuthService.cs
          [ADD]    IGoogleCredentialVerifier.cs
        Queries/
          [DELETE] InitiateGoogleLoginQuery.cs
          [DELETE] InitiateGoogleLoginQueryHandler.cs
      Domain/
        Entities/
          [DELETE] OAuthState.cs
          [KEEP]   ApplicationUser.cs  (GoogleId property remains)
      Infrastructure/
        Persistence/
          [MODIFY] AuthDbContext.cs  (remove OAuthStates DbSet + config)
          [ADD]    Migration: M009_DropOAuthStates
        Services/
          [DELETE] GoogleOAuthOptions.cs
          [DELETE] GoogleOAuthService.cs
          [ADD]    GoogleCredentialVerifier.cs  (uses Google.Apis.Auth)
      API/Controllers/
          [MODIFY] AuthController.cs  (remove /google/login + /callback; add POST /google/verify)
    FinanceSentry.API/
      [MODIFY] Program.cs  (remove GoogleOAuthOptions, HttpClient("google"), GoogleOAuthService; add GoogleCredentialVerifier)
      [MODIFY] appsettings.json  (remove GoogleOAuth section; add GoogleOAuth:ClientId only)
      [MODIFY] FinanceSentry.API.csproj  (version bump 0.3.0 → 0.4.0)

  tests/
    FinanceSentry.Tests.Integration/
      Auth/
        [DELETE] GoogleOAuthContractTests.cs  (old redirect tests)
        [ADD]    GoogleVerifyContractTests.cs  (new verify endpoint tests)
        [MODIFY] LoginContractTests.cs  (remove Google redirect config; add ClientId config)

frontend/
  src/
    app/
      modules/auth/
        pages/
          [DELETE] oauth-callback/  (entire directory)
          [MODIFY] login/login.component.ts  (remove handleOAuthCallback; add GSI init + One Tap)
          [MODIFY] login/login.component.html  (replace custom google-btn with GSI div target)
          [MODIFY] register/register.component.ts  (same GSI pattern)
          [MODIFY] register/register.component.html
        services/
          [MODIFY] auth.service.ts  (replace googleLogin/handleOAuthCallback with verifyGoogleCredential)
      app.routes.ts  [MODIFY]  (remove /auth/callback route)
    index.html  [MODIFY]  (add GSI script tag)
  package.json  [MODIFY]  (version bump; add @types/google.accounts dev dependency)

docker/
  docker-compose.dev.yml  [MODIFY]  (remove GOOGLEOAUTH__CLIENTSECRET + REDIRECTURI env vars; keep CLIENTID + FRONTENDURL)
```
