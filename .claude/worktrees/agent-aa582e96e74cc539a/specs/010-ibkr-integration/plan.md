# Implementation Plan: IBKR Integration

**Branch**: `010-ibkr-integration` | **Date**: 2026-04-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/010-ibkr-integration/spec.md`

## Summary

Add Interactive Brokers portfolio integration as a new `FinanceSentry.Modules.BrokerageSync` module. The module name is generic to allow future brokerage adapters (Schwab, Fidelity, etc.) under the same boundary. IBKR is the first concrete `IBrokerAdapter` implementation. The feature uses the IBKR Client Portal Web API via a self-hosted gateway sidecar; `IBKRCredential` and `BrokerageHolding` entities live in a new `BrokerageSyncDbContext`. Holdings are wired into `/wealth/summary` via `IBrokerageHoldingsReader` in `FinanceSentry.Core`. A Hangfire recurring job syncs positions every 15 minutes. Endpoints: `POST /brokerage/ibkr/connect`, `DELETE /brokerage/ibkr/disconnect`, `GET /brokerage/holdings`.

**Single-tenant gateway (revised 2026-04-26)**:

- **Image**: `voyz/ibeam:latest` — wraps IBKR's Java Client Portal Gateway with a Selenium-driven auto-login. The original choice (`ghcr.io/gnzsnz/ib-gateway`) was the wrong product — it serves the Trading API on port 4001/4002, not the Client Portal Web API the codebase calls. See `research.md` Decision 2 for details.
- **Session ownership**: the IBeam sidecar holds **one** IBKR session for the entire deployment, authenticated from `IBKR_ACCOUNT` / `IBKR_PASSWORD` env vars (`docker/.env`). The user does not type credentials in the UI — the connect flow is a single "Connect" button that verifies the gateway session and stores the user-id ↔ discovered `AccountId` link. See `research.md` Decision 3.
- **Credential model**: `IBKRCredential` stores only `(UserId, AccountId, IsActive, LastSyncAt, LastSyncError, CreatedAt)` — no encrypted username/password, no AES-256-GCM dependency. `ICredentialEncryptionService` is **not** referenced by this module.
- **Adapter interface**: `IBrokerAdapter` exposes `EnsureSessionAsync(ct)` (verifies the gateway is authenticated via `/iserver/auth/status`), `GetAccountIdAsync(ct)`, and `GetPositionsAsync(accountId, ct)`. There is no `AuthenticateAsync` method — the gateway owns auth.
- **Self-signed cert**: IBeam serves `https://ibkr-gateway:5000` with a self-signed cert. The `HttpClient<IBKRGatewayClient>` registration in `Program.cs` accepts the self-signed cert in `Development` or when `IBKR:AllowSelfSignedCert=true`. Production must terminate TLS at a real proxy.
- **Multi-tenant migration**: when this app moves to public/multi-user, switch to IBKR's **OAuth Web API** (per-user authorization) and add encrypted access/refresh-token columns. Estimated 2–3 days of focused work.

