# Data Model: Financial Aggregation and Wealth Overview API

**Feature**: `008-wealth-aggregation-api` | **Date**: 2026-04-20

---

## No New Database Entities

Feature 008 is purely a read layer over existing data. No new tables, columns, or migrations are required.

**Depends on**:
- `BankAccount` — existing entity; `Provider` column added by feature 007 (T003)
- `Transaction` — existing entity; no modifications

---

## Read Models (Response DTOs — not persisted)

### `WealthSummaryResponse`

Top-level response for `GET /api/v1/wealth/summary`.

| Field | Type | Description |
|-------|------|-------------|
| `totalNetWorth` | `decimal` | Sum of all account balances converted to base currency |
| `baseCurrency` | `string` | Always `"USD"` in this feature |
| `categories` | `CategorySummaryDto[]` | One entry per provider category present in the result |
| `appliedFilters` | `AppliedFiltersDto` | Echo of the filters used in this request |

### `CategorySummaryDto`

| Field | Type | Description |
|-------|------|-------------|
| `name` | `string` | Category name: `"banking"`, `"crypto"`, `"brokerage"`, `"other"` |
| `totalInBaseCurrency` | `decimal` | Sum of account balances in this category, converted to USD |
| `accounts` | `AccountBalanceDto[]` | Individual accounts contributing to this category |

### `AccountBalanceDto`

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | `BankAccount.Id` |
| `bankName` | `string` | `BankAccount.BankName` |
| `accountType` | `string` | `BankAccount.AccountType` |
| `accountNumberLast4` | `string` | `BankAccount.AccountNumberLast4` |
| `provider` | `string` | `BankAccount.Provider` (e.g., `"plaid"`, `"monobank"`) |
| `category` | `string` | Derived from provider via `ProviderCategoryMapper` |
| `currency` | `string` | Native currency (e.g., `"UAH"`, `"USD"`) |
| `nativeBalance` | `decimal?` | `BankAccount.CurrentBalance` — null if not yet synced |
| `balanceInBaseCurrency` | `decimal?` | `nativeBalance` converted to USD via static rate table; null if `nativeBalance` is null |
| `syncStatus` | `string` | `BankAccount.SyncStatus` |

### `AppliedFiltersDto`

| Field | Type | Description |
|-------|------|-------------|
| `category` | `string?` | The `category` query param used, or null |
| `provider` | `string?` | The `provider` query param used, or null |

---

### `TransactionSummaryResponse`

Top-level response for `GET /api/v1/wealth/transactions/summary`.

| Field | Type | Description |
|-------|------|-------------|
| `from` | `string` | ISO 8601 date — start of window |
| `to` | `string` | ISO 8601 date — end of window |
| `totalDebits` | `decimal` | Sum of all `debit` transactions in window (native currencies mixed — see note) |
| `totalCredits` | `decimal` | Sum of all `credit` transactions in window |
| `netFlow` | `decimal` | `totalCredits - totalDebits` |
| `categories` | `TransactionCategoryDto[]` | Breakdown by provider category |
| `appliedFilters` | `AppliedFiltersDto` | Echo of filters used |

> **Note on currency**: Transaction summary totals are in mixed native currencies unless filtered to a single currency. In this feature, totals are summed without conversion (i.e., if you have UAH and USD transactions, they are summed as-is). Currency-aware aggregation (converting to USD) is deferred to a future feature when multi-currency transaction totals become a UI requirement.

### `TransactionCategoryDto`

| Field | Type | Description |
|-------|------|-------------|
| `category` | `string` | Category name |
| `totalDebits` | `decimal` | Debits in this category |
| `totalCredits` | `decimal` | Credits in this category |
| `netFlow` | `decimal` | Net flow in this category |
| `transactionCount` | `int` | Number of transactions in this category |

---

## Application-Layer Services (No DB Equivalent)

### `ProviderCategoryMapper`

Static mapping from `BankAccount.Provider` string to category string.

```
"plaid"    → "banking"
"monobank" → "banking"
"binance"  → "crypto"
"ibkr"     → "brokerage"
null / ""  → "other"
(unknown)  → "other"
```

### `CurrencyConverter`

Static rate table for converting native balance to USD.

| Currency | Rate to USD |
|----------|-------------|
| USD      | 1.00        |
| EUR      | 1.08        |
| GBP      | 1.27        |
| UAH      | 0.024       |
| (other)  | 1.00 (pass-through, no conversion) |

---

## Query Parameters

### `GET /api/v1/wealth/summary` query params

| Param | Type | Required | Validation |
|-------|------|----------|------------|
| `category` | `string` | No | One of: `banking`, `crypto`, `brokerage`, `other` |
| `provider` | `string` | No | Free-string (e.g., `monobank`, `plaid`) — no enum enforcement; unknown returns empty |

### `GET /api/v1/wealth/transactions/summary` query params

| Param | Type | Required | Validation |
|-------|------|----------|------------|
| `from` | `string` (ISO date) | Yes | `yyyy-MM-dd`; must be ≤ `to` |
| `to` | `string` (ISO date) | Yes | `yyyy-MM-dd`; must be ≥ `from` |
| `category` | `string` | No | Same as above |
| `provider` | `string` | No | Same as above |
