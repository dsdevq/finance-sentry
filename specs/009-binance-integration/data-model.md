# Data Model: Binance Integration

**Branch**: `009-binance-integration` | **Date**: 2026-04-21

---

## New Module: FinanceSentry.Modules.CryptoSync

All new entities live in the `CryptoSync` module's own `CryptoSyncDbContext`. No changes to `BankSyncDbContext`.

---

## Entity: BinanceCredential

**Purpose**: Stores an encrypted Binance API key and secret for a single user. One record per user.

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `UUID` | PK | |
| `UserId` | `UUID` | NOT NULL, UNIQUE | FK to identity user; unique index enforces one-per-user |
| `EncryptedApiKey` | `BYTEA` | NOT NULL | AES-256-GCM ciphertext |
| `ApiKeyIv` | `BYTEA` | NOT NULL | 12-byte GCM nonce |
| `ApiKeyAuthTag` | `BYTEA` | NOT NULL | 16-byte GCM auth tag |
| `EncryptedApiSecret` | `BYTEA` | NOT NULL | AES-256-GCM ciphertext |
| `ApiSecretIv` | `BYTEA` | NOT NULL | 12-byte GCM nonce |
| `ApiSecretAuthTag` | `BYTEA` | NOT NULL | 16-byte GCM auth tag |
| `KeyVersion` | `INT` | NOT NULL, DEFAULT 1 | Supports future key rotation |
| `IsActive` | `BOOL` | NOT NULL, DEFAULT true | Set to false on disconnect |
| `LastSyncAt` | `TIMESTAMP` | NULLABLE | UTC; updated after each successful sync |
| `LastSyncError` | `TEXT` | NULLABLE | Error message from last failed sync |
| `CreatedAt` | `TIMESTAMP` | DEFAULT NOW() | |

**Indexes**:
- `UNIQUE (UserId)` — enforces one Binance account per user

---

## Entity: CryptoHolding

**Purpose**: Current snapshot of a user's holding in a single crypto asset. One row per `(UserId, Asset)`, upserted on each sync.

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `UUID` | PK | |
| `UserId` | `UUID` | NOT NULL | |
| `Asset` | `VARCHAR(20)` | NOT NULL | e.g. "BTC", "ETH", "USDT" |
| `FreeQuantity` | `DECIMAL(30,10)` | NOT NULL | Withdrawable balance |
| `LockedQuantity` | `DECIMAL(30,10)` | NOT NULL | Locked in open orders |
| `UsdValue` | `DECIMAL(20,4)` | NOT NULL | `(Free + Locked) × spot price at sync time` |
| `SyncedAt` | `TIMESTAMP` | NOT NULL | UTC; when this row was last updated |
| `Provider` | `VARCHAR(20)` | NOT NULL, DEFAULT "binance" | Identifies the exchange |

**Indexes**:
- `UNIQUE (UserId, Asset)` — enables upsert by key; ensures one row per asset per user
- `(UserId)` — for fast per-user holdings queries

**Upsert semantics**: On each sync, `INSERT … ON CONFLICT (UserId, Asset) DO UPDATE SET FreeQuantity, LockedQuantity, UsdValue, SyncedAt`.

**Wallet aggregation (revised 2026-04-26)**: `FreeQuantity` and `LockedQuantity` are aggregated **across multiple Binance wallets** before being stored:

| Source | Contributes to `FreeQuantity` | Contributes to `LockedQuantity` |
|---|---|---|
| Spot wallet (`/api/v3/account`) | `balance.free` | `balance.locked` |
| Funding wallet (`/sapi/v1/asset/get-funding-asset`) | `entry.free` | `entry.locked` (if present) |
| Simple Earn — flexible (`/sapi/v1/simple-earn/flexible/position`) | `position.totalAmount` (redeemable on demand) | — |
| Simple Earn — locked (`/sapi/v1/simple-earn/locked/position`) | — | `position.amount` (cannot be moved until maturity) |

Aggregation is keyed by asset symbol (case-insensitive). Dust threshold is applied **after** summing across sources. See `research.md` Decision 10.

---

## New Interface in FinanceSentry.Core

```csharp
// FinanceSentry.Core/Interfaces/ICryptoHoldingsReader.cs
public interface ICryptoHoldingsReader
{
    Task<IReadOnlyList<CryptoHoldingSummary>> GetHoldingsAsync(Guid userId, CancellationToken ct);
}

public record CryptoHoldingSummary(
    string Asset,
    decimal FreeQuantity,
    decimal LockedQuantity,
    decimal UsdValue,
    DateTime SyncedAt,
    string Provider);
```

---

## Modified: WealthAggregationService (BankSync)

`WealthAggregationService` gains an optional `ICryptoHoldingsReader` dependency (registered in DI). When building `WealthSummaryResponse`, after grouping bank accounts by category, the service fetches crypto holdings and adds them as an additional `CategorySummaryDto` under `"crypto"`.

Each `CryptoHoldingSummary` is mapped to `AccountBalanceDto`:
- `BankName` = exchange name (e.g. "Binance")
- `AccountType` = "crypto"
- `AccountNumberLast4` = asset symbol (e.g. "BTC")
- `Provider` = "binance"
- `Category` = "crypto"
- `Currency` = "USD" (value already in USD)
- `NativeBalance` = `FreeQuantity + LockedQuantity`
- `BalanceInBaseCurrency` = `UsdValue`
- `SyncStatus` = "synced" (or "stale" if `SyncedAt` > 1 hour ago)

---

## Migration

**Migration ID**: `M001_InitialSchema` (first migration in `CryptoSyncDbContext`)
**Tables created**: `BinanceCredentials`, `CryptoHoldings`
**No changes to `BankSyncDbContext`** or existing tables.