**Status (2026-04-26)**: the `ibkr-gateway` Compose service block is currently **commented out** in `docker/docker-compose.dev.yml`. Reason: live-account login through IBeam succeeds at the SSO step but IBKR drops the session immediately afterwards (likely a pending agreement or a read-only-API exemption that hasn't propagated). Backend module, frontend launcher, migration, and tests are all in place — uncommenting the service plus completing IBKR-side configuration will bring the integration online.

**JSON serialization (revised 2026-04-26)**: `IBKRGatewayClient` and `IBKRGatewayModels` use `System.Text.Json` (`[JsonPropertyName]` + `System.Net.Http.Json` extension methods `PostAsJsonAsync` / `ReadFromJsonAsync`). The original implementation used Newtonsoft.Json which has been removed from this module's `csproj` and from `Directory.Packages.props`.

Backend-only — minimal frontend touch (a single "Connect" launcher component already in place).

## Technical Context

**Language/Version**: C# 13 / .NET 9  
**Primary Dependencies**: ASP.NET Core 9, EF Core 9, MediatR, Hangfire, `System.Net.Http` (no new NuGet packages — IBKR Client Portal API is plain REST+JSON; no official IBKR .NET SDK exists)  
**Storage**: PostgreSQL 14 — new `BrokerageSyncDbContext` with migration M001 adding `IBKRCredentials` and `BrokerageHoldings` tables; no changes to `BankSyncDbContext` or `CryptoSyncDbContext`  
**Testing**: xUnit (unit + contract tests); Vitest/Playwright not needed (backend-only)  
**Target Platform**: Linux server (Docker), existing Docker Compose stack; IB Gateway added as a sidecar container  
**Project Type**: Web service (new modular monolith module extension)  
**Performance Goals**: Holdings endpoint ≤ 500ms for 500 positions; sync completes ≤ 60s per user per trigger  
**Constraints**: No new NuGet packages; reuse existing `ICredentialEncryptionService`; IB Gateway must be healthy before sync attempts  
**Scale/Scope**: Single developer; personal finance app; 1 IBKR account per user

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design — all gates pass.*

| Principle | Status | Notes |
|---|---|---|
| I — Modular Monolith + domain interfaces | ✅ PASS | New `BrokerageSync` module per "brokers are a distinct module"; `IBrokerAdapter` introduced; `IBKRAdapter` concrete impl accessed via interface only |
| II — Code quality enforcement | ✅ PASS | StyleCop enforced; zero-warning build required; no frontend TS files in this feature |
| III — Multi-source financial integration | ✅ PASS | This feature IS the IBKR broker integration principle in action |
| IV — AI analytics | N/A | Not in scope |
| V — Security-first | ✅ PASS | No per-user credentials stored (single-tenant gateway); deploy-time creds in `docker/.env` only; user-scoped queries on the link metadata. |
| Testing Discipline — External API contracts | ✅ PASS | Contract tests mandatory for IB Gateway HTTP calls + new REST endpoints |
| Testing Discipline — REST endpoint contracts | ✅ PASS | Each new endpoint (`/connect`, `/disconnect`, `/holdings`) ships with a contract test |
| Versioning | ✅ PASS | Backend minor version bump (3 new endpoints) |

## Project Structure

### Documentation (this feature)

```text
specs/010-ibkr-integration/
├── plan.md              ← this file
├── research.md          ← Phase 0 complete
├── data-model.md        ← Phase 1 complete
├── quickstart.md        ← Phase 1 complete
├── contracts/
│   ├── ibkr-connect.md
│   └── ibkr-holdings.md
└── tasks.md             ← Phase 2 (generated by /speckit.tasks)
```

### Source Code (repository root)

```text
backend/src/FinanceSentry.Modules.BrokerageSync/          [NEW PROJECT]
├── Domain/
│   ├── IBKRCredential.cs                                  [NEW] user ↔ IBKR account-id link (single-tenant; no encrypted creds)
│   ├── BrokerageHolding.cs                                [NEW] per-position snapshot entity
│   ├── Interfaces/
│   │   └── IBrokerAdapter.cs                              [NEW] domain interface for brokers
│   ├── Repositories/
│   │   └── IRepositories.cs                               [NEW] IIBKRCredentialRepository, IBrokerageHoldingRepository
│   └── Exceptions/
│       └── BrokerAuthException.cs                         [NEW]
├── Application/
│   ├── Commands/
│   │   ├── ConnectIBKRCommand.cs                          [NEW] authenticate + discover accountId + store + initial sync
│   │   ├── DisconnectIBKRCommand.cs                       [NEW] delete credentials + holdings
│   │   └── SyncIBKRHoldingsCommand.cs                     [NEW] fetch positions + upsert holdings
│   ├── Queries/
│   │   └── GetBrokerageHoldingsQuery.cs                   [NEW] return current holdings for user
│   └── Services/
│       └── BrokerageHoldingsReader.cs                     [NEW] implements IBrokerageHoldingsReader
├── Infrastructure/
│   ├── IBKR/
│   │   ├── IBKRAdapter.cs                                 [NEW] IBrokerAdapter impl
│   │   ├── IBKRGatewayClient.cs                           [NEW] REST calls to IB Gateway
│   │   └── IBKRGatewayModels.cs                           [NEW] API response records
│   ├── Persistence/
│   │   ├── BrokerageSyncDbContext.cs                      [NEW]
│   │   ├── BrokerageSyncDbContextFactory.cs               [NEW]
│   │   └── Repositories/
│   │       └── Repositories.cs                            [NEW] EF implementations
│   └── Jobs/
│       └── IBKRSyncJob.cs                                 [NEW] Hangfire recurring job (*/15 * * * *)
├── API/
│   └── Controllers/
│       └── BrokerageController.cs                         [NEW] POST/DELETE connect, GET holdings
├── Migrations/
│   └── M001_InitialSchema.cs                              [NEW] + Designer.cs
└── BrokerageSyncModule.cs                                 [NEW]

backend/src/FinanceSentry.Core/
└── Interfaces/
    └── IBrokerageHoldingsReader.cs                        [NEW] shared cross-module contract

backend/src/FinanceSentry.Modules.BankSync/
└── Application/Services/
    └── WealthAggregationService.cs                        [MODIFIED] inject + merge IBrokerageHoldingsReader

backend/src/FinanceSentry.API/
└── Program.cs                                             [MODIFIED] register BrokerageSync DI + DbContext + Hangfire job

backend/tests/FinanceSentry.Tests.Unit/
└── BrokerageSync/
    ├── IBKRAdapterContractTests.cs                        [NEW] mock HTTP; validate IB Gateway response shape
    ├── ConnectIBKRCommandTests.cs                         [NEW]
    └── IBKRSyncJobTests.cs                                [NEW]
```

**Structure Decision**: Mirrors the `CryptoSync` module pattern exactly. New standalone .NET project `FinanceSentry.Modules.BrokerageSync` with its own `BrokerageSyncDbContext` and independent migration history. Cross-module contract (`IBrokerageHoldingsReader`) lives in `FinanceSentry.Core` — same pattern as `ICryptoHoldingsReader`.

## Complexity Tracking

No constitution violations. New module is mandatory per Constitution Principle I (brokers = distinct module). No new NuGet packages. `IBrokerageHoldingsReader` in Core is a minimal shared contract — not a layering violation.
