# Finance Sentry — Claude Context

## Project Overview

Finance Sentry is a personal finance aggregation app built as an ASP.NET Core 9 modular monolith + Angular 20 SPA. It integrates with Plaid for bank data, with plans for Interactive Brokers, Binance, and AI-driven portfolio analytics.

Sole developer: Denys. Spec-driven development via the **speckit** toolchain (constitution → spec → plan → tasks → implement).

---

## Governance

Project principles are in [`.specify/memory/constitution.md`](.specify/memory/constitution.md) (v1.1.1).

Key non-negotiables:
- Every external integration MUST be behind a domain interface (`IBankProvider`, `IBrokerAdapter`, etc.) — no concrete adapters referenced directly in modules
- Strict code quality: zero-warning builds, StyleCop, ESLint/Prettier, Angular strict mode
- Security-first: all financial data encrypted at rest and in transit, user data isolation absolute
- Per-task feature branching mandatory; each task gets its own branch and PR
- Contract tests mandatory for every external API integration AND every REST endpoint

---

## Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 9, EF Core, PostgreSQL 14, MediatR (CQRS), Hangfire, Serilog |
| Frontend | Angular 20+, TypeScript strict, RxJS, lazy-loaded modules |
| Auth | Custom `JwtAuthenticationMiddleware` — no ASP.NET Identity |
| Infra | Docker Compose (single file for full stack) |

---

## How to Run

Everything runs in Docker:

```bash
cd docker
docker compose -f docker-compose.dev.yml up -d --build
```

Startup order enforced by health checks: `postgres → api → frontend`

| Service | URL |
|---|---|
| Frontend (Angular) | http://localhost:4200 |
| Backend API | http://localhost:5000/api/v1 |
| Health check | http://localhost:5000/api/v1/health |
| Swagger | http://localhost:5000/swagger |
| Hangfire dashboard | http://localhost:5000/hangfire |
| PostgreSQL | localhost:5432 (user: finance_user / pw: finance_password / db: finance_sentry) |

For faster frontend iteration, run `ng serve` locally while keeping API + DB in Docker:

```bash
# Terminal 1
cd docker && docker compose -f docker-compose.dev.yml up -d postgres api

# Terminal 2
cd frontend && npm start
```

---

## Key Files

```
backend/
  src/
    FinanceSentry.API/
      Program.cs                          # DI registrations, middleware pipeline
    FinanceSentry.Modules.BankSync/
      API/
        Controllers/                      # REST controllers
        Middleware/
          JwtAuthenticationMiddleware.cs  # JWT validation; exempt: /health, /api/v1/health, /swagger, /api/webhook, /hangfire
      Application/                        # CQRS commands/queries (MediatR)
      Domain/                             # Entities, interfaces, repositories
      Infrastructure/                     # EF Core, Plaid HTTP client, encryption

frontend/
  src/app/
    app.routes.ts                         # / → /accounts (lazy bank-sync module), /dashboard
    app.config.ts                         # provideRouter + provideHttpClient (no auth interceptor yet)
    modules/bank-sync/
      services/bank-sync.service.ts       # All API calls (no auth header — missing interceptor)
      pages/                              # accounts-list, connect-account, transaction-list, dashboard

docker/
  docker-compose.dev.yml                  # Full stack: postgres + api + frontend
  Dockerfile                              # Multi-stage backend build (includes NuGet.Config copy)
  Dockerfile.frontend                     # Node 22 Alpine, ng serve

.specify/
  memory/constitution.md                  # Project governance (v1.1.1)
  specs/001-bank-account-sync/            # Feature spec, plan, tasks for bank sync
```

---

## Current App State

**What works:**
- Full Docker stack runs and all three containers are healthy
- API health check: `GET /api/v1/health` → `{"status":"healthy"}`
- Frontend loads at http://localhost:4200, renders "Finance Sentry" header
- All bank-sync pages exist (accounts list, connect, transactions, dashboard)
- Backend: accounts, transactions, sync, webhook, dashboard endpoints all implemented

**What's missing / broken:**
- **No auth UI** — no login page, no register page
- **No HTTP interceptor** — `BankSyncService` sends requests with no `Authorization` header
- Every API call returns `401 {"error":"Authentication required.","errorCode":"UNAUTHORIZED"}`
- Frontend shows "Failed to load accounts. Please try again." on the accounts page
- No route guards protecting pages

**Next up (not yet assigned):** Build the auth flow — login/register pages, token storage, HTTP interceptor to attach `Bearer` token.

---

## Open Follow-ups from speckit.analyze (2026-04-11)

From the cross-artifact analysis of `specs/001-bank-account-sync/`:

- `spec.md`: Resolve `[NEEDS CLARIFICATION]` webhook note — record formal `[DECISION]`
- `plan.md`: Update constitution reference from v1.0.0 → v1.1.1; add versioning compliance row
- `tasks.md`: Add IBankProvider interface task (C2), Phase 4/5 contract test tasks (C3), migration task for `archived_reason` column (H4), re-auth frontend flow task (H5)

---

## Collaboration Style

- Responses must be short and direct. No trailing summaries — Denys can read the diff.
- Lead with the action, skip preamble.
- One fix at a time. Diagnose before pivoting.
- Never change `Host=postgres` to `localhost` to work around Docker issues — fix Docker instead.
- Never modify connection strings or env config as workarounds — fix the root cause.
