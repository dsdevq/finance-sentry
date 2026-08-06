# Data Model: IBKR Integration

**Branch**: `010-ibkr-integration` | **Date**: 2026-04-22

---

## New Module: FinanceSentry.Modules.BrokerageSync

All new entities live in the `BrokerageSync` module's own `BrokerageSyncDbContext`. No changes to `BankSyncDbContext` or `CryptoSyncDbContext`.

---

## Entity: IBKRCredential

**Purpose** (revised 2026-04-26): Records that a user has linked their IBKR portfolio to Finance Sentry, plus the discovered IBKR account ID. **No per-user credentials are stored** — under the single-tenant gateway model the IBeam sidecar holds the IBKR session for the entire deployment using `IBKR_ACCOUNT` / `IBKR_PASSWORD` env vars. One record per user.

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `UUID` | PK | |
| `UserId` | `UUID` | NOT NULL, UNIQUE | FK to identity user; unique index enforces one-per-user |
| `AccountId` | `VARCHAR(20)` | NULLABLE | Discovered from IBKR on first connect via `/iserver/accounts`; not a secret |
| `IsActive` | `BOOL` | NOT NULL, DEFAULT true | Set to false on disconnect |
| `LastSyncAt` | `TIMESTAMP` | NULLABLE | UTC; updated after each successful sync |
| `LastSyncError` | `TEXT` | NULLABLE | Error message from last failed sync |
| `CreatedAt` | `TIMESTAMP` | DEFAULT NOW() | |

**Indexes**:
- `UNIQUE (UserId)` — enforces one IBKR link per user

**Schema history**:
- M001 (2026-04-22): created table with encrypted `(EncryptedUsername, UsernameIv, UsernameAuthTag, EncryptedPassword, PasswordIv, PasswordAuthTag, KeyVersion)` columns under the original multi-tenant design.
- M002 (2026-04-26): dropped all seven encrypted-credential columns when the model collapsed to single-tenant. The seven dropped columns are restored in `Down()` with empty defaults so the migration is reversible.

**Multi-tenant migration (forward, when going public)**: add encrypted `(AccessToken, AccessTokenIv, AccessTokenAuthTag, RefreshToken, RefreshTokenIv, RefreshTokenAuthTag, KeyVersion, ExpiresAt)` columns and switch to IBKR's OAuth Web API. Different shape from the original encrypted username/password columns dropped in M002 — OAuth has refresh-token mechanics that username/password did not.

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
