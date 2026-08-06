# Data Model: Alerts System (012)

## Entity: Alert

**Module**: `FinanceSentry.Modules.Alerts`
**Table**: `alerts`
**DbContext**: `AlertsDbContext`

### Fields

| Column | C# Type | PG Type | Nullable | Default | Notes |
|---|---|---|---|---|---|
| `id` | `Guid` | `uuid` | No | `gen_random_uuid()` | PK |
| `user_id` | `string` | `varchar(450)` | No | — | FK → `AspNetUsers.Id` (no EF FK; cross-context reference by value) |
| `type` | `string` | `varchar(30)` | No | — | `LowBalance`, `SyncFailure`, `UnusualSpend` |
| `severity` | `string` | `varchar(10)` | No | — | `Error`, `Warning`, `Info` |
| `title` | `string` | `varchar(200)` | No | — | Short summary, e.g. "Low balance on Chase Checking" |
| `message` | `string` | `varchar(1000)` | No | — | Human-readable detail |
| `reference_id` | `Guid?` | `uuid` | Yes | `null` | AccountId for LowBalance/SyncFailure; `null` for UnusualSpend with no account |
| `reference_label` | `string?` | `varchar(200)` | Yes | `null` | Account name or spend category label |
| `is_read` | `bool` | `boolean` | No | `false` | Whether user has marked this as read |
| `is_resolved` | `bool` | `boolean` | No | `false` | Auto-resolved by system (e.g. balance recovered) |
| `is_dismissed` | `bool` | `boolean` | No | `false` | Permanently hidden by user action |
| `created_at` | `DateTimeOffset` | `timestamptz` | No | `now()` | When alert was first created |
| `updated_at` | `DateTimeOffset` | `timestamptz` | No | `now()` | Last state change (read/resolve/dismiss) |
| `resolved_at` | `DateTimeOffset?` | `timestamptz` | Yes | `null` | When auto-resolution occurred |

### Indexes

```sql
-- Primary query path: user's active (non-dismissed) alerts, newest first
CREATE INDEX idx_alert_user_created ON alerts (user_id, created_at DESC)
  WHERE is_dismissed = false;

-- Deduplication check: find existing unresolved alert of same type/reference
CREATE UNIQUE INDEX idx_alert_dedup ON alerts (user_id, type, reference_id)
  WHERE is_resolved = false AND is_dismissed = false;

-- Purge job: find old resolved/dismissed alerts
CREATE INDEX idx_alert_purge ON alerts (created_at)
  WHERE is_resolved = true OR is_dismissed = true;
```

**Note on `idx_alert_dedup`**: For UnusualSpend alerts with `reference_id = null`, PostgreSQL's unique index does not treat two `NULL` values as equal. Application layer must check explicitly for `type = 'UnusualSpend'` duplication using a separate query predicate.

### State Transitions

```
                   auto-resolve
Created ──────────────────────────► Resolved (is_resolved=true)
   │                                      │
   │  user marks read                     │  user dismisses
   ▼                                      ▼
 Read (is_read=true)               Dismissed (is_dismissed=true)
   │
   │  user dismisses
   ▼
 Dismissed (is_dismissed=true)
```

- **Resolved** alerts remain visible until dismissed (FR-011)
- **Dismissed** alerts are hidden from all queries; purged after 90 days (FR-012)
- An alert can be read AND resolved simultaneously

### Validation Rules

- `type` must be one of: `LowBalance`, `SyncFailure`, `UnusualSpend`
- `severity` must be one of: `Error`, `Warning`, `Info`
- `title` max 200 chars; `message` max 1000 chars; `reference_label` max 200 chars
- `user_id` max 450 chars (matches ASP.NET Identity default)
- Cannot dismiss an alert that does not belong to the requesting user (enforced in query)

---

## New Interface: IAlertGeneratorService

**Location**: `FinanceSentry.Core/Interfaces/IAlertGeneratorService.cs`

```csharp
public interface IAlertGeneratorService
{
    Task GenerateLowBalanceAlertAsync(string userId, Guid accountId, string accountName,
        decimal balance, decimal threshold, CancellationToken ct = default);

    Task ResolveLowBalanceAlertAsync(string userId, Guid accountId,
        CancellationToken ct = default);

    Task GenerateSyncFailureAlertAsync(string userId, string provider, Guid? accountId,
        string? accountName, string? errorCode, CancellationToken ct = default);

    Task ResolveSyncFailureAlertAsync(string userId, string provider, Guid? accountId,
        CancellationToken ct = default);

    Task GenerateUnusualSpendAlertAsync(string userId, string category,
        decimal currentMonthSpend, decimal averageMonthlySpend,
        CancellationToken ct = default);
}
```

---

## Event Extension: AccountSyncCompletedEvent

**Location**: `FinanceSentry.Modules.BankSync/Domain/Events/AccountSyncCompletedEvent.cs`

Extend the existing record to include alert-relevant fields:

```csharp
public record AccountSyncCompletedEvent(
    Guid AccountId,
    string UserId,           // NEW
    string Provider,         // NEW  e.g. "plaid", "monobank"
    string Status,           // existing: "success" | "failed"
    int TransactionCountFetched,
    decimal? BalanceAfterSync, // NEW  null if unavailable
    string? ErrorCode,         // NEW  null on success
    string? ErrorMessage) : IEvent;
```

---

## Existing Entities Referenced (no schema change)

| Entity | Module | Usage |
|---|---|---|
| `ApplicationUser` | Auth | Read `LowBalanceThreshold`, `LowBalanceAlerts`, `SyncFailureAlerts` preferences |
| `BankAccount` | BankSync | Read `Balance`, `Name`, `UserId`, `Provider` for alert context |
| `Transaction` | BankSync | Aggregate by `MerchantCategory` for unusual spend detection |
