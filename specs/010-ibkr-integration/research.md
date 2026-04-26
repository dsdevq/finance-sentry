# Research: IBKR Integration

**Feature**: 010-ibkr-integration  
**Date**: 2026-04-22  
**Status**: Complete — all NEEDS CLARIFICATION resolved

---

## Decision 1: IBKR API Selection

**Decision**: Use the **IBKR Client Portal API** (REST) via the self-hosted **IB Gateway** Docker container.

**Rationale**:
- Finance Sentry runs in Docker Compose; adding `ibkr-gateway` as a sidecar service is natural and consistent with the existing infrastructure.
- No developer registration required with IBKR (unlike Web API OAuth).
- REST over HTTPS on the local Docker network — same pattern as Binance.
- The gateway handles IBKR's proprietary session management, leaving Finance Sentry to make simple REST calls.
- No need for a running desktop app (unlike TWS API).

**Alternatives considered**:
- *IBKR Web API (OAuth 2.0)*: Requires developer registration, OAuth redirect flow — too complex for a self-hosted personal tool with no redirect callback.
- *TWS API (socket)*: Requires Trader Workstation running locally as a desktop app; incompatible with headless Docker deployment.

---

## Decision 2: IB Gateway Docker Image

**Decision** (revised 2026-04-26): Use **`ghcr.io/voyz/ibeam:latest`**.

**Original choice was `ghcr.io/gnzsnz/ib-gateway` — wrong product.** That image runs the desktop IB Gateway / TWS app headlessly via IBC + Xvfb and exposes the **Trading API** on port 4001/4002. It does not serve the **Client Portal Web API** (the REST endpoints under `/v1/api/iserver/...` that `IBKRGatewayClient` calls). Those two are different IBKR products despite confusingly similar naming.

**Why IBeam**:
- Wraps IBKR's official Java **Client Portal Gateway** (the REST one).
- Handles the otherwise-interactive browser login automatically via Selenium/Chromium driven by env-var credentials (`IBEAM_ACCOUNT`, `IBEAM_PASSWORD`).
- Auto-re-authenticates every ~24h when IBKR's session expires.
- Free, MIT-licensed, actively maintained.
- Exposes the Client Portal REST surface on `:5000` over **HTTPS with a self-signed cert**.

**Compose wiring**:
- Service name: `ibkr-gateway` (so the existing backend config `IBKR:GatewayBaseUrl=https://ibkr-gateway:5000` still resolves).
- Host port: `5055 → container 5000` (debug only; backend talks container-to-container).
- Backend `HttpClient<IBKRGatewayClient>` accepts the self-signed cert in dev via `ServerCertificateCustomValidationCallback = DangerousAcceptAnyServerCertificateValidator` gated on `IBKR:AllowSelfSignedCert=true` or `IsDevelopment()`.
- IBKR creds live in `docker/.env` as `IBKR_ACCOUNT` / `IBKR_PASSWORD`; blank values are tolerated (IBeam keeps retrying; the rest of the stack runs unaffected).

---

## Decision 3: Credential Model

**Decision**: Finance Sentry stores the user's IBKR **username** + **password**, each encrypted separately with AES-256-GCM (same `ICredentialEncryptionService` pattern as Binance). The `AccountId` (discovered on first connect from the gateway's `/iserver/accounts` response) is stored in plaintext on the credential record.

**Rationale**:
- Consistent with the Binance pattern (two separate encrypted byte arrays).
- The gateway is stateless from Finance Sentry's perspective — each sync session re-authenticates using stored credentials.
- `AccountId` is not a secret (it's the user's account number visible in the IBKR portal).

**Why not gateway-level credential injection**: Storing per-user credentials in Finance Sentry allows the multi-user model described in the spec (each user links their own IBKR account). Baking credentials into Docker env vars would only support a single hardcoded user.

---

## Decision 4: Key API Endpoints

| Purpose | Endpoint | Notes |
|---------|----------|-------|
| Authenticate | `POST /v1/api/iserver/auth/ssodh/init` | Initiates auth with username/password |
| Check auth status | `GET /v1/api/iserver/auth/status` | Returns `{"authenticated":true}` when session is live |
| List accounts | `GET /v1/api/iserver/accounts` | Returns account IDs for the authenticated user |
| Get positions | `GET /v1/api/portfolio/{accountId}/positions/0` | Page 0; paginate with `/positions/1` etc. |
| Keepalive (session) | `POST /v1/api/tickle` | Called every ~60 s to prevent session expiry |

**Position data**: Each position record includes `conid` (contract ID), `contractDesc` (symbol), `assetClass` (STK, OPT, FUT, BOND, FUND, etc.), `position` (quantity), `mktPrice`, `mktValue` (USD market value).

**USD value source**: `mktValue` field — IBKR reports this in USD for all positions regardless of the position's native currency. No separate price ticker call needed (unlike Binance).

---

## Decision 5: Module and Namespace

**Decision**: New module `FinanceSentry.Modules.BrokerageSync`. This name is deliberately generic (not `IBKRSync`) to allow future brokerage adapters (Schwab, Fidelity, etc.) under the same module. IBKR is the first concrete `IBrokerAdapter` implementation.

**Domain interface**: `IBrokerAdapter` (constitution Principle I — external integrations must expose a domain-defined interface). Mirrors `ICryptoExchangeAdapter` from the Binance module.

**Cross-module contract**: `IBrokerageHoldingsReader` in `FinanceSentry.Core.Interfaces` — same pattern as `ICryptoHoldingsReader`. Consumed by `WealthAggregationService` to surface the `"brokerage"` category.

---

## Decision 6: WealthAggregationService Integration

**Decision**: Add `IBrokerageHoldingsReader?` as a second optional injection alongside `ICryptoHoldingsReader?` in `WealthAggregationService`. Filter condition: `category is null || category == "brokerage"`.

**Why optional**: Preserves backward compatibility if the BrokerageSync module is not registered (e.g., in tests or a deployment without IBKR configured).

---

## Decision 7: Session Management

**Decision**: Finance Sentry authenticates with the IB Gateway at the start of each sync cycle (re-authenticate, get a fresh session token, make portfolio calls, let the session expire). No persistent session storage.

**Rationale**: Simpler than managing a long-lived session with keepalive tickles. Each sync is short-lived (one portfolio fetch), so the overhead of re-authentication is negligible at 15-minute intervals.

---

## Decision 8: No New NuGet Packages

**Decision**: No new NuGet packages required. The IBKR Client Portal API is plain REST+JSON; `System.Net.Http.HttpClient` + `Newtonsoft.Json` (already in the project) are sufficient. No official IBKR SDK exists for .NET.
