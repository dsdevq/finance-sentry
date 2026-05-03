# Quickstart: Subscriptions Detection (014)

## Backend: New Module Setup

```bash
# 1. Create project
cd backend/src
dotnet new classlib -n FinanceSentry.Modules.Subscriptions \
  --framework net9.0 -o FinanceSentry.Modules.Subscriptions

# 2. Add references
dotnet add FinanceSentry.Modules.Subscriptions/ \
  reference FinanceSentry.Core/FinanceSentry.Core.csproj

dotnet add FinanceSentry.Modules.Subscriptions/ \
  reference FinanceSentry.Infrastructure/FinanceSentry.Infrastructure.csproj

dotnet add FinanceSentry.API/ \
  reference ../FinanceSentry.Modules.Subscriptions/FinanceSentry.Modules.Subscriptions.csproj

# 3. EF Core packages
dotnet add FinanceSentry.Modules.Subscriptions/ package Microsoft.EntityFrameworkCore
dotnet add FinanceSentry.Modules.Subscriptions/ package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add FinanceSentry.Modules.Subscriptions/ package MediatR

# 4. Create migrations
cd backend
dotnet ef migrations add M001_InitialSchema \
  --project src/FinanceSentry.Modules.Subscriptions \
  --context SubscriptionsDbContext \
  --output-dir Migrations
```

## Backend: Program.cs Registration

```csharp
// 1. CQRS
builder.Services.AddCqrs(
    ...,
    typeof(SubscriptionsModule).Assembly);  // ADD

// 2. DbContext
builder.Services.AddDbContext<SubscriptionsDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. DI
builder.Services.AddScoped<ISubscriptionDetectionResultService,
    SubscriptionDetectionResultService>();

// 4. Migration block
try {
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>();
    await db.Database.MigrateAsync();
} catch (Exception ex) {
    Log.Error(ex, "Failed to migrate SubscriptionsDbContext");
}

// 5. Hangfire job (in SubscriptionsHangfireSetup or HangfireSetup extension)
recurringJobManager.AddOrUpdate<SubscriptionDetectionJob>(
    "subscription-detection",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Daily());
```

## Trigger Detection Job Manually (Dev)

```bash
# Via Hangfire dashboard
open http://localhost:5001/hangfire

# Or trigger via API (if you expose a dev trigger endpoint)
curl -X POST http://localhost:5001/api/v1/subscriptions/trigger-detection \
  -H "Authorization: Bearer $TOKEN"
# (Dev-only endpoint — not in production contract)
```

## Frontend: Wire Real API

```bash
cd frontend
npx ng generate service modules/subscriptions/services/subscriptions
npx eslint src/app/modules/subscriptions/ --fix
```

Key changes to existing scaffolded code:
1. `subscription.model.ts` — replace `status: 'active'|'paused'` with `'active'|'dismissed'|'potentially_cancelled'`; add `cadence`, `averageAmount`, `lastChargeDate`, `nextExpectedDate`, `occurrenceCount`; remove `logo` (derive in component), keep `color` as frontend-only
2. `subscriptions.service.ts` — 4 HTTP methods: getSubscriptions, getSummary, dismiss, restore
3. `subscriptions.effects.ts` — replace mock with API calls; add dismiss/restore rxMethods
4. `subscriptions.methods.ts` — update dismiss/restore mutations
5. `subscriptions.component.ts` — rename `confirmCancel` to `confirmDismiss`; add restore handler

## Database: Verify Migration

```sql
\c finance_sentry
\d detected_subscriptions
\di idx_detected_subscription_user_merchant
```

## Verify Detection Output

After triggering the detection job:
```bash
# Check what was detected for the test user
curl http://localhost:5001/api/v1/subscriptions \
  -H "Authorization: Bearer $TOKEN"

# Check summary
curl http://localhost:5001/api/v1/subscriptions/summary \
  -H "Authorization: Bearer $TOKEN"
```

Expected: known recurring charges from Monobank/Plaid transaction history appear as DetectedSubscription entries.
