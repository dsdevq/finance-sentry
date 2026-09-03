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

## Slice 2 — US3 (dashboard split: Spent / Supported family / Kept)

**Surface**: Angular frontend dashboard, backend `DashboardQueryService` (new bucket).

**Files touched**: `DashboardQueryService.cs` (add counterparty expense bucket),
`dashboard.component.*`, `dashboard.store.*`, new Playwright test.

---

## Slice 3 — US3 completion (invested bucket + one shared classification, this PR)

**Surface**: BankSync application services + queries, one data migration, dashboard store
and component, Playwright + Vitest specs.

**Files touched**:
- `BankSync/Domain/FlowRoles.cs` — new: `family_support` / `investment`
- `BankSync/Application/Services/CounterpartyClassificationService.cs` — `ClassifyForWindowAsync`
- `BankSync/Application/Services/MoneyFlowStatisticsService.cs` — takes the classification,
  adds `InvestedOutflowUsd`, role-aware merge
- `BankSync/Application/Services/MerchantCategoryStatisticsService.cs` — takes the
  classification, counts only `family_support` as spend
- `BankSync/Application/Services/DashboardQueryService.cs` — classifies once, passes to both
- `BankSync/Application/Queries/GetMoneyFlowStatisticsQuery.cs`, `GetTopCategoriesQuery.cs`
- `BankSync/Migrations/20260903000000_M012_InvestmentRoutingCounterparty.cs` — data-only seed
- frontend `dashboard.model.ts`, `dashboard.computed.ts`, `dashboard.component.ts`

**Load-bearing decisions**

- **The flow ROLE decides where a netted movement lands.** `family_support` net expense joins
  Outflow (it is spending and must drag the savings rate down). `investment` net expense stays
  OUT of Outflow — the money is still the user's, it only changed sleeve — and is carved out of
  the surplus instead. Folding it into spend would understate the savings rate by exactly the
  amount that was saved.
- **`Kept = inflow − outflow − invested`**, clamped at zero, so the four buckets partition the
  month's income rather than overlapping it.
- **The classification is computed once per request** by `ClassifyForWindowAsync` and passed as
  a required parameter to both readers. Required, not optional-with-a-fallback, so the compiler
  stops a future caller from silently re-classifying and forming a second opinion about the
  same month.
- **Investment venues are matched with the counterparty engine**, not a second mechanism: an
  `investment`-role system counterparty seeded in M012 with rules for Binance and Interactive
  Brokers. Precedent: the M011 family-support seeds.

**Constraints discovered**

- M011 shipped without a `[Migration]` attribute (hand-written, no designer file), so EF never
  discovered it and the counterparty tables were never created. Fixed here — hand-written
  migrations in this module must carry `[DbContext]` + `[Migration]` inline.
- Playwright's html reporter wipes its output folder, and it was pointed at the same directory
  as the json report's `outputFile`, so `playwright-report/results.json` was deleted at the end
  of every run. The html report now lives in `playwright-report/html`.
