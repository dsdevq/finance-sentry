# Quickstart: Budgets (013)

## Backend: New Module Setup

```bash
# 1. Create project
cd backend/src
mkdir FinanceSentry.Modules.Budgets

# 2. Create csproj and add project references
dotnet new classlib -n FinanceSentry.Modules.Budgets \
  --framework net9.0 \
  -o FinanceSentry.Modules.Budgets

dotnet add FinanceSentry.Modules.Budgets/ \
  reference FinanceSentry.Core/FinanceSentry.Core.csproj

dotnet add FinanceSentry.Modules.Budgets/ \
  reference FinanceSentry.Infrastructure/FinanceSentry.Infrastructure.csproj

# 3. Add EF Core + Npgsql
dotnet add FinanceSentry.Modules.Budgets/ package Microsoft.EntityFrameworkCore
dotnet add FinanceSentry.Modules.Budgets/ package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add FinanceSentry.Modules.Budgets/ package MediatR

# 4. Reference from API
dotnet add FinanceSentry.API/ \
  reference ../FinanceSentry.Modules.Budgets/FinanceSentry.Modules.Budgets.csproj

# 5. Create EF migration
cd backend
dotnet ef migrations add M001_InitialSchema \
  --project src/FinanceSentry.Modules.Budgets \
  --context BudgetsDbContext \
  --output-dir Migrations

# 6. Add performance index migration to BankSync
dotnet ef migrations add M003_TransactionCategoryIndex \
  --project src/FinanceSentry.Modules.BankSync \
  --context BankSyncDbContext \
  --output-dir Migrations
```

## Backend: Program.cs Registration

```csharp
// 1. CQRS (near AddCqrs call)
builder.Services.AddCqrs(
    typeof(JwtTokenService).Assembly,
    typeof(CryptoSyncModule).Assembly,
    typeof(BrokerageSyncModule).Assembly,
    typeof(BankSyncModule).Assembly,
    typeof(AlertsModule).Assembly,      // existing
    typeof(BudgetsModule).Assembly);    // ADD

// 2. DbContext
builder.Services.AddDbContext<BudgetsDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. DI registrations
builder.Services.AddScoped<ICategoryNormalizationService, CategoryNormalizationService>();

// 4. Migration block
try {
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BudgetsDbContext>();
    await db.Database.MigrateAsync();
} catch (Exception ex) {
    Log.Error(ex, "Failed to migrate BudgetsDbContext");
}
```

## Frontend: Wire Real API

```bash
cd frontend

# Generate service
npx ng generate service modules/budgets/services/budgets

# After editing store, run lint
npx eslint src/app/modules/budgets/ --fix
npx eslint src/app/core/errors/error-messages.registry.ts --fix
```

Key changes to existing scaffolded code:
1. `budget.model.ts` — add `id`, `currency`, `createdAt`; add `BudgetSummaryItem` interface (with `spent`)
2. `budgets.service.ts` — 5 HTTP methods
3. `budgets.effects.ts` — replace BUDGET_MOCK_DATA with API calls; add create/update/delete effects
4. `budgets.methods.ts` — add `addBudget`, `updateBudgetLimit`, `removeBudget`
5. `budgets.state.ts` — add `selectedYear`, `selectedMonth` for period navigation (US3)
6. `budgets.component.ts` — add create and delete form handlers

## Local Dev: Verify Budgets Endpoint

```bash
# Ensure stack is running
cd docker && docker compose -f docker-compose.dev.yml up -d

# Create a budget (replace TOKEN)
curl -X POST http://localhost:5001/api/v1/budgets \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"category": "food_and_drink", "monthlyLimit": 400}'

# Get budget summary for current month
curl http://localhost:5001/api/v1/budgets/summary \
  -H "Authorization: Bearer $TOKEN"

# Verify spending matches transaction sum
curl "http://localhost:5001/api/v1/dashboard/transactions/summary?from=2026-05-01&to=2026-05-31" \
  -H "Authorization: Bearer $TOKEN"
```

## Database: Verify Migration

```sql
\c finance_sentry

-- Confirm budgets table
\d budgets

-- Confirm unique constraint
\di idx_budget_user_category_unique

-- Confirm performance index on transactions
\di idx_transaction_user_category_date
```
