# Data Model: Historical Net Worth Backfill

**Feature**: 016-history-backfill  
**Date**: 2026-05-03

---

## Schema Changes

**None.** This feature reuses the existing `net_worth_snapshots` table (created in feature 015). No new migrations are required.

---

## Existing Entity: NetWorthSnapshot

```
net_worth_snapshots
├── id             UUID        PK
├── user_id        UUID        NOT NULL
├── snapshot_date  DATE        NOT NULL    ← last day of the month
├── banking_total  DECIMAL     NOT NULL
├── brokerage_total DECIMAL    NOT NULL
├── crypto_total   DECIMAL     NOT NULL
├── total_net_worth DECIMAL    NOT NULL    ← computed: banking + brokerage + crypto
├── currency       VARCHAR(3)  NOT NULL    default 'USD'
└── taken_at       TIMESTAMPTZ NOT NULL

UNIQUE(user_id, snapshot_date)
```

The backfill job deletes all rows for a user, then inserts a new set. The unique constraint is unchanged.

---

## New Domain Objects (in-memory only, not persisted)

### ProviderMonthlyBalance

Intermediate value produced by each `IProviderMonthlyHistorySource` implementation. Lives only in job memory during backfill execution — never written to the database.

```
ProviderMonthlyBalance
├── MonthEnd       DateOnly    ← last calendar day of the month
├── TotalUsd       decimal     ← sum of all provider accounts for this month, in USD
└── AssetCategory  string      ← "banking" | "brokerage" | "crypto"
```

---

## New Core Interfaces

### IHistoricalBackfillScheduler

```csharp
// FinanceSentry.Core/Interfaces/IHistoricalBackfillScheduler.cs
public interface IHistoricalBackfillScheduler
{
    void ScheduleForUser(Guid userId);
}
```

### IProviderMonthlyHistorySource

```csharp
// FinanceSentry.Core/Interfaces/IProviderMonthlyHistorySource.cs
public record ProviderMonthlyBalance(
    DateOnly MonthEnd,
    decimal TotalUsd,
    string AssetCategory);  // "banking" | "brokerage" | "crypto"

public interface IProviderMonthlyHistorySource
{
    Task<IReadOnlyList<ProviderMonthlyBalance>> GetMonthlyBalancesAsync(
        Guid userId, CancellationToken ct = default);
}
```

---

## Modified Core Interfaces

### INetWorthSnapshotService (existing, extended)

```csharp
// Adds ReplaceAllSnapshotsAsync alongside existing PersistSnapshotAsync
public interface INetWorthSnapshotService
{
    Task PersistSnapshotAsync(Guid userId, NetWorthSnapshotData data, CancellationToken ct = default);
    Task<bool> HasSnapshotForCurrentMonthAsync(Guid userId, CancellationToken ct = default);

    // NEW
    Task ReplaceAllSnapshotsAsync(
        Guid userId,
        IReadOnlyList<NetWorthSnapshotData> snapshots,
        CancellationToken ct = default);
}
```

### INetWorthSnapshotRepository (existing, extended)

```csharp
public interface INetWorthSnapshotRepository
{
    Task PersistAsync(NetWorthSnapshot snapshot, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, DateOnly snapshotDate, CancellationToken ct = default);
    Task<IReadOnlyList<NetWorthSnapshot>> GetByUserIdAsync(Guid userId, DateOnly? from, DateOnly? to, CancellationToken ct = default);

    // NEW
    Task DeleteAllByUserIdAsync(Guid userId, CancellationToken ct = default);
}
```

---

## Provider History API Shapes (external)

### Binance `/sapi/v1/accountSnapshot?type=SPOT`

```json
{
  "code": 200,
  "msg": "",
  "snapshotVos": [
    {
      "type": "spot",
      "updateTime": 1640966400000,
      "data": {
        "totalAssetOfBtc": "0.12345",
        "balances": [
          { "asset": "BTC", "free": "0.1", "locked": "0.02345" }
        ]
      }
    }
  ]
}
```

The backfill uses `totalAssetOfBtc` × current BTC/USD price **OR** sums `balances` using the existing `BinanceHoldingsAggregator` price map. In practice: the `SnapshotVo.Data.TotalAssetOfBtc` value is BTC-denominated NAV — the simplest path is to sum asset USD values using the same price ticker already fetched by `BinanceAdapter`.

New model in `BinanceAdapterModels.cs`:

```csharp
public sealed record BinanceSnapshotResponse(
    int Code,
    IReadOnlyList<BinanceSnapshotVo> SnapshotVos);

public sealed record BinanceSnapshotVo(
    string Type,
    long UpdateTime,
    BinanceSnapshotData Data);

public sealed record BinanceSnapshotData(
    string TotalAssetOfBtc,
    IReadOnlyList<BinanceBalance> Balances);
```

### IBKR `/v1/api/portfolio/{accountId}/performance`

```json
{
  "nav": {
    "data": [
      { "date": "20240101", "nav": 15234.56 }
    ]
  }
}
```

New model in `IBKRGatewayModels.cs`:

```csharp
public sealed record IBKRPerformanceResponse(IBKRNavData Nav);
public sealed record IBKRNavData(IReadOnlyList<IBKRNavEntry> Data);
public sealed record IBKRNavEntry(string Date, decimal Nav);
```

### Monobank `/personal/statement/{account}/{from}/{to}`

Already modelled as `MonobankTransaction.Balance` (long, kopecks). No new models needed.

---

## Aggregation Logic Summary

```
For each month covered by any provider:
  banking_total  = sum of ProviderMonthlyBalance where AssetCategory == "banking"
  brokerage_total = sum of ProviderMonthlyBalance where AssetCategory == "brokerage"
  crypto_total   = sum of ProviderMonthlyBalance where AssetCategory == "crypto"
  total_net_worth = banking_total + brokerage_total + crypto_total
  snapshot_date  = last calendar day of the month
```

Months where a provider has no data contribute 0 to their respective category.
