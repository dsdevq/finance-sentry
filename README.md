# Finance Sentry

Personal finance aggregation platform with bank account sync, transaction history, and multi-currency dashboard.

## Features

- **Connect bank accounts** via Plaid Link (OAuth-based, no credentials stored in plaintext)
- **Automatic transaction sync** every 2 hours with manual trigger support
- **Multi-currency dashboard** with aggregated balances, money flow charts, and spending categories
- **Transfer detection** across linked accounts
- **AES-256-GCM encryption** for all stored bank credentials
- **Full audit logging** of all data access events (Constitution V compliant)
- **24-month data retention** with automatic archival

## Prerequisites

- .NET 9 SDK
- Node.js 20+
- Docker & Docker Compose
- Plaid developer account (sandbox credentials)

## Local Development Setup

```bash
# 1. Clone the repository
git clone https://github.com/your-org/finance-sentry
cd finance-sentry

# 2. Start PostgreSQL
docker-compose up -d db

# 3. Configure secrets (copy and fill in values)
cp backend/src/FinanceSentry.API/appsettings.json \
   backend/src/FinanceSentry.API/appsettings.Development.json

# Required values in appsettings.Development.json:
# - ConnectionStrings:Default  (PostgreSQL connection string)
# - Deduplication:MasterKeyBase64  (32-byte AES key, base64)
# - Plaid:ClientId, Plaid:Secret, Plaid:WebhookKey
# - Jwt:Secret  (JWT signing secret, ≥32 chars)

# 4. Run database migrations
cd backend
dotnet ef database update \
  --project src/FinanceSentry.Modules.BankSync \
  --startup-project src/FinanceSentry.API

# 5. Start the backend
dotnet run --project src/FinanceSentry.API

# 6. Start the frontend (separate terminal)
cd frontend
npm install
npm start
```

Application URLs:
- Frontend: http://localhost:4200
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Hangfire: http://localhost:5000/hangfire
- Health: http://localhost:5000/health/ready

## Running Tests

```bash
# All tests
cd backend && dotnet test

# Unit tests only
dotnet test tests/FinanceSentry.Tests.Unit

# Integration tests
dotnet test tests/FinanceSentry.Tests.Integration

# Load tests (requires k6)
k6 run tests/load/bank-sync-load-test.js
```

## Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__Default` | PostgreSQL DSN | `Host=localhost;Database=finance_sentry;Username=postgres;Password=...` |
| `Deduplication__MasterKeyBase64` | AES-256 master key (base64) | `<32-byte key>` |
| `Plaid__ClientId` | Plaid API client ID | `abc123` |
| `Plaid__Secret` | Plaid API secret | `secret` |
| `Plaid__WebhookKey` | Plaid webhook signing key | `whsec_...` |
| `Jwt__Secret` | JWT signing secret | `<≥32 char secret>` |
| `FeatureFlags__BANK_SYNC_ENABLED` | Feature flag (bool or 0-100%) | `true` |

## Deployment

```bash
# Build production Docker image
docker build -t finance-sentry-api ./backend

# Run with production environment
docker run -p 5000:5000 \
  -e ConnectionStrings__Default="..." \
  -e Plaid__ClientId="..." \
  finance-sentry-api
```

## Architecture

```
frontend/          Angular 18+ SPA (standalone components)
backend/
  src/
    FinanceSentry.API/           ASP.NET Core 9 host + Program.cs
    FinanceSentry.Core/          Shared domain abstractions
    FinanceSentry.Infrastructure/ Encryption service
    FinanceSentry.Modules.BankSync/  Modular monolith module
      Domain/                    Entities, repositories, domain events
      Application/               CQRS commands/queries/handlers, services
      Infrastructure/            EF Core, Plaid adapter, Hangfire jobs
      API/                       Controllers, middleware, validators
      Migrations/                EF Core migrations
  tests/
    FinanceSentry.Tests.Unit/
    FinanceSentry.Tests.Integration/
tests/
  load/                          k6 load tests
docs/                            API docs, runbooks, security audit
```

## Documentation

- [API Collection (Postman)](docs/Bank-Sync-API.postman_collection.json)
- [Security Audit](docs/SECURITY_AUDIT.md)
- [Database Backup & Recovery](docs/DATABASE_BACKUP.md)
- [Operations Runbook](docs/OPERATIONS_RUNBOOK.md)
- [QA Test Plan](docs/QA_TEST_PLAN.md)
- [Data Export Guide](docs/DATA_EXPORT_GUIDE.md)
- [Multi-Currency Guide](docs/MULTI_CURRENCY_GUIDE.md)
