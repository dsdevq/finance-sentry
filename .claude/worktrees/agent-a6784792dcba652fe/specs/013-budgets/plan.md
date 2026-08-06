# Implementation Plan: Budgets

**Branch**: `013-budgets` | **Date**: 2026-05-02 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/013-budgets/spec.md`

## Summary

Build a monthly budget management feature: users create per-category spending limits; the system calculates how much has been spent in each budget category for the current (or selected) month by aggregating existing transaction data. Backend: new `FinanceSentry.Modules.Budgets` module with `Budget` entity, 5 REST endpoints, and a `CategoryNormalizationService` that maps raw Plaid/Monobank category strings to a fixed 9-item internal taxonomy. Spending is computed cross-module via MediatR (`GetTransactionSummaryQuery` from BankSync). Frontend: the scaffolded budgets page and store already exist with full UI; the task is replacing mock data with real API calls and adding create/delete/edit flows.

## Technical Context

**Language/Version**: C# 13/.NET 9 (backend) · TypeScript 5.x strict / Angular 21.2 (frontend)
**Primary Dependencies**: ASP.NET Core 9, EF Core 9, MediatR · NgRx SignalStore 21.1, @dsdevq-common/ui
**Storage**: PostgreSQL 14 — new `budgets` table in `BudgetsDbContext`; BankSync M003 adds composite index on `(user_id, merchant_category, posted_date)`
**Testing**: xUnit (backend) · Vitest + Playwright (frontend)
**Target Platform**: Linux (Docker) server + Angular SPA
**Project Type**: Web application (modular monolith + SPA)
**Performance Goals**: Budget summary loads within one page render; spending aggregated at query time
**Constraints**: One budget per category per user; spending in user's base currency only; monthly periods only
**Scale/Scope**: Low-volume per-user data (~10–20 budgets max per user)

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Modular Monolith | ✅ | New `FinanceSentry.Modules.Budgets`; cross-module data via MediatR (no direct module coupling) |
| II. Code Quality | ✅ | ESLint gate + zero `dotnet build` warnings enforced per file |
| III. Multi-Source Integration | ✅ | Category normalization covers Plaid and Monobank raw categories |
| IV. AI Analytics | N/A | No AI in this feature |
| V. Security | ✅ | All queries/mutations scoped to `userId` from JWT |
| VI. Frontend State | ✅ | `BudgetsStore` page-scoped; 5-file SignalStore split already scaffolded |
| VI.5 File Organisation | ✅ | Frontend already in canonical layout; backend follows established module patterns |
| Versioning | ✅ | Backend minor version bump for new endpoints; frontend minor version bump |

**Post-design re-check**: No violations. MediatR cross-module query is the established pattern; `CategoryNormalizationService` in Budgets does not create a BankSync dependency.

## Project Structure

### Documentation (this feature)

```text
specs/013-budgets/
├── plan.md              # This file
├── research.md          # Phase 0 decisions
├── data-model.md        # Entity schema + category taxonomy
├── quickstart.md        # Dev setup and verification
├── contracts/
│   └── budgets-rest-api.md
└── tasks.md             # /speckit.tasks output
```

### Source Code

```text
backend/src/
├── FinanceSentry.Modules.Budgets/              [NEW MODULE]
│   ├── API/
│   │   ├── Controllers/
│   │   │   └── BudgetsController.cs
│   │   └── Responses/
│   │       ├── BudgetDto.cs
│   │       ├── BudgetsListResponse.cs
│   │       ├── BudgetSummaryItemDto.cs
│   │       └── BudgetSummaryResponse.cs
│   ├── Application/
│   │   ├── Commands/
│   │   │   ├── CreateBudgetCommand.cs
│   │   │   ├── UpdateBudgetCommand.cs
│   │   │   └── DeleteBudgetCommand.cs
│   │   ├── Queries/
│   │   │   ├── GetBudgetsQuery.cs
│   │   │   └── GetBudgetSummaryQuery.cs
│   │   └── Services/
│   │       ├── ICategoryNormalizationService.cs
│   │       └── CategoryNormalizationService.cs
│   ├── Domain/
│   │   ├── Budget.cs
│   │   ├── CategoryTaxonomy.cs
│   │   └── Repositories/
│   │       └── IBudgetRepository.cs
│   ├── Infrastructure/
│   │   └── Persistence/
│   │       ├── BudgetsDbContext.cs
│   │       ├── BudgetsDbContextFactory.cs
│   │       └── Repositories/
│   │           └── BudgetRepository.cs
│   ├── Migrations/
│   ├── BudgetsModule.cs
│   └── FinanceSentry.Modules.Budgets.csproj
│
├── FinanceSentry.Modules.BankSync/
│   └── Migrations/
│       └── [M003_TransactionCategoryIndex.cs]  [NEW migration]
│
└── FinanceSentry.API/
    ├── Program.cs                              [MODIFY: add BudgetsModule, DbContext, migration, DI]
    └── FinanceSentry.API.csproj               [MODIFY: bump minor version]

