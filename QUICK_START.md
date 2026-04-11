# Finance Sentry — Quick Start

## How to Run

Everything runs in Docker. One command starts the full stack (frontend, API, database):

```bash
cd docker
docker compose -f docker-compose.dev.yml up -d --build
```

Stop everything:

```bash
cd docker
docker compose -f docker-compose.dev.yml down
```

---

## Services

| Service | URL | Description |
|---------|-----|-------------|
| **Frontend** (Angular) | http://localhost:4200 | Angular dev server — main UI |
| **Backend API** | http://localhost:5000/api/v1 | ASP.NET Core REST API |
| **Health check** | http://localhost:5000/api/v1/health | Returns `{"status":"healthy"}` |
| **Swagger UI** | http://localhost:5000/swagger | Interactive API documentation |
| **Hangfire dashboard** | http://localhost:5000/hangfire | Background job monitor (dev only) |
| **PostgreSQL** | localhost:5432 | Database (user: finance_user / pw: finance_password / db: finance_sentry) |

---

## Environment Variables

Sensitive values can be overridden via environment variables or a `docker/.env` file:

```bash
# docker/.env (optional — defaults below work for local sandbox testing)
PLAID_CLIENT_ID=your_plaid_client_id
PLAID_SECRET=your_plaid_secret
JWT_SECRET=your_jwt_secret
ENCRYPTION_MASTER_KEY=your_encryption_key
```

Sandbox defaults are pre-configured in `docker-compose.dev.yml` so the stack
runs out of the box without a `.env` file.

---

## Startup Order

Docker Compose enforces startup order automatically:

```
postgres (healthy)
    └─> api (healthy)
            └─> frontend
```

The API auto-runs EF Core migrations on startup. The frontend waits until the API
reports healthy before starting.

---

## What Is Running

| Container | Image | Source |
|-----------|-------|--------|
| `finance-sentry-postgres` | postgres:14-alpine | Official image |
| `finance-sentry-api` | Built from `docker/Dockerfile` | `backend/` (ASP.NET 9) |
| `finance-sentry-frontend` | Built from `docker/Dockerfile.frontend` | `frontend/` (Angular, `ng serve`) |

---

## Connecting to the Database

```bash
docker exec -it finance-sentry-postgres psql -U finance_user -d finance_sentry
```

---

## Viewing Logs

```bash
# All services
docker compose -f docker/docker-compose.dev.yml logs -f

# Single service
docker compose -f docker/docker-compose.dev.yml logs -f api
docker compose -f docker/docker-compose.dev.yml logs -f frontend
```

---

## Rebuilding After Code Changes

Frontend and API images must be rebuilt after source changes (no hot-reload in Docker):

```bash
cd docker
docker compose -f docker-compose.dev.yml up -d --build
```

> **Tip**: For faster frontend iteration, run `ng serve` locally (outside Docker)
> while keeping the API and database in Docker:
>
> ```bash
> # Terminal 1 — DB + API in Docker
> cd docker && docker compose -f docker-compose.dev.yml up -d postgres api
>
> # Terminal 2 — Frontend locally (hot-reload)
> cd frontend && npm start
> ```
