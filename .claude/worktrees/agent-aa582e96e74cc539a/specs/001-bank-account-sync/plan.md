# Implementation Plan: Bank Account Aggregation & Sync (Feature 001)

**Branch**: `001-bank-account-sync` | **Date**: 2026-03-21 | **Spec**: [specs/001-bank-account-sync/spec.md](specs/001-bank-account-sync/spec.md)
**Input**: Feature specification from `/specs/001-bank-account-sync/spec.md`

## Summary

Feature 001 provides foundational bank account aggregation and synchronization for Finance Sentry. Users connect their bank accounts (Ireland: AIB, Revolut; Ukraine: Monobank, PrivateBank via Plaid), and the system securely stores encrypted credentials, retrieves transaction history (6-12 months), and automatically syncs new transactions on a configurable schedule. Multi-account aggregation enables unified dashboard views with money flow statistics. Success measured by: sync completion within 5 minutes, 99.9% sync reliability, sub-second query response, and accurate transaction deduplication. Architecture uses modular monolith pattern (.NET 9+ backend) with isolated sync modules per bank, AES-256 credential encryption, and PostgreSQL transaction store.

## Technical Context

**Language/Version**: .NET Core 9+ (C#), ASP.NET Core modular monolith  
**Primary Dependencies**: 
  - **Plaid**: Bank API aggregation (credential-based, multiple institutions)
  - **MediatR**: CQRS command/query dispatcher within modules
  - **Entity Framework Core 9**: ORM for transaction/account persistence
  - **Hangfire** or hosted service: Background job scheduling for sync
  - **Microsoft.Extensions.Caching.Distributed**: Distributed caching (Redis or in-memory)
  - **System.Security.Cryptography**: AES-256 encryption for credentials  
**Storage**: PostgreSQL 14+ (accounts, transactions, sync_jobs, encrypted_credentials tables)  
**Testing**: xUnit + Moq (unit), Docker + testcontainers (integration, real PostgreSQL), contract tests against Plaid API mocks  
**Target Platform**: ASP.NET Core hosted on Linux (Docker containers); also runs on Windows for dev
**Project Type**: Web service backend (modular monolith) + frontend (Angular SPA consuming REST API)  
**Performance Goals**: 
  - Sync completion: < 5 minutes for initial 6-12 month history fetch
  - Query latency: < 1 second for aggregated balance/stats across 50+ accounts
  - Throughput: minimum 100 concurrent sync jobs without queue backlog
  - Database query: < 50ms for transaction list + balance by account  
**Constraints**: 
  - Credential encryption adds ≤ 50ms latency per encrypt/decrypt cycle
  - Plaid rate limits: 20 items per minute (must respect backoff)
  - PostgreSQL max connections: plan for < 50 active connections
  - Bank API outages: graceful degradation with exponential retry backoff (max 3 attempts)  
**Scale/Scope**: 
  - Phase 1: 100 concurrent beta users, each with 1-5 bank accounts
  - Supports scaling to 10k users with shard-by-user strategy if needed
  - Initial history: 6-12 months per account (~200-500 transactions per account avg)
  - Sync frequency: configurable 2-hour default (16 syncs/day per user)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Constitutional Principles Applied** (from Finance Sentry Constitution v1.0.0):

| Principle | Gate | Status | Evidence |
|-----------|------|--------|----------|
| **I. Modular Monolith Architecture** | Backend MUST use ASP.NET 9+ with DDD, modules isolated by bank integration type | ✅ PASS | Plan specifies isolated sync modules per bank, MediatR for CQRS, modular boundaries enforced |
| **II. Code Quality Enforcement** | Strict linting + zero-warning builds (.NET analyzers, StyleCop), 80%+ test coverage required | ✅ PASS | Plan includes xUnit + contract tests, CI/CD gates specified, linting automated |
| **III. Multi-Source Financial Integration** | Multiple bank APIs via adapter pattern, graceful API failure handling, retry logic | ✅ PASS | Plan uses Plaid (multi-bank), exponential backoff (max 3 attempts), error scoping per module |
| **IV. AI-Driven Analytics (Not Applicable)** | N/A for Feature 001 (sync layer only) | ⏭️ DEFER | AI analytics deferred to Feature 003 (forecasts/recommendations) |
| **V. Security-First Data Handling** | All credentials encrypted at rest (AES-256), user-scoped queries, no plaintext logging | ✅ PASS | Plan enforces AES-256 encryption, user scoping in data model, sensitive data never logged |

**Complexity Justifications**: No violations detected. All design choices adhere to Constitution v1.0.0.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

## Project Structure

### Documentation (this feature)

```text
specs/001-bank-account-sync/
├── plan.md              # This file (implementation plan)
├── research.md          # Phase 0 output (research findings & decisions)
├── data-model.md        # Phase 1 output (entity diagrams, relationships)
├── quickstart.md        # Phase 1 output (developer quick-start guide)
├── contracts/           # Phase 1 output (API contracts, Plaid integration specs)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

**Selected**: Option 2 - Web application (frontend + backend)

```text
backend/
├── src/
│   ├── Modules/
│   │   ├── BankSync/
│   │   │   ├── Domain/               # DDD entities, value objects
│   │   │   │   ├── BankAccount.cs
│   │   │   │   ├── Transaction.cs
│   │   │   │   ├── SyncJob.cs
│   │   │   │   └── EncryptedCredential.cs
│   │   │   ├── Application/          # Use cases, CQRS commands/queries
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── ConnectBankAccountCommand.cs
│   │   │   │   │   ├── SyncTransactionsCommand.cs
│   │   │   │   │   └── DeleteAccountCommand.cs
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetAccountsQuery.cs
│   │   │   │   │   ├── GetTransactionsQuery.cs
│   │   │   │   │   └── GetAggregatedBalanceQuery.cs
│   │   │   │   └── Services/
│   │   │   │       ├── SyncOrchestrator.cs
│   │   │   │       ├── CredentialEncryption.cs
│   │   │   │       └── TransactionDeduplication.cs
│   │   │   ├── Infrastructure/       # External integrations (Plaid, DB)
│   │   │   │   ├── PlaidAdapter.cs
│   │   │   │   ├── Persistence/      # EF Core DbContext
│   │   │   │   │   └── BankSyncDbContext.cs
│   │   │   │   └── Jobs/
│   │   │   │       └── ScheduledSyncJob.cs
│   │   │   └── API/
│   │   │       └── BankSyncController.cs
│   │   └── Shared/                   # Common, shared modules
│   │       ├── Encryption/
│   │       ├── Retry/
│   │       └── Logging/
│   ├── Startup/
│   │   ├── Program.cs
│   │   ├── DependencyInjection.cs
│   │   └── appsettings.json
│   └── Migrations/                   # EF Core migrations
│       └── M001_BankSyncSchema.cs
│
├── tests/
│   ├── Unit/
│   │   ├── BankSync/
│   │   │   ├── Domain/
│   │   │   │   ├── BankAccountTests.cs
│   │   │   │   └── TransactionDeduplicationTests.cs
│   │   │   └── Application/
│   │   │       ├── SyncOrchestratorTests.cs
│   │   │       └── CredentialEncryptionTests.cs
│   │   └── Shared/
│   │       └── RetryPolicyTests.cs
│   │
│   ├── Integration/
│   │   ├── BankSync/
│   │   │   ├── PlaidAdapterTests.cs (uses testcontainers + Plaid mock)
│   │   │   ├── BankSyncControllerTests.cs
│   │   │   └── EndToEndSyncTests.cs
│   │   └── Database/
│   │       └── BankSyncDbContextTests.cs (uses testcontainers PostgreSQL)
│   │
│   └── Contract/
│       ├── PlaidContractTests.cs      # Validate Plaid API contracts
│       └── APIContractTests.cs        # Validate REST API contracts
│
└── docker/
    └── Dockerfile                     # Multi-stage build for backend

frontend/
├── src/
│   ├── app/
│   │   ├── modules/
│   │   │   └── bank-sync/
│   │   │       ├── pages/
│   │   │       │   ├── connect-account/
│   │   │       │   │   └── connect-account.component.ts
│   │   │       │   ├── accounts-list/
│   │   │       │   │   └── accounts-list.component.ts
│   │   │       │   └── transaction-list/
│   │   │       │       └── transaction-list.component.ts
│   │   │       ├── services/
│   │   │       │   └── bank-sync.service.ts
│   │   │       ├── models/
│   │   │       │   ├── bank-account.model.ts
│   │   │       │   └── transaction.model.ts
│   │   │       └── bank-sync.module.ts
│   │   ├── core/
│   │   │   ├── auth/
│   │   │   ├── interceptors/
│   │   │   └── guards/
│   │   ├── shared/
│   │   │   ├── components/
│   │   │   └── pipes/
│   │   └── app.component.ts
│   ├── assets/
│   ├── styles/
│   └── main.ts
│
├── tests/
│   ├── unit/
│   │   └── bank-sync/
│   │       ├── services/
│   │       │   └── bank-sync.service.spec.ts
│   │       └── components/
│   │           └── accounts-list.component.spec.ts
│   │
│   └── integration/
│       └── bank-sync/
│           └── connect-account.e2e.spec.ts
│
├── angular.json
├── tsconfig.json                     # Strict mode: true
├── .eslintrc.json                    # Strict linting
└── docker/
    └── Dockerfile.web                # Multi-stage build for frontend

root/
├── docker-compose.yml                # Local dev: backend + frontend + PostgreSQL + Redis
├── docker-compose.prod.yml           # Production deployment
├── .github/
│   └── workflows/
│       ├── ci.yml                    # Lint + test + build on PR
│       └── deploy.yml                # Deploy to staging/prod on merge
├── Dockerfile.backend                # Backend container
├── Dockerfile.frontend               # Frontend container
└── .env.example                      # Environment configuration template
```

**Structure Decision**: 
- **Backend**: ASP.NET modular monolith with Bank Sync as first module. Modules communicate via service contracts (interfaces). Each integration (Plaid) is isolated within the module's Infrastructure layer.
- **Frontend**: Angular feature module (`bank-sync`) isolated within larger app. Angular strict mode enforced. Feature communicates via `BankSyncService` to REST API.
- **Testing**: Unit tests co-located with source (xUnit convention `*.Tests.cs`), integration tests use testcontainers + Docker for PostgreSQL and Plaid mocks, contract tests validate external APIs.
- **Docker**: Multi-stage builds for both backend and frontend. `docker-compose` for local development (database + cache + APIs). Separate prod compose for scalability.
