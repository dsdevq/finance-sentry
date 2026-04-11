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

Everything runs in Docker:

```bash
cd docker
docker compose -f docker-compose.dev.yml up -d --build
```

Startup order is enforced by health checks: `postgres → api → frontend`

| Service | URL |
|---|---|
| Frontend (Angular) | http://localhost:4200 |
| Backend API | http://localhost:5000/api/v1 |
| Health check | http://localhost:5000/api/v1/health |
| Swagger UI | http://localhost:5000/swagger |
| Hangfire dashboard | http://localhost:5000/hangfire |
| PostgreSQL | localhost:5432 |

For faster frontend iteration, run `ng serve` locally while keeping API + DB in Docker:

```bash
# Terminal 1 — backend + db only
cd docker && docker compose -f docker-compose.dev.yml up -d postgres api

# Terminal 2 — frontend with hot reload
cd frontend && npm start
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
