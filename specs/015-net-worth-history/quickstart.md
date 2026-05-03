# Quickstart: Net Worth History (015)

## Backend: New Module Setup

```bash
# 1. Create project
cd backend/src
dotnet new classlib -n FinanceSentry.Modules.NetWorthHistory \
  --framework net9.0 -o FinanceSentry.Modules.NetWorthHistory

# 2. Add references
dotnet add FinanceSentry.Modules.NetWorthHistory/ \
  reference FinanceSentry.Core/FinanceSentry.Core.csproj

dotnet add FinanceSentry.Modules.NetWorthHistory/ \
  reference FinanceSentry.Infrastructure/FinanceSentry.Infrastructure.csproj

dotnet add FinanceSentry.API/ \
  reference ../FinanceSentry.Modules.NetWorthHistory/FinanceSentry.Modules.NetWorthHistory.csproj

# 3. EF Core packages
dotnet add FinanceSentry.Modules.NetWorthHistory/ package Microsoft.EntityFrameworkCore
dotnet add FinanceSentry.Modules.NetWorthHistory/ package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add FinanceSentry.Modules.NetWorthHistory/ package MediatR

# 4. Create migration
cd backend
dotnet ef migrations add M001_InitialSchema \
  --project src/FinanceSentry.Modules.NetWorthHistory \
  --context NetWorthHistoryDbContext \
  --output-dir Migrations
```

## Backend: Program.cs Registration

```csharp
// 1. CQRS
builder.Services.AddCqrs(
    ...,
    typeof(NetWorthHistoryModule).Assembly);  // ADD

// 2. DbContext
builder.Services.AddDbContext<NetWorthHistoryDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. DI
builder.Services.AddScoped<INetWorthSnapshotService, NetWorthSnapshotService>();

// 4. Migration block
try {
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NetWorthHistoryDbContext>();
    await db.Database.MigrateAsync();
} catch (Exception ex) {
    Log.Error(ex, "Failed to migrate NetWorthHistoryDbContext");
}

// 5. Hangfire job (in SyncScheduler.ScheduleAllActiveAccounts)
recurringJobManager.AddOrUpdate<NetWorthSnapshotJob>(
    "net-worth-snapshot",
    job => job.ExecuteAsync(null, CancellationToken.None),
    "0 1 L * *");  // 1am on last day of each month
```

## Trigger Snapshot Job Manually (Dev)

```bash
# Via Hangfire dashboard
open http://localhost:5001/hangfire

# Or via curl after running the app
# Trigger via Hangfire dashboard UI: find "net-worth-snapshot" → Trigger Now
```

## Verify Snapshot Output

```bash
# Check snapshots for the test user
curl http://localhost:5001/api/v1/net-worth/history \
  -H "Authorization: Bearer $TOKEN"

# With range
curl "http://localhost:5001/api/v1/net-worth/history?range=all" \
  -H "Authorization: Bearer $TOKEN"
```

Expected: after triggering the job, the response contains one snapshot for the current month.

## Database: Verify Migration

```sql
\c finance_sentry
\d net_worth_snapshots
\di idx_net_worth_snapshot_user_date_unique
```

## Frontend: Wire Real Data

Key changes:
1. `dashboard.model.ts` — add `NetWorthHistoryResponse`, `NetWorthSnapshotDto`, `HistoryRange` type
2. `bank-sync.service.ts` — add `getNetWorthHistory(range: HistoryRange)` HTTP method
3. `dashboard.state.ts` — add `netWorthHistory`, `historyRange`, `historyLoading`, `historyError` fields
4. `dashboard.computed.ts` — update `netWorthHistoryData` to map from state (remove mock); add `isHistoryLoading`, `historyErrorMessage`
5. `dashboard.methods.ts` — add `setNetWorthHistory`, `setHistoryRange`, `setHistoryLoading`, `setHistoryError`
6. `dashboard.effects.ts` — add `loadNetWorthHistory` rxMethod; call on init and on range change
7. `dashboard.component.ts` — add range selector buttons; bind to `store.historyRange()`

## Test: Dashboard Chart Shows Real Data

1. Start Docker stack: `docker compose -f docker/docker-compose.dev.yml up -d`
2. Trigger snapshot job via Hangfire dashboard
3. Open `http://localhost:4200/bank-sync/dashboard`
4. Chart should show one real data point (current month) with correct totals
5. Select different range options → chart updates
6. Confirm no mock data remains (chart empty-state visible before job runs)
