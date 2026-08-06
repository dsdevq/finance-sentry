# Data Model: Budgets (013)

## Entity: Budget

**Module**: `FinanceSentry.Modules.Budgets`
**Table**: `budgets`
**DbContext**: `BudgetsDbContext`

### Fields

| Column | C# Type | PG Type | Nullable | Default | Notes |
|---|---|---|---|---|---|
| `id` | `Guid` | `uuid` | No | `gen_random_uuid()` | PK |
| `user_id` | `string` | `varchar(450)` | No | — | Cross-context reference to `AspNetUsers.Id` (no EF FK) |
| `category` | `string` | `varchar(50)` | No | — | Internal normalized key (see taxonomy below) |
| `monthly_limit` | `decimal` | `numeric(15,2)` | No | — | Must be > 0 |
| `currency` | `string` | `varchar(3)` | No | — | ISO 4217, e.g. "USD". Set to user's `BaseCurrency` at creation |
| `created_at` | `DateTimeOffset` | `timestamptz` | No | `now()` | |
| `updated_at` | `DateTimeOffset` | `timestamptz` | No | `now()` | Updated on limit change |

### Indexes

```sql
-- PK
PRIMARY KEY (id)

-- Query path: list user's budgets
CREATE INDEX idx_budget_user_id ON budgets (user_id);

-- Deduplication constraint: one budget per category per user
CREATE UNIQUE INDEX idx_budget_user_category_unique ON budgets (user_id, category);
```

### Validation Rules

- `category` must be one of the 9 internal taxonomy keys (see below); validated in command handler
- `monthly_limit` must be > 0; validated in command handler  
- `currency` max 3 chars; defaults to user's `BaseCurrency`
- One budget per `(userId, category)` pair — enforced by DB constraint, returns `BUDGET_DUPLICATE_CATEGORY` on conflict

### State Transitions

```
Created → Updated (limit changed via edit)
        → Deleted (user removes budget)
```

No soft-delete; hard delete is intentional (FR-003: "removed from the list permanently").

---

## Internal Category Taxonomy

**Defined in**: `FinanceSentry.Modules.Budgets/Domain/CategoryTaxonomy.cs`

| Internal Key | Display Name | Raw Category Samples (Plaid/Monobank) |
|---|---|---|
| `housing` | Housing | "Rent", "Mortgage & Rent", "Home Improvement" |
| `food_and_drink` | Food & Drink | "Food and Drink", "Restaurants", "Coffee Shop", "Groceries" |
| `transport` | Transport | "Travel", "Gas Stations", "Taxi", "Public Transportation", "Parking" |
| `shopping` | Shopping | "Shops", "Clothing", "Electronics", "Sporting Goods" |
| `entertainment` | Entertainment | "Recreation", "Arts and Entertainment", "Games", "Movies and DVDs" |
| `health` | Health & Fitness | "Healthcare", "Pharmacies", "Gyms and Fitness Centers", "Doctor" |
| `utilities` | Utilities | "Utilities", "Electric", "Gas", "Water", "Phone", "Internet" |
| `travel` | Travel | "Airlines and Aviation Services", "Hotels and Motels", "Car Rental" |
| `other` | Other | All unrecognised categories; catch-all |

**Mapping service**: `CategoryNormalizationService.Normalize(rawCategory: string?) → string` — returns `other` for `null` or unrecognised input.

---

## New BankSync Migration (M003)

**Location**: `FinanceSentry.Modules.BankSync/Migrations/`

```sql
-- Performance: composite index for spending-by-category-by-month queries
CREATE INDEX idx_transaction_user_category_date
  ON transactions (user_id, merchant_category, posted_date DESC)
  WHERE is_active = true;
```

This index supports the `GetTransactionSummaryQuery` filter pattern `WHERE user_id = @uid AND posted_date >= @start AND posted_date < @end` with `GROUP BY merchant_category`.

---

## New Interface (Core)

No new Core interfaces needed — cross-module spending query goes through `IMediator.Send(GetTransactionSummaryQuery)` (already defined in BankSync, auto-registered via CQRS assembly scanning).

---

## Existing Entities Referenced (no schema change)

| Entity | Module | Usage |
|---|---|---|
| `ApplicationUser` | Auth | Read `BaseCurrency` when creating a budget |
| `Transaction` | BankSync | Aggregate `Amount` by `MerchantCategory` for spending calculation (via MediatR) |
