# Research: Financial Aggregation and Wealth Overview API

**Feature**: `008-wealth-aggregation-api` | **Date**: 2026-04-20

---

## Decision 1: Module Placement — New Module vs. Extend BankSync

**Decision**: Extend `FinanceSentry.Modules.BankSync` with new classes, not a new project.

**Rationale**: Feature 008 reads `BankAccount` and `Transaction` — entities owned by the BankSync module. Creating a separate `Wealth` module would require cross-module data access, which the constitution forbids (modules must communicate through well-defined service boundaries). Adding a dedicated `WealthController` + `IWealthAggregationService` inside BankSync keeps data access local while still being logically distinct from sync concerns.

**Alternatives considered**: New `FinanceSentry.Modules.Wealth` project — rejected because it would require BankSync to expose its repositories as a public service contract, adding complexity without benefit at the current scale.

---

## Decision 2: Relationship to Existing Dashboard Aggregation

**Decision**: New `WealthController` at `/api/v1/wealth/...` endpoints; existing `DashboardController` at `/dashboard/...` is left unchanged.

**Rationale**: `DashboardController.GetAggregated` returns a combined payload (balances + flow + categories + transfers) optimised for a single-screen dashboard load. Feature 008 needs filterable, provider-aware endpoints with different response shapes. Reusing the dashboard endpoint would require breaking changes or complex optional parameters. Two separate controllers for two distinct use cases.

**Alternatives considered**: Extending the existing `/dashboard/aggregated` endpoint with filter params — rejected because it would break the existing contract and mix concerns.

---

## Decision 3: Provider-to-Category Mapping

**Decision**: Static dictionary in a `ProviderCategoryMapper` application service. No new database column.

```
"plaid"    → "banking"
"monobank" → "banking"
"binance"  → "crypto"
"ibkr"     → "brokerage"
```

Unknown providers default to `"other"`.

**Rationale**: The mapping is a business rule, not data. It changes only when new providers are added. A static dictionary is zero-maintenance and instantly extensible. No migration needed.

**Alternatives considered**: `provider_categories` DB table — rejected as over-engineering for a static, low-cardinality lookup.

---

## Decision 4: Provider Field Dependency on Feature 007

**Decision**: Feature 008 depends on feature 007's T003 (`BankAccount.Provider` column). Implementation of 008 must follow 007's Phase 2 foundational tasks.

**Rationale**: `WealthAggregationService` groups accounts by `BankAccount.Provider`. Without that column all accounts return as `null` provider. Rather than adding workarounds, the plan simply orders 008 after 007's foundational phase.

**Fallback**: Until 007 is merged, all existing accounts are effectively `Provider = null`, which maps to `"other"` category. The endpoint still works; the breakdown is just ungrouped.

---

## Decision 5: Currency Conversion Strategy

**Decision**: Static hardcoded exchange rate table in `CurrencyConverter`. Rates are approximate and declared as advisory-only. No external API.

Seed rates (approximate at time of writing):
| From | To USD |
|------|--------|
| EUR  | 1.08   |
| GBP  | 1.27   |
| UAH  | 0.024  |
| USD  | 1.00   |

**Rationale**: Live rates require an external API dependency, error handling, caching, and secrets. The spec explicitly defers live rates. Static rates deliver the feature value (rough net worth in one currency) without the operational complexity.

**Alternatives considered**: Open Exchange Rates API — deferred to a future feature.

---

## Decision 6: Query Architecture — MediatR Queries vs. Direct Service

**Decision**: Use MediatR `IRequest<T>` queries dispatched from the controller, consistent with the existing pattern (`GetAggregatedBalanceQuery`, `GetMoneyFlowStatisticsQuery`).

**Rationale**: Consistent with the entire BankSync Application layer. All existing read paths use MediatR queries. Deviating would create an inconsistency.

---

## Decision 7: Transaction Summary Filtering

**Decision**: Expense/income summary filters transactions via a JOIN with `BankAccount` to apply provider/category filters. The `Transaction` entity has a denormalized `UserId` but no `Provider` field — the join is necessary.

**Rationale**: `Transaction` does not carry provider information directly. A DB-level join on `BankAccount.Provider` is the correct approach. This is a single-query JOIN, not a N+1 pattern.

**Alternatives considered**: Loading all transactions in memory and filtering in LINQ — rejected for large datasets; a proper query service with EF LINQ join is the right approach.

---

## Decision 8: No New NuGet Packages

**Decision**: No new packages required. All implementation uses `System.Linq`, EF Core (existing), and `MediatR` (existing).

---

## Decision 9: Response Shape — Flat vs. Nested

**Decision**: Nested response — categories contain account lists. This allows a single call to return both the summary totals and the breakdown without a second request.

```json
{
  "totalNetWorth": 12500.00,
  "baseCurrency": "USD",
  "categories": [
    {
      "name": "banking",
      "totalInBaseCurrency": 12500.00,
      "accounts": [ ... ]
    }
  ]
}
```

**Alternatives considered**: Flat account list with category field — rejected because it requires client-side grouping.
