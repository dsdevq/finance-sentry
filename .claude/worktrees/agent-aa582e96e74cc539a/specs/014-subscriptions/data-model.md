# Data Model: Subscriptions Detection (014)

## Entity: DetectedSubscription

**Module**: `FinanceSentry.Modules.Subscriptions`
**Table**: `detected_subscriptions`
**DbContext**: `SubscriptionsDbContext`

### Fields

| Column | C# Type | PG Type | Nullable | Default | Notes |
|---|---|---|---|---|---|
| `id` | `Guid` | `uuid` | No | `gen_random_uuid()` | PK |
| `user_id` | `string` | `varchar(450)` | No | — | Cross-context reference to `AspNetUsers.Id` |
| `merchant_name_normalized` | `string` | `varchar(200)` | No | — | Canonicalized key used for deduplication |
| `merchant_name_display` | `string` | `varchar(200)` | No | — | Most frequent raw name, shown to user |
| `cadence` | `string` | `varchar(10)` | No | — | `monthly` or `annual` |
| `average_amount` | `decimal` | `numeric(15,2)` | No | — | Average of last N detected charges |
| `last_known_amount` | `decimal` | `numeric(15,2)` | No | — | Amount of the most recent charge |
| `currency` | `string` | `varchar(3)` | No | — | ISO 4217, from the source transaction |
| `last_charge_date` | `DateOnly` | `date` | No | — | Date of most recent detected charge |
| `next_expected_date` | `DateOnly` | `date` | No | — | Computed: last_charge_date + average_interval |
| `status` | `string` | `varchar(25)` | No | `'active'` | `active`, `dismissed`, `potentially_cancelled` |
| `occurrence_count` | `int` | `integer` | No | `0` | Number of times detected |
| `confidence_score` | `int` | `integer` | No | `0` | Occurrence count at time of detection |
| `category` | `string?` | `varchar(50)` | Yes | `null` | Internal taxonomy key from transaction category |
| `detected_at` | `DateTimeOffset` | `timestamptz` | No | `now()` | When first detected |
| `updated_at` | `DateTimeOffset` | `timestamptz` | No | `now()` | Last detection run update |
| `dismissed_at` | `DateTimeOffset?` | `timestamptz` | Yes | `null` | When user dismissed |

### Indexes

```sql
-- Primary query: user's active subscriptions
CREATE INDEX idx_detected_subscription_user_status
  ON detected_subscriptions (user_id, status);

-- Upsert deduplication key
CREATE UNIQUE INDEX idx_detected_subscription_user_merchant
  ON detected_subscriptions (user_id, merchant_name_normalized);

-- Detection job: find stale entries for potentially_cancelled check
CREATE INDEX idx_detected_subscription_last_charge
  ON detected_subscriptions (user_id, last_charge_date)
  WHERE status = 'active';
```

### State Transitions

```
Detected (active)
   │
   │  user dismisses (FR-006)        1.5× interval passes without charge (FR-009)
   ▼                                  ▼
dismissed                     potentially_cancelled
   │
   │  user restores (FR-007)
   ▼
active
```

- `dismissed` → `active`: user explicitly restores
- `active` → `potentially_cancelled`: automated (detection job)
- `potentially_cancelled` → `active`: next detection run finds a new charge

### Validation Rules

- `cadence` must be `monthly` or `annual`
- `average_amount` and `last_known_amount` must be > 0
- `currency` max 3 chars
- `merchant_name_normalized` and `merchant_name_display` max 200 chars
- `occurrence_count` ≥ 3 (minimum confidence threshold from spec)

---

## New Core Interface

**Location**: `FinanceSentry.Core/Interfaces/ISubscriptionDetectionResultService.cs`

```csharp
public interface ISubscriptionDetectionResultService
{
    Task UpsertDetectedSubscriptionsAsync(
        string userId,
        IReadOnlyList<DetectedSubscriptionData> results,
        CancellationToken ct = default);

    Task MarkStaleAsPotentiallyCancelledAsync(
        string userId,
        CancellationToken ct = default);
}

public record DetectedSubscriptionData(
    string MerchantNameNormalized,
    string MerchantNameDisplay,
    string Cadence,
    decimal AverageAmount,
    decimal LastKnownAmount,
    string Currency,
    DateOnly LastChargeDate,
    DateOnly NextExpectedDate,
    int OccurrenceCount,
    int ConfidenceScore,
    string? Category);
```

---

## New Static Utility: MerchantNameNormalizer

**Location**: `FinanceSentry.Modules.BankSync/Application/Services/MerchantNameNormalizer.cs`

```csharp
public static class MerchantNameNormalizer
{
    public static string Normalize(string? input);
    // 1. Lowercase
    // 2. Strip domain suffixes (.com, .net, .io, .co, .org)
    // 3. Strip "paypal*" prefix
    // 4. Strip leading *, #, and trailing numeric identifiers
    // 5. Trim and collapse whitespace
}
```

---

## New BankSync Job

**Location**: `FinanceSentry.Modules.BankSync/Infrastructure/Jobs/SubscriptionDetectionJob.cs`

Responsibilities:
1. For each active user (distinct user IDs from `BankAccounts` table), get their debits for last 13 months via `ITransactionRepository`
2. Apply `MerchantNameNormalizer.Normalize` for grouping
3. Run interval + variance analysis per group
4. Call `ISubscriptionDetectionResultService.UpsertDetectedSubscriptionsAsync`
5. Call `ISubscriptionDetectionResultService.MarkStaleAsPotentiallyCancelledAsync`

---

## Existing Entities Referenced (no schema change)

| Entity | Module | Usage |
|---|---|---|
| `Transaction` | BankSync | Source data for detection algorithm (Amount, MerchantName, TransactionDate, PostedDate, IsPending, TransactionType) |
| `BankAccount` | BankSync | Enumerate active users for nightly detection run |
| `ApplicationUser` | Auth | Read `BaseCurrency` for subscription currency display |
