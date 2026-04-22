# Data Model: IBKR Integration

**Branch**: `010-ibkr-integration` | **Date**: 2026-04-22

---

## New Module: FinanceSentry.Modules.BrokerageSync

All new entities live in the `BrokerageSync` module's own `BrokerageSyncDbContext`. No changes to `BankSyncDbContext` or `CryptoSyncDbContext`.

---

## Entity: IBKRCredential

**Purpose**: Stores encrypted IBKR username and password for a single user, plus the discovered account ID. One record per user.

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `UUID` | PK | |
| `UserId` | `UUID` | NOT NULL, UNIQUE | FK to identity user; unique index enforces one-per-user |
| `EncryptedUsername` | `BYTEA` | NOT NULL | AES-256-GCM ciphertext |
| `UsernameIv` | `BYTEA` | NOT NULL | 12-byte GCM nonce |
| `UsernameAuthTag` | `BYTEA` | NOT NULL | 16-byte GCM auth tag |
| `EncryptedPassword` | `BYTEA` | NOT NULL | AES-256-GCM ciphertext |
| `PasswordIv` | `BYTEA` | NOT NULL | 12-byte GCM nonce |
| `PasswordAuthTag` | `BYTEA` | NOT NULL | 16-byte GCM auth tag |
| `KeyVersion` | `INT` | NOT NULL, DEFAULT 1 | Supports future key rotation |
| `AccountId` | `VARCHAR(20)` | NULLABLE | Discovered from IBKR on first connect; stored in plaintext (not a secret) |
| `IsActive` | `BOOL` | NOT NULL, DEFAULT true | Set to false on disconnect |
| `LastSyncAt` | `TIMESTAMP` | NULLABLE | UTC; updated after each successful sync |
| `LastSyncError` | `TEXT` | NULLABLE | Error message from last failed sync |
| `CreatedAt` | `TIMESTAMP` | DEFAULT NOW() | |

**Indexes**:
- `UNIQUE (UserId)` — enforces one IBKR account per user

---

## Entity: BrokerageHolding

**Purpose**: Current snapshot of a single position in a user's brokerage portfolio. One row per `(UserId, Symbol, Provider)`, upserted on each sync.

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `UUID` | PK | |
| `UserId` | `UUID` | NOT NULL | |
| `Symbol` | `VARCHAR(50)` | NOT NULL | Instrument ticker/symbol (e.g. "AAPL", "MSFT") |
| `InstrumentType` | `VARCHAR(20)` | NOT NULL | Asset class reported by IBKR: "STK", "OPT", "FUT", "BOND", "FUND", etc. |
| `Quantity` | `DECIMAL(30,10)` | NOT NULL | Number of units held |
| `UsdValue` | `DECIMAL(20,4)` | NOT NULL | USD market value as reported by IBKR (`mktValue` field) |
| `SyncedAt` | `TIMESTAMP` | NOT NULL | UTC; when this row was last updated |
| `Provider` | `VARCHAR(20)` | NOT NULL, DEFAULT 'ibkr' | Identifies the brokerage |

**Indexes**:
- `UNIQUE (UserId, Symbol, Provider)` — enables upsert by key; ensures one row per position per user per provider
- `(UserId)` — for fast per-user holdings queries

**Upsert semantics**: On each sync, `INSERT … ON CONFLICT (UserId, Symbol, Provider) DO UPDATE SET Quantity, UsdValue, SyncedAt`.

---

## New Interface in FinanceSentry.Core

```csharp
// FinanceSentry.Core/Interfaces/IBrokerageHoldingsReader.cs
public interface IBrokerageHoldingsReader
{
    Task<IReadOnlyList<BrokerageHoldingSummary>> GetHoldingsAsync(Guid userId, CancellationToken ct);
}

public record BrokerageHoldingSummary(
    string Symbol,
    string InstrumentType,
    decimal Quantity,
    decimal UsdValue,
    DateTime SyncedAt,
    string Provider);
```

---

## Modified: WealthAggregationService (BankSync)

`WealthAggregationService` gains an optional `IBrokerageHoldingsReader` dependency alongside the existing optional `ICryptoHoldingsReader`. When building `WealthSummaryResponse`, after grouping bank and crypto data, the service fetches brokerage holdings and adds them as an additional `CategorySummaryDto` under `"brokerage"`.

Each `BrokerageHoldingSummary` is mapped to `AccountBalanceDto`:
- `BankName` = "IBKR"
- `AccountType` = "brokerage"
- `AccountNumberLast4` = first 4 chars of symbol (e.g. "AAPL")
- `Provider` = "ibkr"
- `Category` = "brokerage"
- `Currency` = "USD"
- `NativeBalance` = `Quantity`
- `BalanceInBaseCurrency` = `UsdValue`
- `SyncStatus` = "synced" or "stale" (if `SyncedAt` > 1 hour ago)

---

## Migration

**Migration ID**: `M001_InitialSchema` (first migration in `BrokerageSyncDbContext`)
**Tables created**: `IBKRCredentials`, `BrokerageHoldings`
**No changes to `BankSyncDbContext`**, `CryptoSyncDbContext`, or any existing tables.