frontend/src/app/
├── modules/budgets/
│   ├── models/budget/
│   │   └── budget.model.ts                    [MODIFY: add id, currency, createdAt; add BudgetSummaryItem]
│   ├── services/
│   │   └── budgets.service.ts                 [NEW]
│   ├── store/budgets/
│   │   ├── budgets.state.ts                   [MODIFY: add selectedYear, selectedMonth]
│   │   ├── budgets.computed.ts                [no change]
│   │   ├── budgets.methods.ts                 [MODIFY: add addBudget, updateBudgetLimit, removeBudget]
│   │   ├── budgets.effects.ts                 [MODIFY: replace mock with API; CRUD rxMethods]
│   │   └── budgets.store.ts                   [no change — stays page-scoped]
│   └── pages/budgets/
│       └── budgets.component.ts               [MODIFY: add create/delete form handlers]
└── core/
    └── errors/
        └── error-messages.registry.ts         [MODIFY: add 5 BUDGET_* error codes]
```

## Complexity Tracking

No constitution violations. No complexity tracking required.

---

## Implementation Phases (for /speckit.tasks)

### Phase 1 — Backend foundation

- Scaffold `FinanceSentry.Modules.Budgets` project + csproj + references
- `Budget` domain entity + `CategoryTaxonomy` static class
- `IBudgetRepository` + `BudgetRepository`
- `BudgetsDbContext` + migration M001
- `ICategoryNormalizationService` + `CategoryNormalizationService` (full 9-category mapping)
- Register in `Program.cs`
- BankSync migration M003 (composite transaction index)

### Phase 2 — CRUD endpoints (US1)

- `GetBudgetsQuery` + handler
- `CreateBudgetCommand` + handler (409 on duplicate)
- `UpdateBudgetCommand` + handler
- `DeleteBudgetCommand` + handler
- Response DTOs
- `BudgetsController`
- Contract tests for all 4 CRUD endpoints

### Phase 3 — Spending summary endpoint (US2 + US3)

- `GetBudgetSummaryQuery` + handler (MediatR cross-module → `GetTransactionSummaryQuery`)
- Unit tests for `CategoryNormalizationService`
- Contract test for `GET /budgets/summary`
- Version bumps

### Phase 4 — Frontend wiring (US1 + US2)

- Update `budget.model.ts`
- Create `budgets.service.ts`
- Update store files (state, methods, effects — real API + CRUD)
- Update `budgets.component.ts` (create/delete/edit handlers)
- Add error codes to registry

### Phase 5 — Historical period navigation (US3)

- Add `selectedYear`/`selectedMonth` to state + navigation methods
- Wire period picker in template (trigger `loadSummary` with year/month)

### Phase 6 — QA

- Playwright: create budget → spending shown → edit → delete
- Verify over-budget visual state
- Verify historical month navigation
