# Finance Sentry — Claude Context

> **Source of truth split**: For architecture principles, testing requirements, code quality gates, and branching rules — the constitution at [`.specify/memory/constitution.md`](.specify/memory/constitution.md) is authoritative. This file covers **current state only** (what's built, what's running, what's next). When in doubt, constitution wins.

## Project Overview

Finance Sentry is a personal finance aggregation app built as an ASP.NET Core 9 modular monolith + Angular 20 SPA. It integrates with Plaid for bank data, with plans for Interactive Brokers, Binance, and AI-driven portfolio analytics.

Sole developer: Denys. Spec-driven development via the **speckit** toolchain (constitution → spec → plan → tasks → implement).

---

## Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 10 (.NET 10, C# 14), EF Core 10, PostgreSQL 14, hand-rolled CQRS (`FinanceSentry.Core.Cqrs`), Hangfire, Serilog |
| Frontend | Angular 21.2, TypeScript strict, standalone components, NgRx SignalStore (`@ngrx/signals`), lazy-loaded modules |
| UI library | `@dsdevq-common/ui` (local, ng-packagr) — components, `ToastService`, `ErrorMessageService`, `ThemeService` |
| Auth | Custom `JwtAuthenticationMiddleware` (backend) + `AuthStore` signal store + functional `authInterceptor` (frontend). Access token lives **in memory only** (store signal); refresh token is an httpOnly/Secure/SameSite=Strict cookie set by the backend. Silent refresh fires on app init. |
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
| Backend API | http://localhost:5001/api/v1 |
| Health check | http://localhost:5001/api/v1/health |
| Swagger | http://localhost:5001/swagger |
| Hangfire dashboard | http://localhost:5001/hangfire |
| PostgreSQL | localhost:5432 (user: finance_user / pw: finance_password / db: finance_sentry) |

For faster frontend iteration, run `ng serve` locally while keeping API + DB in Docker:

```bash
# Terminal 1
cd docker && docker compose -f docker-compose.dev.yml up -d postgres api

# Terminal 2
cd frontend && npm start
```

---

## Mandatory Rules (auto-loaded)

The gates below apply to every change — they are imported into context on every session:

@docs/claude/frontend-rules.md
@docs/claude/backend-rules.md

## Reference Docs (open when relevant)

Not auto-loaded — follow these links when the task touches them:

- [App state & key files](docs/claude/app-state.md) — what's built/running per feature; update the relevant block when a feature lands
- [QA guide](docs/claude/qa.md) — test creds, golden-path scenarios, post-implementation e2e process
- [AI development pipeline](docs/claude/ai-pipeline.md) — Claude/Qwen roles (Qwen path currently disabled)
- [Speckit agent context](docs/claude/speckit-context.md) — machine-appended Active Technologies / Recent Changes (owned by `.specify/scripts/bash/update-agent-context.sh`; never hand-grow this file's sections in CLAUDE.md again)
- [Program roadmap & backlog](specs/ROADMAP.md) — destination, radar architecture, unimplemented specs

## QA — Test User

`test@gmail.com` / `Darkfly21` — has Plaid, Monobank, Binance and IBKR connections. Full scenarios: [QA guide](docs/claude/qa.md).

---

## Naming & Planning Conventions (adopted 2026-08-12)

| Thing | Convention | Example |
|---|---|---|
| Branch | `<type>/<issue#>-<slug>` — type is a conventional-commit type; create via `gh issue develop <n> -b` so the branch links to the issue | `feat/411-canonical-book-figures` |
| Commit / PR title | Conventional commits, scope = module or spec number (release-please parses these) | `feat(mcp): …`, `fix(040): …` |
| Issue title | Imperative sentence, **no priority prefix** — priority lives in the `P1`/`P2` label and the Project field | `Asset Dossier — per-holding page …` |
| Issue body | Traceability first (destination / source — why this exists), then acceptance criteria (**P1 issues only**; P2/P3 stay one-liners until promoted), then shape in PR count | see #411 |
| Issue metadata | Type (Feature/Bug/Task) + milestone + `P1`/`P2` label + Project "Finance Sentry" Priority/Size fields. Size only on P1 (S=1 PR, M=2–3, L=4+) — never size the fog | — |
| Milestone | `M<n> — <outcome>` — named for the outcome, never a date | `M1 — Ledger earns its keep` |

Backlog planning happens in dedicated sessions (plan-backlog skill); every issue must trace to a destination. Main is protected — all changes land via PR (squash), including agent work.

## Collaboration Style

- Responses must be short and direct. No trailing summaries — Denys can read the diff.
- Lead with the action, skip preamble.
- One fix at a time. Diagnose before pivoting.
- Never change `Host=postgres` to `localhost` to work around Docker issues — fix Docker instead.
- Never modify connection strings or env config as workarounds — fix the root cause.
- Do not create markdown files at the repo root. Only `README.md` and `CLAUDE.md` belong there. Session artifacts, debug notes, and how-to docs do not get their own files — put relevant content in `README.md` or the appropriate `.specify/` artifact.
