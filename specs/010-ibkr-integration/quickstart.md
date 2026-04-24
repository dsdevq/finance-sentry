# Quickstart: IBKR Integration Development

**Branch**: `010-ibkr-integration` | **Date**: 2026-04-22

---

## Prerequisites

- Docker Desktop running
- Full stack healthy: `cd docker && docker compose -f docker-compose.dev.yml up -d`
- Health check: `GET http://localhost:5000/api/v1/health` → `{"status":"healthy"}`
- IB Gateway container reachable at `http://ibkr-gateway:5000` on the Docker internal network

---

## New Project Structure

```
backend/src/
  FinanceSentry.Modules.BrokerageSync/    ← NEW
    Domain/
      IBKRCredential.cs
      BrokerageHolding.cs
      Interfaces/
        IBrokerAdapter.cs
      Repositories/
        IRepositories.cs
      Exceptions/
        BrokerAuthException.cs
    Application/
      Commands/
        ConnectIBKRCommand.cs
        DisconnectIBKRCommand.cs
        SyncIBKRHoldingsCommand.cs
      Queries/
        GetBrokerageHoldingsQuery.cs
      Services/
        BrokerageHoldingsReader.cs       ← implements IBrokerageHoldingsReader from Core
    Infrastructure/
      IBKR/
        IBKRAdapter.cs                   ← implements IBrokerAdapter
        IBKRGatewayClient.cs
        IBKRGatewayModels.cs
      Persistence/
        BrokerageSyncDbContext.cs
        BrokerageSyncDbContextFactory.cs
        Repositories/
          Repositories.cs
      Jobs/
        IBKRSyncJob.cs
    API/
      Controllers/
        BrokerageController.cs
    Migrations/
      M001_InitialSchema.cs
    BrokerageSyncModule.cs

  FinanceSentry.Core/
    Interfaces/
      IBrokerageHoldingsReader.cs        ← NEW shared contract

  FinanceSentry.Modules.BankSync/
    Application/Services/
      WealthAggregationService.cs        ← MODIFIED: inject IBrokerageHoldingsReader
```

---

## IB Gateway Docker Service

Add to `docker/docker-compose.dev.yml`:

```yaml
ibkr-gateway:
  image: ghcr.io/gnzsnz/ib-gateway:latest
  environment:
    IBKR_USERNAME: ${IBKR_USERNAME}
    IBKR_PASSWORD: ${IBKR_PASSWORD}
    TRADING_MODE: paper          # "paper" for dev, "live" for production
    TWS_SETTINGS_PATH: /root/Jts
    VNC_SERVER_PASSWORD: ${VNC_PASSWORD:-changeme}
  ports:
    - "5001:5000"                # expose on host port 5001 to avoid conflict with API
  volumes:
    - ibkr-settings:/root/Jts
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:5000/v1/api/iserver/auth/status"]
    interval: 30s
    timeout: 10s
    retries: 5
```

Finance Sentry calls the gateway at `http://ibkr-gateway:5000/v1/api/...` on the internal Docker network. The host-mapped port `5001` is for manual testing only.

---

## Running Migrations

After implementing `BrokerageSyncDbContext` and domain entities:

```bash
cd backend/src
dotnet ef migrations add M001_InitialSchema \
  --project FinanceSentry.Modules.BrokerageSync \
  --startup-project FinanceSentry.API \
  --context BrokerageSyncDbContext

dotnet ef database update \
  --project FinanceSentry.Modules.BrokerageSync \
  --startup-project FinanceSentry.API \
  --context BrokerageSyncDbContext
```

---

## Testing the Connect Flow

```bash
# 1. Register / login to get JWT
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test1234!"}' \
  | jq -r '.accessToken')

# 2. Connect IBKR (use IBKR Paper Trading credentials)
curl -X POST http://localhost:5000/api/v1/brokerage/ibkr/connect \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"username":"<IBKR_USERNAME>","password":"<IBKR_PASSWORD>"}'

# 3. Query brokerage holdings
curl http://localhost:5000/api/v1/brokerage/holdings \
  -H "Authorization: Bearer $TOKEN"

# 4. Check wealth summary includes brokerage category
curl "http://localhost:5000/api/v1/wealth/summary" \
  -H "Authorization: Bearer $TOKEN"

# 5. Filter wealth summary to brokerage only
curl "http://localhost:5000/api/v1/wealth/summary?category=brokerage" \
  -H "Authorization: Bearer $TOKEN"

# 6. Disconnect
curl -X DELETE http://localhost:5000/api/v1/brokerage/ibkr/disconnect \
  -H "Authorization: Bearer $TOKEN"
```

---

## IBKR Paper Trading

Use IBKR Paper Trading account for development:
- Paper accounts are free with any IBKR account
- Log in at https://www.interactivebrokers.com/en/trading/paper-trading.php
- Set `TRADING_MODE=paper` in the gateway container environment

---

## appsettings Configuration

Add to `appsettings.json`:

```json
{
  "IBKR": {
    "GatewayBaseUrl": "http://ibkr-gateway:5000",
    "SyncIntervalMinutes": 15
  }
}
```

For local development without Docker networking, override in `appsettings.Development.json`:

```json
{
  "IBKR": {
    "GatewayBaseUrl": "http://localhost:5001"
  }
}
```

---

## Key Invariants

1. IBKR password is **never** returned in any API response — only the encrypted form is stored.
2. `BrokerageHolding` rows are **upserted** by `(UserId, Symbol, Provider)` — no append-only history.
3. `IBKRSyncJob` skips users with `IsActive = false` credentials (disconnected).
4. Finance Sentry **re-authenticates** at the start of each sync cycle — no persistent gateway session.
5. `WealthAggregationService` treats `IBrokerageHoldingsReader` as **optional** — if no IBKR account is connected, the `"brokerage"` category is absent from the summary.
6. `AccountId` is discovered from the gateway on first connect and stored in plaintext — it is not a secret.
7. Positions with `mktValue = 0` (e.g. expired options) are stored with `UsdValue = 0` and included in the response.
