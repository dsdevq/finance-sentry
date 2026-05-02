# Data Model: Net Worth History (015)

## Entity: NetWorthSnapshot

**Module**: `FinanceSentry.Modules.NetWorthHistory`
**Table**: `net_worth_snapshots`
**DbContext**: `NetWorthHistoryDbContext`

### Fields

| Column | C# Type | PG Type | Nullable | Default | Notes |
|---|---|---|---|---|---|
| `id` | `Guid` | `uuid` | No | `gen_random_uuid()` | PK |
| `user_id` | `string` | `varchar(450)` | No | — | Cross-context reference to `AspNetUsers.Id` |
| `snapshot_date` | `DateOnly` | `date` | No | — | Last calendar day of the month (e.g., `2026-04-30`) |
| `banking_total` | `decimal` | `numeric(18,2)` | No | `0` | Sum of all active bank account balances in USD |
| `brokerage_total` | `decimal` | `numeric(18,2)` | No | `0` | Sum of all brokerage holding values in USD |
| `crypto_total` | `decimal` | `numeric(18,2)` | No | `0` | Sum of all crypto holding values in USD |
| `total_net_worth` | `decimal` | `numeric(18,2)` | No | `0` | Pre-computed: banking + brokerage + crypto |
| `currency` | `string` | `varchar(3)` | No | `'USD'` | Always `USD` in v1 |
| `taken_at` | `DateTimeOffset` | `timestamptz` | No | `now()` | Wall-clock time when snapshot was taken |

### Indexes

```sql
-- Primary history query: user's snapshots ordered by date
CREATE INDEX idx_net_worth_snapshot_user_date
  ON net_worth_snapshots (user_id, snapshot_date DESC);

-- Upsert / idempotency key
CREATE UNIQUE INDEX idx_net_worth_snapshot_user_date_unique
  ON net_worth_snapshots (user_id, snapshot_date);
```

### Constraints

- `banking_total >= 0`, `brokerage_total >= 0`, `crypto_total >= 0`
- `total_net_worth = banking_total + brokerage_total + crypto_total` (enforced in application layer, not DB check constraint)
- `currency` is exactly 3 chars
- One snapshot per (user, month-end date) — enforced by unique index

---

## New Core Interface

**Location**: `FinanceSentry.Core/Interfaces/INetWorthSnapshotService.cs`

```csharp
public interface INetWorthSnapshotService
{
    /// <summary>
    /// Persists a net worth snapshot for the given user. Idempotent: if a snapshot
    /// already exists for the same (userId, snapshotDate), it is a no-op.
    /// </summary>
    Task PersistSnapshotAsync(
        string userId,
        NetWorthSnapshotData data,
        CancellationToken ct = default);

    /// <summary>
    /// Returns true if a snapshot already exists for the current calendar month.
    /// Used by the first-sync trigger to avoid duplicate snapshots.
    /// </summary>
    Task<bool> HasSnapshotForCurrentMonthAsync(
        string userId,
        CancellationToken ct = default);
}

public record NetWorthSnapshotData(
    DateOnly SnapshotDate,
    decimal BankingTotal,
    decimal BrokerageTotal,
    decimal CryptoTotal,
    string Currency = "USD");
```

---

## New BankSync Job

**Location**: `FinanceSentry.Modules.BankSync/Infrastructure/Jobs/NetWorthSnapshotJob.cs`

Responsibilities:
1. Accept optional `userId` parameter (null = all users)
2. Enumerate users: distinct `UserId` values from active `BankAccounts` table
3. For each user: call `IAggregationService.GetTotalNetWorthUsdAsync` (banking)
4. Call `ICryptoHoldingsReader.GetHoldingsAsync` → sum `UsdValue` (crypto)
5. Call `IBrokerageHoldingsReader.GetHoldingsAsync` → sum `UsdValue` (brokerage)
6. Compute `snapshotDate = DateOnly.FromDateTime(DateTime.UtcNow)` rounded to month-end
7. Call `INetWorthSnapshotService.PersistSnapshotAsync`

---

## New BankSync Event Handler

**Location**: `FinanceSentry.Modules.BankSync/Application/EventHandlers/FirstSyncSnapshotTrigger.cs`

```csharp
public class FirstSyncSnapshotTrigger(
    IBankAccountRepository accounts,
    INetWorthSnapshotService snapshotService,
    IBackgroundJobClient jobClient)
    : INotificationHandler<AccountSyncCompletedEvent>
{
    // On successful sync: look up userId from accountId,
    // check HasSnapshotForCurrentMonthAsync,
    // if none: jobClient.Enqueue(() => snapshotJob.ExecuteForUserAsync(userId, ct))
}
```

---

## Existing Entities Referenced (no schema change)

| Entity | Module | Usage |
|---|---|---|
| `BankAccount` | BankSync | Source banking balance via `IAggregationService`; enumerate active users |
| `CryptoHolding` | CryptoSync | Source crypto balance via `ICryptoHoldingsReader` (Core interface) |
| `BrokerageHolding` | BrokerageSync | Source brokerage balance via `IBrokerageHoldingsReader` (Core interface) |
| `ApplicationUser` | Auth | User identity (`BaseCurrency` — not used in v1; USD assumed) |
