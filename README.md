# Finance Sentry

Personal finance aggregation platform — bank accounts, crypto, brokerage, budgets, subscriptions, and alerts in one place.

## Features

- **Multi-provider sync** — Plaid (US banking), Monobank, Binance, Interactive Brokers
- **Automatic transaction sync** with cursor-based incremental updates and webhook support
- **Subscription detection** via Plaid's native recurring transaction API; heuristic fallback for non-Plaid accounts
- **Budget tracking** with spending analysis per category
- **Alerts** — unusual spend detection and configurable thresholds
- **Multi-currency dashboard** with aggregated net worth, money flow, and category breakdown
- **AES-256-GCM encryption** for all stored credentials
- **Full audit logging** of all data access events

## Prerequisites

- Docker & Docker Compose
- Plaid developer account (sandbox credentials)

## Local Development

All `docker compose` commands assume `cd docker` first.

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

### Run everything

```bash
docker compose -f docker-compose.dev.yml up -d --build       # first time / after backend changes
docker compose -f docker-compose.dev.yml up -d               # subsequent runs
```

### Run services separately

```bash
docker compose -f docker-compose.dev.yml up -d postgres api  # db + api only
```

For native frontend with hot reload:

```bash
docker compose -f docker-compose.dev.yml up -d postgres api
cd ../frontend && npm start
```

### Rebuild

```bash
docker compose -f docker-compose.dev.yml up -d --build api         # rebuild and restart api
docker compose -f docker-compose.dev.yml build --no-cache frontend  # clean frontend rebuild
```

Backend (`.cs`) edits require an api rebuild. Frontend (`.ts`/`.html`) edits hot-reload via the bind mount.

### Logs / shell

```bash
docker compose -f docker-compose.dev.yml logs -f api
docker compose -f docker-compose.dev.yml ps

docker exec -it finance-sentry-api sh
docker exec -it finance-sentry-postgres psql -U finance_user -d finance_sentry
```

### Stop / clean

```bash
docker compose -f docker-compose.dev.yml down                 # stop + remove containers
docker compose -f docker-compose.dev.yml down -v              # also drop postgres volume (wipes DB)
```

### Environment variables

| Variable | Description |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL DSN |
| `Deduplication__MasterKeyBase64` | AES-256 master key (base64, 32 bytes) |
| `Plaid__ClientId` | Plaid API client ID |
| `Plaid__Secret` | Plaid API secret |
| `Plaid__WebhookKey` | Plaid webhook signing key |
| `Jwt__Secret` | JWT signing secret (≥32 chars) |

## Running Tests

```bash
# Backend unit + integration tests
cd backend && dotnet test

# Frontend unit tests (Vitest)
cd frontend && npm test
```

## Architecture

```
frontend/                             Angular 21 SPA — strict TypeScript, standalone components
                                      NgRx SignalStore, lazy-loaded feature modules
backend/
  src/
    FinanceSentry.API/                ASP.NET Core 9 host — middleware, DI, migration runner
    FinanceSentry.Core/               Shared interfaces and domain primitives
    FinanceSentry.Infrastructure/     Cross-cutting: encryption, logging
    FinanceSentry.Modules.Auth/       Registration, login, Google OAuth, JWT + refresh tokens
    FinanceSentry.Modules.BankSync/   Plaid + Monobank sync, transactions, dashboard, webhooks
    FinanceSentry.Modules.CryptoSync/ Binance integration, crypto holdings
    FinanceSentry.Modules.BrokerageSync/ IBKR Client Portal, brokerage holdings
    FinanceSentry.Modules.Budgets/    Budget definitions, spend tracking per category
    FinanceSentry.Modules.Alerts/     Alert rules, unusual spend detection, nightly job
    FinanceSentry.Modules.Subscriptions/ Recurring charge detection (Plaid native + heuristic)
docker/
  docker-compose.dev.yml             Full stack (postgres + api + frontend)
  Dockerfile                         Multi-stage backend build with BuildKit cache mounts
  Dockerfile.frontend                Node 22 Alpine, ng serve
```

Each module follows the same internal structure: `Domain/` → `Application/` (CQRS via MediatR) → `Infrastructure/` (EF Core, external clients, Hangfire jobs) → `API/` (controllers). Modules register themselves via `IModuleRegistrar` / `IJobRegistrar` — no manual wiring in `Program.cs`.

## Development Workflow

This project uses **speckit** — a spec-driven development toolchain built on top of Claude Code.

```
constitution → spec → plan → tasks → implement
```

| Command | Purpose |
|---|---|
| `/speckit.specify` | Create or update a feature spec |
| `/speckit.plan` | Generate implementation design from the spec |
| `/speckit.tasks` | Generate ordered task list from the plan |
| `/speckit.implement` | Execute tasks from `tasks.md` |
| `/speckit.analyze` | Cross-artifact consistency check |

Specs live in `.specify/specs/<feature>/`. Architecture principles and quality gates are in [`.specify/memory/constitution.md`](.specify/memory/constitution.md).
