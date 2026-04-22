# Research: Binance Integration

**Branch**: `009-binance-integration` | **Date**: 2026-04-21

---

## Decision 1: Binance REST API — Endpoint Inventory

**Decision**: Use Binance REST API at `https://api.binance.com`. Authentication via `X-MBX-APIKEY` header (API key) + HMAC-SHA256 signature on query parameters using the secret key. Requests to signed endpoints append `timestamp` (Unix ms) and `signature` query params.

**Endpoints used**:

| Endpoint | Auth | Purpose | Weight |
|---|---|---|---|
| `GET /api/v3/ping` | None | Health check / connectivity test | 1 |
| `GET /api/v3/account` | Signed | Fetch all asset balances (free + locked) | 20 |
| `GET /api/v3/ticker/price` | None | Current USD price for a single symbol (e.g. BTCUSDT) | 2 |
| `GET /api/v3/ticker/price` (no symbol) | None | All prices in one call | 4 |

**Rate limits**: Weight-based — 1200 weight per minute per IP. A full sync for one user costs ≤ 25 weight (20 for `/account` + 4 for all-prices bulk call), comfortably within limits even for many users.

**Error body shape**:
```json
{ "code": -1121, "msg": "Invalid symbol." }
```
HTTP 429 = rate limit exceeded. HTTP 418 = IP temporarily banned (after repeated 429s).

**Rationale**: Only spot balance retrieval is needed. `/api/v3/account` returns the full spot wallet in a single call. Bulk price fetch (`GET /api/v3/ticker/price` without `symbol`) returns all prices, eliminating N+1 price lookups.

---

## Decision 2: USD Value Calculation

**Decision**: Fetch all prices in one bulk call (`GET /api/v3/ticker/price`). For each held asset:
- If asset is a stablecoin (USDT, USDC, BUSD, DAI, TUSD): treat 1:1 USD.
- If a `{ASSET}USDT` symbol exists in the price map: use `quantity × price`.
- If a `{ASSET}BTC` pair exists: convert via BTC/USDT price.
- Otherwise: USD value = 0 (asset is exotic / no liquid USDT pair).

**Rationale**: Eliminates per-asset API calls. One bulk call returns thousands of prices. BTC bridge covers BNB and most altcoins that lack a direct USDT pair. Exotic assets with no liquid pair are safely zero-valued rather than guessed.

**Alternatives considered**: Dedicated pricing service (CoinGecko, CoinMarketCap) — out of scope; adds external dependency and API key management for a single feature.

---

## Decision 3: Module Placement — New `CryptoSync` Module

**Decision**: Introduce a new `FinanceSentry.Modules.CryptoSync` project in `backend/src/`. This module owns all crypto-exchange domain logic: credentials, holdings snapshots, sync jobs, and the `ICryptoExchangeAdapter` interface.

**Cross-module wealth integration**: Define `ICryptoHoldingsReader` in `FinanceSentry.Core` (the shared contract layer). `CryptoSync` implements it. `WealthAggregationService` (in `BankSync`) injects `ICryptoHoldingsReader` and merges crypto holdings into the wealth summary under the `"crypto"` category.

**Rationale**: Constitution Principle I explicitly names `banks`, `brokers`, and `crypto` as distinct module categories. Crypto holdings (balance snapshots) have a fundamentally different domain model from bank accounts (transactional ledgers) — no transactions, no sync cursor, no Plaid-style link flow. A separate module prevents leakage of crypto concerns into `BankSync` and prepares for future exchanges (Kraken, Coinbase) without touching `BankSync`.

**Alternatives considered**: Adding to `BankSync` — rejected; would force `CryptoHolding` into the same `DbContext` and migrations as `BankAccount`, pollute `IBankProvider` with a non-transactional interface, and violate Constitution Principle I.

---

## Decision 4: Credential Storage Model

**Decision**: Introduce a `BinanceCredential` domain entity with separate encrypted storage for API key and secret. Both are encrypted with AES-256-GCM (reusing `ICredentialEncryptionService` from `FinanceSentry.Infrastructure`). One credential record per user (enforced by unique index on `UserId`).

**Fields**: `Id`, `UserId`, `EncryptedApiKey`, `ApiKeyIv`, `ApiKeyAuthTag`, `EncryptedApiSecret`, `ApiSecretIv`, `ApiSecretAuthTag`, `KeyVersion`, `IsActive`, `LastSyncAt`, `CreatedAt`.

