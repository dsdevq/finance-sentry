# Tasks: Counterparty Flows (044)

**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)
**GitHub Issue**: #429

---

## [US1, US2] Slice 1 — Counterparty engine (backend)

### Core layer

- [x] Add `CategoryKeys.FamilySupport = "FAMILY_SUPPORT"` to `CategoryKeys.cs`
- [x] Add `new(CategoryKeys.FamilySupport, "Family Support", 135)` to `CanonicalCategories.Definitions`

### BankSync domain

- [x] Create `BankSync/Domain/Counterparty.cs` entity (UserId, Name, FlowRole, Rules nav)
- [x] Create `BankSync/Domain/CounterpartyRule.cs` entity (CounterpartyId, MatchType, Pattern)
- [x] Add `ICounterpartyRepository` to `IRepositories.cs` (`GetForUserAsync`)

### BankSync infrastructure

- [x] Create `CounterpartyRepository.cs` implementing `ICounterpartyRepository`
- [x] Add `DbSet<Counterparty>` + `DbSet<CounterpartyRule>` to `BankSyncDbContext.cs`
- [x] Configure entity mappings in `BankSyncDbContext.OnModelCreating`
- [x] Create migration `20260902000000_M011_CounterpartyFlows.cs`
  - Creates `counterparties` table
  - Creates `counterparty_rules` table
  - Inserts FAMILY_SUPPORT into `categories`
  - Seeds Людмила Сичова (rules: "Людмила Сичова", "мама") with `UserId = Guid.Empty`
  - Seeds Єлизавета Морозова (rules: "Єлизавета Морозова", "Ліза") with `UserId = Guid.Empty`
- [x] Update `BankSyncDbContextModelSnapshot.cs` to include new entities

### BankSync application

- [x] Create `CounterpartyClassificationService.cs` (interface + implementation)
  - `ClassifyAsync`: loads counterparties, matches transactions, nets per month
- [x] Update `MoneyFlowStatisticsService.cs` to inject and apply classification
- [x] Update `MerchantCategoryStatisticsService.cs` to inject and apply classification

### Registration

- [x] Register `ICounterpartyRepository → CounterpartyRepository` in `BankSyncModule.cs`
- [x] Register `ICounterpartyClassificationService → CounterpartyClassificationService` in `BankSyncModule.cs`

### Tests

- [x] Create `Tests.Unit/BankSync/Application/CounterpartyClassificationTests.cs`
  - Match by description_contains
  - Match by merchant_name_contains
  - No match returns empty set
  - Netting: credits dominate → income, zero expense
  - Netting: debits dominate → expense, zero income
  - Netting: equal credits and debits → both zero
  - Multi-counterparty in same month
  - Multi-month window produces per-month results
  - First-match wins when multiple rules could match

### Quality gates

- [x] `dotnet build FinanceSentry.sln --no-restore -c Release` → zero warnings
- [x] `dotnet test FinanceSentry.sln --filter "Category!=Integration"` → all pass (553 unit tests, 0 failed)

---

## [US3] Slice 2 — Dashboard four-bucket split (this PR)

- [x] Update `MonthlyFlow` record + `MoneyFlowStatisticsService` to return `FamilySupportOutflowUsd`
- [x] Update dashboard Angular component with four-bucket breakdown (`@if hasFamilySupport`)
- [x] Update `DashboardStore` computed signals: `monthlySpentFormatted`, `monthlyFamilySupportFormatted`, `monthlyKeptFormatted`, `hasFamilySupport`
- [x] Playwright test: four buckets visible; "supported family" matches backend mock ($800)
