# Plan: Counterparty Flows (044)

## Architecture

### Core addition

- `CategoryKeys.FamilySupport = "FAMILY_SUPPORT"` — new canonical key for the
  family-support expense bucket.
- `CanonicalCategories.Definitions` gains a "Family Support" entry (sort order 135,
  between LoanPayments=120 and Income=140).

### Domain layer (BankSync module)

- `Counterparty` entity: `Id`, `UserId` (Guid.Empty = system default), `Name`, `FlowRole`
  (string, e.g. "family_support").
- `CounterpartyRule` entity: `Id`, `CounterpartyId` (FK), `MatchType`
  ("description_contains" | "merchant_name_contains"), `Pattern`.
- `ICounterpartyRepository` in `IRepositories.cs`: `GetForUserAsync(userId)` returns
  counterparties where `UserId == userId OR UserId == Guid.Empty`, eagerly loading Rules.

### Infrastructure layer (BankSync module)

- `CounterpartyRepository`: EF Core impl of `ICounterpartyRepository`.
- `BankSyncDbContext`: add `DbSet<Counterparty>` + `DbSet<CounterpartyRule>`, configure
  mappings for both tables in `bank_sync` schema.
- Migration **M011_CounterpartyFlows**: create `counterparties` + `counterparty_rules`
  tables; insert two default counterparties with three rules each; add FAMILY_SUPPORT
  to the `categories` table.

### Application layer (BankSync module)

- `ICounterpartyClassificationService` + `CounterpartyClassificationService`:
  - `ClassifyAsync(userId, transactions, accountCurrencies, ct)`
  - Loads counterparties (including system defaults).
  - For each transaction: tests each rule (description_contains / merchant_name_contains
    case-insensitive). First matching counterparty wins.
  - Groups matched transactions by (counterparty.Name, month).
  - Computes netting per group: netIncomeUsd = max(0, totalCredits − totalDebits);
    netExpenseUsd = max(0, totalDebits − totalCredits).
  - Returns `CounterpartyClassificationResult(MatchedTransactionIds, MonthlyFlows)`.

- `MoneyFlowStatisticsService`: inject `ICounterpartyClassificationService`.
  In `GetMonthlyFlowAsync`:
  1. Classify counterparty transactions first → `matchedIds` + monthly flows.
  2. Transfer-detect only non-matched transactions → `transferIds`.
  3. Normal flow: exclude `matchedIds ∪ transferIds ∪ TRANSFER-category`.
  4. Merge counterparty net income into each month's Inflow, net expense into Outflow.

- `MerchantCategoryStatisticsService`: inject `ICounterpartyClassificationService`.
  In `GetTopCategoriesAsync`:
  1. Classify counterparty transactions → `matchedIds`.
  2. Exclude matched debits from normal category stats.
  3. Sum `netExpenseUsd` across all months and counterparties → one FAMILY_SUPPORT entry.

### Registration

`BankSyncModule.AddBankSyncModule` registers:
- `ICounterpartyRepository` → `CounterpartyRepository`
- `ICounterpartyClassificationService` → `CounterpartyClassificationService`

---

## Slice 1 — US1 + US2 (backend engine, this PR)

**Surface**: `BankSync` module (domain + infra + application), `Core` domain.

**Files touched/created**:
- `Core/Domain/CategoryKeys.cs` — add FamilySupport constant
- `Core/Domain/CanonicalCategories.cs` — add FamilySupport definition
- `BankSync/Domain/Counterparty.cs` — new entity
- `BankSync/Domain/CounterpartyRule.cs` — new entity
- `BankSync/Domain/Repositories/IRepositories.cs` — add ICounterpartyRepository
- `BankSync/Infrastructure/Persistence/Repositories/CounterpartyRepository.cs` — new
- `BankSync/Infrastructure/Persistence/BankSyncDbContext.cs` — DbSets + config
- `BankSync/Migrations/20260902000000_M011_CounterpartyFlows.cs` — migration
- `BankSync/Migrations/BankSyncDbContextModelSnapshot.cs` — updated
- `BankSync/Application/Services/CounterpartyClassificationService.cs` — new
- `BankSync/Application/Services/MoneyFlowStatisticsService.cs` — updated
- `BankSync/Application/Services/MerchantCategoryStatisticsService.cs` — updated
- `BankSync/BankSyncModule.cs` — register new services
- `Tests.Unit/BankSync/Application/CounterpartyClassificationTests.cs` — new

**Constraint**: No management API in this slice. Seeded counterparties via migration.

---

## Slice 2 — US3 (dashboard four-bucket split, next PR)

**Surface**: Angular frontend dashboard, backend `DashboardQueryService` (new bucket).

**Files touched**: `DashboardQueryService.cs` (add counterparty expense bucket),
`dashboard.component.*`, `dashboard.store.*`, new Playwright test.