**Rationale**: Symmetric with `MonobankCredential` design. API key and secret are separate secrets with distinct IV/auth-tag pairs, reducing blast radius if one encrypted value is compromised. `KeyVersion` supports future key rotation. Unique-per-user enforces the one-account-per-user constraint from the spec.

**Alternatives considered**: Single encrypted JSON blob — simpler schema but makes key rotation harder and forces decryption to access either field.

---

## Decision 5: CryptoHolding — Snapshot vs Upsert Model

**Decision**: `CryptoHolding` records are **upserted** on each sync: one row per `(UserId, Asset)`, updated in-place. No historical holding records are kept in this feature (historical tracking is deferred). A `SyncedAt` timestamp on each row records when the value was last updated.

**Rationale**: Historical snapshots would grow indefinitely and are not in the current spec's scope. Upsert keeps the holdings table lean (one row per user/asset) and makes the holdings query trivial (no `DISTINCT ON` or window functions needed). Historical price tracking can be layered on in a future feature.

**Alternatives considered**: Append-only snapshots (one row per sync per asset) — richer for history but wasteful for a feature that only needs current state; deferred per spec scope.

---

## Decision 6: ICryptoExchangeAdapter Interface

**Decision**: Define `ICryptoExchangeAdapter` in `CryptoSync.Domain.Interfaces`:

```csharp
public interface ICryptoExchangeAdapter
{
    string ExchangeName { get; }
    Task ValidateCredentialsAsync(string apiKey, string apiSecret, CancellationToken ct = default);
    Task<IReadOnlyList<CryptoAssetBalance>> GetHoldingsAsync(string apiKey, string apiSecret, CancellationToken ct = default);
    Task DisconnectAsync(string apiKey, CancellationToken ct = default);  // revoke/validate nothing on Binance — no-op + local cleanup
}

public record CryptoAssetBalance(string Asset, decimal Free, decimal Locked, decimal UsdValue);
```

`ValidateCredentialsAsync` calls `GET /api/v3/account` with a small `recvWindow`; throws `BinanceException` on auth failure. `DisconnectAsync` is a no-op on Binance's side (Binance has no server-side revocation via REST) — it signals local cleanup.

**Rationale**: Separation of validation (connect-time) and sync (recurring) simplifies the connect flow. Future exchanges (Kraken, Coinbase) implement the same interface without changing business logic.

---

## Decision 7: Sync Job Schedule

**Decision**: Register a Hangfire recurring job `BinanceSyncJob` that fires every 15 minutes (cron: `*/15 * * * *`). On each tick, the job fetches all active `BinanceCredential` records and runs a sync for each. Failed syncs update `BinanceCredential.LastSyncError` but do not cancel future runs.

**Rationale**: 15-minute default is specified in the spec. Hangfire is already configured in the project. Centralized scheduled job avoids per-user job registration complexity.

**Alternatives considered**: Per-user Hangfire delayed jobs — more granular control but adds complexity to the connect/disconnect lifecycle; overkill for a personal app with few users.

---

## Decision 8: Dust Filter Threshold

**Decision**: Filter out holdings where `(Free + Locked) × UsdValue < 0.01 USD` (one cent). This threshold is configurable via `appsettings.json` under `Binance:DustThresholdUsd`. Assets with no known USD price (exotic coins) are excluded from the holdings view (USD value = 0 < threshold).

**Rationale**: Binance accounts often accumulate hundreds of dust balances from trading fees. Filtering at one cent is a safe default that removes noise without hiding meaningful holdings.

---

## Decision 9: Wealth Integration via ICryptoHoldingsReader

**Decision**: Add `ICryptoHoldingsReader` to `FinanceSentry.Core`:

```csharp
public interface ICryptoHoldingsReader
{
    Task<IReadOnlyList<CryptoHoldingSummary>> GetHoldingsAsync(Guid userId, CancellationToken ct);
}

public record CryptoHoldingSummary(string Asset, decimal Quantity, decimal UsdValue, DateTime SyncedAt, string Provider);
```

`CryptoSync` implements `CryptoHoldingsReader`. `WealthAggregationService` (BankSync) injects `ICryptoHoldingsReader` and adds crypto holdings as `AccountBalanceDto` rows under category `"crypto"` in the summary. When no crypto holdings exist, the category is simply omitted.

**Rationale**: Clean cross-module contract via Core — `BankSync` never references `CryptoSync` directly. Follows the same pattern the spec references ("consistent with the existing adapter pattern"). Minimal change to `WealthAggregationService` (add holdings aggregation after bank accounts loop).
