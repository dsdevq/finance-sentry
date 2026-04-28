# Finance Sentry

Personal finance aggregation platform with bank account sync, transaction history, and multi-currency dashboard.

## Features

- **Connect bank accounts** via Plaid Link (OAuth-based, no credentials stored in plaintext)
- **Automatic transaction sync** every 2 hours with manual trigger support
- **Multi-currency dashboard** with aggregated balances, money flow charts, and spending categories
- **Transfer detection** across linked accounts
- **AES-256-GCM encryption** for all stored bank credentials
- **Full audit logging** of all data access events

## Prerequisites

- Docker & Docker Compose
- Plaid developer account (sandbox credentials)

## Local Development

All `docker compose` commands assume `cd docker` first. The compose file is `docker-compose.dev.yml`. Frontend source is bind-mounted into its container, so edits hot-reload without rebuilding.

### Service map

| Service | Container | Port |
|---|---|---|
| Frontend (Angular dev server) | `finance-sentry-frontend` | 4200 |
| Backend API (.NET 9) | `finance-sentry-api` | 5001 (host) → 5000 (container) |
| PostgreSQL 14 | `finance-sentry-postgres` | 5432 |

Startup order enforced by health checks: `postgres → api → frontend`.

| URL | What |
|---|---|
| http://localhost:4200 | Angular SPA |
| http://localhost:5001/api/v1 | REST API |
| http://localhost:5001/api/v1/health | Health probe |
| http://localhost:5001/swagger | Swagger UI |
| http://localhost:5001/hangfire | Hangfire dashboard |

### Run everything together

```bash
docker compose -f docker-compose.dev.yml up -d --build       # first time / after Dockerfile changes
docker compose -f docker-compose.dev.yml up -d               # subsequent runs (no rebuild)
```

### Run services separately

```bash
docker compose -f docker-compose.dev.yml up -d postgres            # db only
docker compose -f docker-compose.dev.yml up -d postgres api        # db + api (skip frontend)
docker compose -f docker-compose.dev.yml up -d frontend            # frontend only (assumes api+db already up)
```

For native frontend with hot reload (alternative to the frontend container):

```bash
docker compose -f docker-compose.dev.yml up -d postgres api
cd ../frontend && npm start
```

### Rebuild

```bash
docker compose -f docker-compose.dev.yml build api                 # rebuild a single service
docker compose -f docker-compose.dev.yml build --no-cache frontend # force a clean rebuild
docker compose -f docker-compose.dev.yml up -d --build api         # rebuild and restart in one step
docker compose -f docker-compose.dev.yml up -d --force-recreate    # recreate containers without rebuild
```

Backend (`.cs`) edits require an api rebuild; frontend (`.ts`/`.html`) edits do **not** — the bind mount + `ng serve --watch` picks them up live.

### Logs / shell / inspect

```bash
docker compose -f docker-compose.dev.yml logs -f api               # tail one service
docker compose -f docker-compose.dev.yml logs -f                   # tail everything
docker compose -f docker-compose.dev.yml ps                        # service status + health

docker exec -it finance-sentry-api bash                            # shell into api
docker exec -it finance-sentry-postgres psql -U finance_user -d finance_sentry
```

### Stop / clean

```bash
docker compose -f docker-compose.dev.yml stop                      # stop, keep containers + volumes
docker compose -f docker-compose.dev.yml down                      # stop + remove containers
docker compose -f docker-compose.dev.yml down -v                   # also drop the postgres volume (DESTRUCTIVE — wipes the DB)
```

### Environment Variables

| Variable | Description |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL DSN |
| `Deduplication__MasterKeyBase64` | AES-256 master key (base64, 32 bytes) |
| `Plaid__ClientId` | Plaid API client ID |
| `Plaid__Secret` | Plaid API secret |
| `Plaid__WebhookKey` | Plaid webhook signing key |
| `Jwt__Secret` | JWT signing secret (≥32 chars) |

## Development Workflow

This project uses **speckit** — a spec-driven development toolchain built on top of Claude Code.

```
constitution → spec → plan → tasks → implement
```

### Toolchain

| Command | Purpose |
|---|---|
| `/speckit.specify` | Create or update a feature spec from a natural language description |
| `/speckit.clarify` | Identify underspecified areas in the current spec |
| `/speckit.plan` | Generate implementation design from the spec |
| `/speckit.tasks` | Generate a dependency-ordered task list from the plan |
| `/speckit.implement` | Execute tasks from `tasks.md` |
| `/speckit.analyze` | Cross-artifact consistency check across spec/plan/tasks |
| `/speckit.constitution` | Create or amend the project constitution |

### Artifacts

```
.specify/
  memory/
    constitution.md     # Project governance — principles, quality gates, branching rules
  specs/
    001-bank-account-sync/
      spec.md           # Feature requirements and acceptance criteria
      plan.md           # Implementation design and component breakdown
      tasks.md          # Ordered, actionable task list
  templates/            # Spec, plan, tasks, agent-file templates
```

### Agent Context

- [`CLAUDE.md`](CLAUDE.md) — Current project state (stack, what's built, what's next). Auto-loaded by Claude on every session.
- [`.specify/memory/constitution.md`](.specify/memory/constitution.md) — Authoritative source for architecture principles, testing requirements, and code quality gates. When CLAUDE.md and the constitution conflict, the constitution wins.

### Governance Rules (summary)

Full rules are in the constitution. Key gates that block PR merge:

- Failing linter / zero-warning build violation
- Missing or incomplete tests (unit >80% coverage; contract tests mandatory for every endpoint and external integration)
- External integration accessed without a domain interface
- Version not bumped on frontend/API changes

## Architecture

```
frontend/                       Angular 20+ SPA (strict TypeScript, lazy-loaded modules)
backend/
  src/
    FinanceSentry.API/           ASP.NET Core 9 host — middleware pipeline, DI
    FinanceSentry.Modules.BankSync/
      Domain/                   Entities, repositories, domain events
      Application/              CQRS commands/queries (MediatR)
      Infrastructure/           EF Core, Plaid adapter, Hangfire jobs, encryption
      API/                      Controllers, JWT middleware, validators
docker/
  docker-compose.dev.yml        Full stack (postgres + api + frontend)
  Dockerfile                    Multi-stage backend build
  Dockerfile.frontend           Node 22 Alpine, ng serve
```

## Running Tests

```bash
# Backend
cd backend && dotnet test

# Frontend
cd frontend && npm test
```
