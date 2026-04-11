# Implementation Plan: Auth Flow

**Branch**: `003-auth-flow` | **Date**: 2026-04-11 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `specs/003-auth-flow/spec.md`

## Summary

Build end-to-end authentication for Finance Sentry. The backend gains a new `FinanceSentry.Modules.Auth` modular project powered by ASP.NET Core Identity — providing `/auth/register` and `/auth/login` endpoints, a custom JWT generation service, and a migration for the Identity schema (AspNetUsers + family). All existing BankSync controllers are updated to read the authenticated user's identity from JWT claims instead of query parameters, closing the current security gap. The frontend gains an auth module with login/register pages, an `AuthService` backed by localStorage, a function-based HTTP interceptor that attaches `Bearer` tokens to every API request and redirects to `/login` on 401, and function-based route guards protecting all existing pages. Both frontend and backend versions are bumped (MINOR) and GitHub tags created post-merge.

## Technical Context

**Language/Version**: C# 13 / .NET 9 (backend) · TypeScript 5.x strict (frontend)  
**Primary Dependencies**: ASP.NET Core 9, EF Core 9, MediatR, ASP.NET Core Identity (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`), Npgsql.EF Core (backend) · Angular 20, RxJS, Angular standalone routing (frontend)  
**Storage**: PostgreSQL 14 — shared database, separate `AuthDbContext : IdentityDbContext<ApplicationUser>` with independent migrations  
**Testing**: xUnit + Moq (backend) · Jasmine/Karma (frontend)  
**Target Platform**: Docker Compose (full stack) + local `ng serve` for frontend iteration  
**Project Type**: Modular monolith (backend) + Angular SPA (frontend)  
**Performance Goals**: Login / register < 500ms P95 (single-user, no concurrency concern)  
**Constraints**: Token expiry 60 min (env-configurable); localStorage for token; no refresh tokens in this iteration; app not public-facing  
**Scale/Scope**: Single user; ~5 new backend files; ~10 new frontend files; 1 EF migration

## Constitution Check

*GATE: Must pass before implementation begins. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I — Modular Monolith | ✅ PASS | New `FinanceSentry.Modules.Auth` project; `ITokenService` interface in domain; `JwtTokenService` concrete in Infrastructure; no coupling to BankSync module |
| II — Code Quality | ✅ PASS | Backend: zero-warning build, StyleCop; Frontend: strict TypeScript, Angular linting |
| III — Multi-Source Integration | N/A | Feature does not touch financial data sync |
| IV — AI Analytics | N/A | Feature does not touch analytics |
| V — Security-First | ✅ PASS | Passwords hashed via Identity (PBKDF2); JWT secret from env var; secrets never logged; user data scoped by claim in all controllers; no plain-text credential storage |
| Testing Discipline | ✅ PASS | Contract tests required for POST /auth/login and POST /auth/register; unit tests for command handlers; >80% coverage target |
| Versioning | ✅ PASS | Frontend: 0.1.0 → 0.2.0 (MINOR — new auth module, interceptor, guards); Backend: 0.1.0 → 0.2.0 (MINOR — new auth endpoints); tags `frontend-v0.2.0` and `backend-v0.2.0` after merge |
| Branching | ✅ PASS | Per-task sub-branches from `003-auth-flow`; each task gets its own branch (e.g., `T301-users-schema`, `T302-auth-endpoints`), PR to `003-auth-flow`, merged and deleted |

**No violations.** Complexity Tracking section omitted.

## Project Structure

### Documentation (this feature)

```text
specs/003-auth-flow/
├── plan.md              ← this file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── contracts/
│   └── auth-api.md      ← Phase 1 output
└── tasks.md             ← Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/
  src/
    FinanceSentry.Modules.Auth/           ← NEW project
      API/
        Controllers/
          AuthController.cs               ← POST /api/v1/auth/login, /register
      Application/
        Commands/
          LoginCommand.cs
          LoginCommandHandler.cs
          RegisterCommand.cs
          RegisterCommandHandler.cs
        Interfaces/
          ITokenService.cs                ← domain interface (Principle I)
        DTOs/
          AuthRequest.cs                  ← shared login/register request shape
          AuthResponse.cs                 ← { token, expiresAt, userId }
      Domain/
        Entities/
          ApplicationUser.cs              ← extends IdentityUser
      Infrastructure/
        Persistence/
          AuthDbContext.cs                ← extends IdentityDbContext<ApplicationUser>
          Migrations/
            M005_IdentitySchema.cs        ← AspNetUsers + family
        Services/
          JwtTokenService.cs              ← implements ITokenService
      FinanceSentry.Modules.Auth.csproj
    FinanceSentry.API/
      Program.cs                          ← register Auth module (Identity, AuthDbContext, handlers, controller)

  ← MODIFIED (breaking change — userId from JWT claims):
    FinanceSentry.Modules.BankSync/
      API/
        Controllers/
          AccountsController.cs           ← remove [FromQuery] Guid userId, read from User claims
          (other controllers if they also accept userId as query param)
      API/
        Middleware/
          JwtAuthenticationMiddleware.cs  ← populate HttpContext.User with ClaimsPrincipal

frontend/
  src/app/
    modules/
      auth/                               ← NEW lazy-loaded module
        auth.routes.ts
        pages/
          login/
            login.component.ts
            login.component.html
            login.component.scss
          register/
            register.component.ts
            register.component.html
            register.component.scss
        services/
          auth.service.ts                 ← token storage, login/register calls, logout, isAuthenticated
        guards/
          auth.guard.ts                   ← CanActivateFn; redirect to /login if no token
          guest.guard.ts                  ← CanActivateFn; redirect to /accounts if already authenticated
        interceptors/
          auth.interceptor.ts             ← HttpInterceptorFn; attach Bearer; redirect on 401
        models/
          auth.models.ts                  ← AuthRequest, AuthResponse interfaces
    app.routes.ts                         ← add /login, /register routes; apply authGuard to /accounts, /dashboard
    app.config.ts                         ← register withInterceptors([authInterceptor])
```

**Structure Decision**: Web application layout (backend + frontend). New backend module follows the exact same folder/layer pattern as `FinanceSentry.Modules.BankSync`. Frontend auth module is lazy-loaded and uses Angular 20 standalone standalone components + function-based guards and interceptors.
