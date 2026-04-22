# Quickstart: Binance Integration Development

**Branch**: `009-binance-integration` | **Date**: 2026-04-21

---

## Prerequisites

- Docker Desktop running
- Full stack healthy: `cd docker && docker compose -f docker-compose.dev.yml up -d`
- Health check: `GET http://localhost:5000/api/v1/health` → `{"status":"healthy"}`

---

## New Project Structure

```
backend/src/
  FinanceSentry.Modules.CryptoSync/    ← NEW
    Domain/
      BinanceCredential.cs
      CryptoHolding.cs
      Interfaces/
        ICryptoExchangeAdapter.cs
        ICryptoHoldingsReader.cs       ← also added to FinanceSentry.Core
      Repositories/
        IRepositories.cs
      Exceptions/
        BinanceException.cs
    Application/
      Commands/
        ConnectBinanceCommand.cs
        DisconnectBinanceCommand.cs
        SyncBinanceHoldingsCommand.cs
      Queries/
        GetCryptoHoldingsQuery.cs
      Services/
        CryptoHoldingsReader.cs        ← implements ICryptoHoldingsReader from Core
    Infrastructure/
      Binance/
        BinanceAdapter.cs              ← implements ICryptoExchangeAdapter
        BinanceHttpClient.cs
        BinanceAdapterModels.cs
        BinanceException.cs
      Persistence/
        CryptoSyncDbContext.cs
        CryptoSyncDbContextFactory.cs
        Repositories/
          Repositories.cs
      Jobs/
        BinanceSyncJob.cs
    API/
      Controllers/
        CryptoController.cs
    Migrations/
      M001_InitialSchema.cs
    CryptoSyncModule.cs

  FinanceSentry.Core/
    Interfaces/
      ICryptoHoldingsReader.cs         ← NEW shared contract

  FinanceSentry.Modules.BankSync/
    Application/Services/
      WealthAggregationService.cs      ← MODIFIED: inject ICryptoHoldingsReader
```

---

## Running Migrations

After implementing the `CryptoSyncDbContext` and domain entities:

```bash
cd backend/src
dotnet ef migrations add M001_InitialSchema \
  --project FinanceSentry.Modules.CryptoSync \
  --startup-project FinanceSentry.API \
  --context CryptoSyncDbContext

dotnet ef database update \
  --project FinanceSentry.Modules.CryptoSync \
  --startup-project FinanceSentry.API \
  --context CryptoSyncDbContext
```

---

## Testing the Connect Flow

```bash
# 1. Register / login to get JWT
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test1234!"}' \
  | jq -r '.accessToken')

# 2. Connect Binance (use Binance Testnet keys for dev)
curl -X POST http://localhost:5000/api/v1/crypto/binance/connect \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"apiKey":"<YOUR_TESTNET_KEY>","apiSecret":"<YOUR_TESTNET_SECRET>"}'

# 3. Query holdings
curl http://localhost:5000/api/v1/crypto/holdings \
  -H "Authorization: Bearer $TOKEN"

# 4. Check wealth summary includes crypto category
curl "http://localhost:5000/api/v1/wealth/summary" \
  -H "Authorization: Bearer $TOKEN"

# 5. Disconnect
curl -X DELETE http://localhost:5000/api/v1/crypto/binance/disconnect \
  -H "Authorization: Bearer $TOKEN"
```

---

## Binance Testnet

Use Binance Spot Testnet for development:
- URL: `https://testnet.binance.vision`
- Generate test keys at: https://testnet.binance.vision/
- Configure in `appsettings.Development.json`: `"Binance:BaseUrl": "https://testnet.binance.vision"`

---

## appsettings Configuration

Add to `appsettings.json` (and `appsettings.Development.json` for testnet overrides):

```json
{
  "Binance": {
    "BaseUrl": "https://api.binance.com",
    "DustThresholdUsd": 0.01,
    "SyncIntervalMinutes": 15,
    "RecvWindowMs": 5000
  }
}
```

---

## Key Invariants

1. API secret is **never** returned in any API response — only the encrypted form is stored.
2. `CryptoHolding` rows are **upserted** by `(UserId, Asset)` — no append-only history.
3. `BinanceSyncJob` skips users with `IsActive = false` credentials (disconnected).
4. `WealthAggregationService` treats `ICryptoHoldingsReader` as optional — if no Binance account is connected, the `"crypto"` category is simply absent from the summary.
5. All Binance API calls use the `X-MBX-APIKEY` header + HMAC-SHA256 signed query parameters — never pass credentials in the request body.
