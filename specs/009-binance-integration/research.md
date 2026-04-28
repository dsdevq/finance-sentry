# Research: Binance Integration

**Branch**: `009-binance-integration` | **Date**: 2026-04-21

---

## Decision 1: Binance REST API — Endpoint Inventory

**Decision**: Use Binance REST API at `https://api.binance.com`. Authentication via `X-MBX-APIKEY` header (API key) + HMAC-SHA256 signature on query parameters using the secret key. Requests to signed endpoints append `timestamp` (Unix ms) and `signature` query params.

**Endpoints used** (updated 2026-04-26 to span Spot + Funding + Simple Earn):

| Endpoint | Auth | Purpose | Weight |
|---|---|---|---|
| `GET /api/v3/account` | Signed | Spot wallet — free + locked per asset (also used for credential validation) | 20 |
| `POST /sapi/v1/asset/get-funding-asset` | Signed | Funding wallet — fiat deposits, P2P, gift cards | 1 |
| `GET /sapi/v1/simple-earn/flexible/position` | Signed | Simple Earn flexible positions (paginated, `size=100`) | 150 |
| `GET /sapi/v1/simple-earn/locked/position` | Signed | Simple Earn locked-stake positions (paginated, `size=100`) | 150 |
| `GET /api/v3/ticker/price` | None | All symbol prices in one call | 4 |

**Rate limits**: Weight-based — 1200 weight per minute per IP. A full per-user sync costs ≤ 325 weight (20 + 1 + 150 + 150 + 4). The Earn endpoints carry the bulk of the cost; for the typical personal-finance instance with one user every 15 min this is negligible.

**Error body shape**:
```json
{ "code": -1121, "msg": "Invalid symbol." }
```
HTTP 429 = rate limit exceeded. HTTP 418 = IP temporarily banned (after repeated 429s).

**Permission scope**: Read-Only API keys cover Spot account, Funding wallet, and Simple Earn endpoints. If the user scoped the key narrower, individual endpoints return 401/403; the adapter logs a warning per source and continues — Spot remains the source of truth for credential health, so a hard failure there still throws.

**Rationale**: A single Spot call leaves Earn balances invisible — the most common holding pattern for personal users. Funding adds fiat / off-trading-desk balances. The two Simple Earn endpoints capture flexible savings + locked staking. Bulk price fetch (`GET /api/v3/ticker/price` without `symbol`) eliminates N+1 lookups across all aggregated assets.

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

---

## Decision 10: Wallet Aggregation — Spot + Funding + Simple Earn (added 2026-04-26)

**Decision**: `BinanceAdapter.GetHoldingsAsync` fans out to four endpoints in parallel and aggregates per asset symbol before pricing and dust-filtering:

1. `GET /api/v3/account` — Spot.
2. `POST /sapi/v1/asset/get-funding-asset` — Funding wallet.
3. `GET /sapi/v1/simple-earn/flexible/position?size=100&current=1` — Simple Earn flexible.
4. `GET /sapi/v1/simple-earn/locked/position?size=100&current=1` — Simple Earn locked.

For each `(asset, free, locked)` produced by the four sources, the adapter accumulates into a `Dictionary<string,(decimal Free, decimal Locked)>` keyed by asset symbol (case-insensitive). Earn balances treat flexible positions as "free" (redeemable on demand) and locked positions as "locked" (cannot be moved until maturity). Dust filtering runs **after** aggregation so a tiny Spot balance plus a real Earn position aren't accidentally filtered.

**Permission handling**: Each non-Spot source is wrapped in a `SafeFetchAsync<T>(fetcher, label, fallback)` helper that catches `BinanceException` and logs a warning with the failed source label, then returns the fallback (empty list / empty page). Spot stays as the source of truth for credential validity — a hard failure there still throws and the whole sync aborts. This means a key scoped without Earn read still surfaces Spot/Funding holdings instead of nuking the entire sync.

**Pagination**: Earn endpoints are paginated (default page size 10, max 100). The adapter requests `size=100&current=1` — sufficient for personal-finance use (single user, rarely 100+ Earn positions). If a user ever has more, the first page wins; this is a known follow-up if it bites anyone.

**Rationale**:
- Original Spot-only scope produced the symptom that motivated this decision: a real account with funds parked in Earn showed only its dust Spot balance in the UI.
- The web Binance Overview page is exactly this aggregation — Spot + Funding + Earn (+ Futures/Margin/Options for power users). Mirroring it covers the personal-finance happy path.
- Futures (USD-M, Coin-M) and Options live on different hosts (`fapi.binance.com`, `eapi.binance.com`) and use distinct margin/PnL semantics — out of scope until a power user asks. Margin (cross/isolated) is similarly excluded; most personal users don't use leverage.

**Alternatives considered**:
- `GET /sapi/v1/asset/wallet/balance` (per-wallet BTC totals) — gives the right total but loses per-asset detail. Useful only as a sanity-check sum; not a substitute for per-asset aggregation.
- `POST /sapi/v3/asset/getUserAsset` (consolidated user-asset endpoint) — looked promising as a single call, but Binance docs indicate it covers Spot only and excludes Funding + Earn. Not the silver bullet the name suggests.

**Migration path for Futures/Options support**: add a new `BinanceFuturesClient` (different host, identical signing) calling `/fapi/v2/account` (USD-M) and `/dapi/v1/account` (Coin-M); add a new contributor method (`AddFuturesBalances`) to `BinanceHoldingsAggregator`; thread the new responses through `BinanceAdapter.GetHoldingsAsync` alongside the existing four. Estimated half-day work when needed.

### Aggregation/orchestration split

`BinanceAdapter` does HTTP orchestration only: parallel fan-out, `SafeFetchAsync` permission tolerance, hands raw responses to the aggregator. `BinanceHoldingsAggregator` is a pure-function class (no IO, no DI) that takes the four wallet responses + price ticker list + dust threshold and returns the per-asset `CryptoAssetBalance` list. This keeps the aggregator unit-testable with hand-rolled fixtures (no HTTP mocks needed), and limits adapter contract tests to "did we sign and call the right endpoints". Adding a new wallet source = one new contributor method on the aggregator + one new endpoint call in the adapter, rather than a sprawl in a single class.
