# Finance Sentry — Claude Context

> **Source of truth split**: For architecture principles, testing requirements, code quality gates, and branching rules — the constitution at [`.specify/memory/constitution.md`](.specify/memory/constitution.md) is authoritative. This file covers **current state only** (what's built, what's running, what's next). When in doubt, constitution wins.

## Project Overview

Finance Sentry is a personal finance aggregation app built as an ASP.NET Core 9 modular monolith + Angular 20 SPA. It integrates with Plaid for bank data, with plans for Interactive Brokers, Binance, and AI-driven portfolio analytics.

Sole developer: Denys. Spec-driven development via the **speckit** toolchain (constitution → spec → plan → tasks → implement).

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
  memory/constitution.md                  # Project governance (v1.2.0)
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

## Frontend ESLint — mandatory gate

After writing or modifying **any** Angular `.ts` file, run `npx eslint <file>` from `frontend/` and fix all errors before moving on. Non-negotiable rules (see constitution § II for the full list):
- `inject()` only — no constructor parameter injection
- `ChangeDetectionStrategy.OnPush` on every component
- Selector prefix: `fns-` (e.g. `fns-login`, `fns-dashboard`)
- Explicit access modifiers on all class members (`public`/`private`)
- No magic numbers — extract to named constants
- camelCase class properties, no underscore prefix
- Run `eslint --fix` after writing imports (auto-sorts + auto-formats)

---

## UI Component Library Rule

**Any new UI component MUST be created in `@dsdevq-common/ui` first.** Components are never built directly in the host Angular app (`frontend/`). This applies to all future features, starting with 005-ui-component-library. The `cmn-` selector prefix is reserved for library components.

---

## AI Development Pipeline

This project uses a two-model pipeline. [`.specify/memory/pipeline.md`](.specify/memory/pipeline.md) is the **source of truth** for pipeline roles, knowledge store structure, and the per-task implementation loop — read it before starting any implementation session.

**Claude** = planner + orchestrator + reviewer (this session).
**Qwen2.5-coder:14b** (local Ollama) = implementer, called via the `qwen-code` MCP server.

> **Hard rule:** Qwen is the sole code producer. If a Qwen MCP call fails with a connection error, **stop immediately** and report the error — do NOT write implementation code directly as a fallback. Wait for the user to confirm Ollama is back up before retrying. Only bypass this rule if the user explicitly says so.

- MCP config: `.mcp.json` + `.claude/settings.json` → `enabledMcpjsonServers: ["qwen-code"]`
- MCP is only active in **new sessions** — check `/mcp` to confirm it loaded
- Knowledge rules live in `.specify/knowledge/index.yaml` (30 rules); inject into QWEN.md with `py .specify/integrations/qwen/scripts/inject-knowledge.py`
- Per-task loop: Claude calls Qwen MCP → reads diff → reviews inline → approves or requests fix → commits → next task
- Reviews saved to `.specify/knowledge/reviews/<feature>/<task>.yaml`

---

## QA — End-to-End Testing After Implementation

After **all tasks in a feature are complete**, act as a QA engineer: spin up the app and test the feature through the browser using Playwright MCP.

**Steps (mandatory):**
1. Ensure the full Docker stack is running: `cd docker && docker compose -f docker-compose.dev.yml up -d`
2. Wait for health check: `GET http://localhost:5000/api/v1/health` → `{"status":"healthy"}`
3. Open `http://localhost:4200` via Playwright
4. Navigate the golden path of the feature as a real user would — click buttons, fill forms, follow redirects
5. Also test key error/edge cases (invalid input, cancelled flows, etc.)
6. Report findings: what passed, what failed, screenshots of any broken state
7. If bugs are found, fix them (via Qwen) before declaring the feature done

**Tools:** Use `mcp__plugin_playwright_playwright__browser_*` tools — snapshot first, screenshot only when visual proof is needed.

---

## Collaboration Style

- Responses must be short and direct. No trailing summaries — Denys can read the diff.
- Lead with the action, skip preamble.
- One fix at a time. Diagnose before pivoting.
- Never change `Host=postgres` to `localhost` to work around Docker issues — fix Docker instead.
- Never modify connection strings or env config as workarounds — fix the root cause.
- Do not create markdown files at the repo root. Only `README.md` and `CLAUDE.md` belong there. Session artifacts, debug notes, and how-to docs do not get their own files — put relevant content in `README.md` or the appropriate `.specify/` artifact.

## Active Technologies
- C# 13 / .NET 9 (backend) · TypeScript 5.x strict (frontend) + ASP.NET Core 9, EF Core 9, MediatR, ASP.NET Core Identity (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`), Npgsql.EF Core (backend) · Angular 20, RxJS, Angular standalone routing (frontend) (003-auth-flow)
- PostgreSQL 14 — shared database, separate `AuthDbContext : IdentityDbContext<ApplicationUser>` with independent migrations (003-auth-flow)
- TypeScript 5.3 / Angular 21.2 + Angular CDK (behavior primitives), ng-packagr (library build), Tailwind CSS v3 3.4.x (design tokens via `tailwind.config.js` + CSS custom properties in `styles/theme.css`), Storybook 10 (`@storybook/angular`), chroma-js 3.x (runtime palette generation), Lucide Icons (icon set), Vitest 4 (unit tests), Playwright (visual regression) (005-ui-component-library)
- `localStorage` (theme + accent persistence only) (005-ui-component-library)
- TypeScript 5.3 / Angular 21.2 (strict mode) + `@dsdevq-common/ui` (local library, feature 005), Angular `ReactiveFormsModule`, Angular CLI (006-ui-library-adoption)
- `localStorage` (ThemeService — already implemented in feature 005) (006-ui-library-adoption)
- C# 13 / .NET 9 (backend) · TypeScript 5.x strict (frontend) + ASP.NET Core 9, EF Core 9, MediatR, ASP.NET Core Identity, `Google.Apis.Auth` (new) · Angular 20, RxJS, `@types/google.accounts` (new) (004-adopt-oauth)
- PostgreSQL 14 — `AuthDbContext : IdentityDbContext<ApplicationUser>` — `OAuthStates` table to be DROPPED via new migration (004-adopt-oauth)

## Recent Changes
- 003-auth-flow: Added C# 13 / .NET 9 (backend) · TypeScript 5.x strict (frontend) + ASP.NET Core 9, EF Core 9, MediatR, ASP.NET Core Identity (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`), Npgsql.EF Core (backend) · Angular 20, RxJS, Angular standalone routing (frontend)
