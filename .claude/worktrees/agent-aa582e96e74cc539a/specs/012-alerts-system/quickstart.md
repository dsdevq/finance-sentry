# Quickstart: Alerts System (012)

## Backend: New Module Setup

```bash
# 1. Create project
mkdir -p backend/src/FinanceSentry.Modules.Alerts
cd backend/src/FinanceSentry.Modules.Alerts
dotnet new classlib -n FinanceSentry.Modules.Alerts --framework net9.0

# 2. Add project reference to API
dotnet add backend/src/FinanceSentry.API/FinanceSentry.API.csproj \
  reference backend/src/FinanceSentry.Modules.Alerts/FinanceSentry.Modules.Alerts.csproj

# 3. Add required packages to alerts module
dotnet add backend/src/FinanceSentry.Modules.Alerts/ package Microsoft.EntityFrameworkCore
dotnet add backend/src/FinanceSentry.Modules.Alerts/ package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add backend/src/FinanceSentry.Modules.Alerts/ package MediatR
dotnet add backend/src/FinanceSentry.Modules.Alerts/ package Hangfire.Core

# 4. Add reference to Core
dotnet add backend/src/FinanceSentry.Modules.Alerts/ \
  reference backend/src/FinanceSentry.Core/FinanceSentry.Core.csproj

# 5. Create initial EF migration
cd backend
dotnet ef migrations add M001_InitialSchema \
  --project src/FinanceSentry.Modules.Alerts \
  --context AlertsDbContext \
  --output-dir Migrations
```

## Backend: Program.cs Registration

Add to `Program.cs` (following the pattern of BankSync/CryptoSync):

```csharp
// 1. CQRS registration (near line 97)
builder.Services.AddCqrs(
    typeof(JwtTokenService).Assembly,
    typeof(CryptoSyncModule).Assembly,
    typeof(BrokerageSyncModule).Assembly,
    typeof(BankSyncModule).Assembly,
    typeof(AlertsModule).Assembly);   // ADD

// 2. DbContext (near line 113)
builder.Services.AddDbContext<AlertsDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. IAlertGeneratorService (in module's DI extension method)
builder.Services.AddScoped<IAlertGeneratorService, AlertGeneratorService>();

// 4. Migration block (near line 300)
try {
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AlertsDbContext>();
    await db.Database.MigrateAsync();
} catch (Exception ex) {
    Log.Error(ex, "Failed to migrate AlertsDbContext");
}

// 5. Hangfire jobs (in AlertsHangfireSetup, called from Program.cs)
recurringJobManager.AddOrUpdate<AlertPurgeJob>(
    "alert-purge",
    job => job.RunAsync(CancellationToken.None),
    Cron.Monthly());
```

## Frontend: Wire Real API

After backend endpoints are live, update the store:

```bash
# Generate AlertsService
cd frontend
npx ng generate service modules/alerts/services/alerts

# Run lint after changes
npx eslint src/app/modules/alerts/ --fix
```

Key changes needed in the store:
1. `alerts.effects.ts` — replace `ALERT_MOCK_DATA` with `AlertsService` HTTP calls
2. `alerts.store.ts` — add `{providedIn: 'root'}` for sidebar badge (SC-005)
3. `alert.model.ts` — add `dismissed: boolean`, `resolved: boolean`, `resolvedAt: number | null`

## Local Dev: Verify Alerts Endpoint

```bash
# Ensure stack is running
cd docker && docker compose -f docker-compose.dev.yml up -d

# Test endpoint (replace TOKEN with your auth token)
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5001/api/v1/alerts

# Trigger a low-balance alert by syncing an account with balance < threshold
# (threshold default: $500; adjust in Settings > Notifications)
```

## Database: Verify Migration

```sql
-- Connect to finance_sentry DB
\c finance_sentry

-- Confirm alerts table
\d alerts

-- Check dedup index
\di idx_alert_dedup
```
