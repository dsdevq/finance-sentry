# Research: Budgets (013)

## Decision 1 — Backend module placement

**Decision**: New standalone `FinanceSentry.Modules.Budgets` project with its own `BudgetsDbContext` and migrations.

**Rationale**: Budgets own user-managed data (budget limits per category). Placing them in BankSync would create an unwanted ownership boundary; budgets logically depend on transaction data but are conceptually separate. Follows the same isolation pattern as Alerts (012).

**Alternatives considered**:
- Add to `FinanceSentry.Modules.BankSync` — rejected: Budgets are user preference data, not sync data; would violate single-responsibility.
- Add to `FinanceSentry.Core` — rejected: Core is shared infrastructure, not a domain module.

---

## Decision 2 — Transaction spending query: cross-module access pattern

**Decision**: The Budgets module's `GetBudgetSummaryQuery` handler dispatches `GetTransactionSummaryQuery` (defined in BankSync) via `IMediator.Send()`. MediatR assembly scanning covers all modules; cross-module CQRS queries are already the established pattern in this project. The handler then maps the resulting category totals against the user's budgets.

**Rationale**: `GetTransactionSummaryQuery` already groups transactions by `MerchantCategory` with debit/credit totals and supports date range filtering — exactly what budget spending calculation needs. Reusing it avoids duplicating aggregation logic.

**Alternatives considered**:
- `IBudgetSpendingReader` interface in Core, implemented in BankSync — rejected: adds an interface for a single query that MediatR already handles.
- Direct `BankSyncDbContext` injection into Budgets module — rejected: violates module data ownership.
- Duplicate the SQL query in BudgetsDbContext via a cross-database view — rejected: overcomplicated for this scale.

---

## Decision 3 — Category normalisation strategy

**Decision**: A static `CategoryNormalizationService` in `FinanceSentry.Modules.Budgets` maps raw `MerchantCategory` strings (from Plaid/Monobank) to an internal `BudgetCategory` taxonomy. Normalization happens **at query time** inside the `GetBudgetSummaryQuery` handler when joining budget limits against transaction totals.

**Internal taxonomy (9 categories)**:

| Internal Key | Display Label | Maps from (Plaid examples) |
|---|---|---|
| `housing` | Housing | "Rent", "Mortgage" |
| `food_and_drink` | Food & Drink | "Food and Drink", "Restaurants" |
| `transport` | Transport | "Travel", "Public Transit", "Gas", "Taxi" |
| `shopping` | Shopping | "Shops", "Clothing", "Electronics" |
| `entertainment` | Entertainment | "Recreation", "Movies", "Games" |
| `health` | Health & Fitness | "Healthcare", "Pharmacies", "Gyms" |
| `utilities` | Utilities | "Utilities", "Telecom", "Internet" |
| `travel` | Travel | "Airlines", "Hotels", "Car Rental" |
| `other` | Other | Everything else |

**Rationale**: At-query-time normalization avoids a migration on the Transactions table (raw `MerchantCategory` stays as-is) and lets us refine mappings without a DB migration. The mapping dictionary is a static constant — zero DB reads required.

**Alternatives considered**:
- Normalize at sync time (store normalized category in a new `NormalizedCategory` column) — rejected: requires BankSync migration + re-sync of existing data; mapping corrections would need backfill jobs.
- No normalization — use raw `MerchantCategory` as budget key — rejected: Plaid category strings are provider-specific and inconsistent across versions.

---

## Decision 4 — Budget entity design (no period column)

**Decision**: The `Budget` entity stores only `(UserId, Category, MonthlyLimit, Currency)`. There is no `Period` or `Year/Month` column. Spending for a given month is always calculated on-the-fly from transactions. This satisfies FR-007 (automatic monthly reset) without any scheduled job.

**Rationale**: Budgets are recurring monthly limits, not per-period snapshots. The "reset" is implicit: the spending query always filters by the selected calendar month. Storing budgets with period columns would require creating new rows each month (complexity). Since spending is computed from transaction history (US3: historical view), no pre-computed data is needed.

**Alternatives considered**:
- Store a `BudgetPeriod` snapshot per month — rejected: writes for every budget every month even if nothing changed; adds unnecessary complexity.
- Store a `CurrentSpent` denormalized column — rejected: requires update on every sync; stale when accounts reconnect or transactions are backdated.

---

## Decision 5 — Frontend BudgetsStore scope

**Decision**: Keep `BudgetsStore` **page-scoped** (provided at component level, not root). Unlike `AlertsStore`, there is no global sidebar element that needs budget data.

**Rationale**: Constitution Principle VI.1: "Page-scoped stores are provided at the component via `providers: [Store]`". Budget state is only needed on the `/budgets` page.

**Alternatives considered**:
- Root-scoped — rejected: no global consumer (no badge, no dashboard widget requiring live store state).

---

## Decision 6 — Duplicate budget prevention

**Decision**: DB-level unique constraint on `(user_id, category)` in the `budgets` table, plus application-level check returning `BUDGET_DUPLICATE_CATEGORY` error with 409 status.

**Rationale**: FR-004 requires preventing duplicate budgets for the same category. The DB constraint is the safety net; the application check provides the user-friendly error.

---

## Decision 7 — Currency handling

**Decision**: Budget `Currency` is stored but always set to the user's `BaseCurrency` from `ApplicationUser`. No conversion is performed. Spending query returns transaction amounts in the account's currency — in v1 all amounts are assumed to be in the user's base currency (same assumption as the existing dashboard).

**Rationale**: The spec explicitly defers multi-currency handling. The existing wealth aggregation queries make the same assumption.

---

## Decision 8 — Transaction index for spending queries

**Decision**: Add a composite index `idx_transaction_user_category_date` on `(UserId, MerchantCategory, PostedDate)` in BankSync migration M003. This avoids a full table scan when computing monthly spending per category.

**Rationale**: `GetTransactionSummaryQuery` filters by `UserId`, groups by `MerchantCategory`, and filters by date range — a composite index covering all three columns will significantly improve query performance at scale.

---

## Resolved Unknowns

| Unknown | Resolution |
|---|---|
| How are raw Plaid/Monobank categories normalised? | At query time via static `CategoryNormalizationService` in Budgets module (Decision 3) |
| How does Budgets access transaction data? | MediatR cross-module via `GetTransactionSummaryQuery` (Decision 2) |
| Is category normalisation in BankSync or Budgets? | Budgets module owns the taxonomy; BankSync stores raw strings (Decision 3) |
| Should `Budget` have a period field? | No — spending calculated on-the-fly per calendar month (Decision 4) |
| Is `BudgetsStore` root or page-scoped? | Page-scoped (Decision 5) |
| How to prevent duplicate budgets? | DB unique constraint + 409 application error (Decision 6) |
