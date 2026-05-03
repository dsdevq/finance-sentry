# Implementation Plan: Historical Net Worth Backfill

**Branch**: `016-history-backfill` | **Date**: 2026-05-03 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `specs/016-history-backfill/spec.md`

## Summary

When a user connects Binance, IBKR, or Monobank, a background backfill job fetches historical portfolio data from all currently connected providers and recreates the user's monthly net worth snapshot history (delete-and-recreate strategy). No schema changes — the existing `net_worth_snapshots` table is reused. The trigger lives in the provider `Connect*Command` handlers via a new `IHistoricalBackfillScheduler` Core interface.

## Technical Context

**Language/Version**: C# 13 / .NET 9 (backend only — no frontend changes)  
**Primary Dependencies**: ASP.NET Core 9, EF Core 9, MediatR, Hangfire, `System.Net.Http` (no new NuGet packages required)  
**Storage**: PostgreSQL 14 — existing `net_worth_snapshots` table; no new migrations  
**Testing**: xUnit + Moq (backend unit tests)  
**Target Platform**: Linux server (Docker)  
**Project Type**: Modular monolith extension  
**Performance Goals**: Backfill completes within 5 minutes for Binance/IBKR; Monobank exceeds this due to mandatory 60s inter-window delays (accepted per spec)  
**Constraints**: Monobank rate limit: 1 statement request per 60 seconds (FR-010)  
**Scale/Scope**: Single user at a time; up to 2 years history; up to 3 providers

## Constitution Check

| Gate | Status | Notes |
|------|--------|-------|
| Principle I — Modular Monolith | ✓ PASS | `HistoricalBackfillJob` in Wealth uses only Core interfaces; credential access stays inside each provider module |
| Principle II — Zero warnings | ✓ PASS | All new .cs files will pass `dotnet build` with zero warnings before marking any task complete |
| Principle II — ESLint gate | N/A | Backend-only feature; no frontend changes |
| Principle V — Security | ✓ PASS | Credentials read through existing encrypted repositories; no new credential handling |
| Principle VI — Frontend discipline | N/A | No frontend changes |

No violations. Complexity tracking table not required.

## Project Structure

### Documentation (this feature)

```text
specs/016-history-backfill/
├── plan.md              ← this file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── checklists/
│   └── requirements.md
└── tasks.md             ← Phase 2 output (/speckit.tasks — not yet created)
```

### Source Code Changes

```text
backend/
  src/
    FinanceSentry.Core/
      Interfaces/
        IHistoricalBackfillScheduler.cs   [NEW] void ScheduleForUser(Guid userId)
        IProviderMonthlyHistorySource.cs  [NEW] + ProviderMonthlyBalance record
        INetWorthSnapshotService.cs       [MODIFY] + ReplaceAllSnapshotsAsync
    
    FinanceSentry.Modules.Wealth/
      Application/
        Services/
          NetWorthSnapshotService.cs      [MODIFY] implement ReplaceAllSnapshotsAsync
      Domain/
        Repositories/
          INetWorthSnapshotRepository.cs  [MODIFY] + DeleteAllByUserIdAsync
      Infrastructure/
        Jobs/
          HistoricalBackfillJob.cs        [NEW] orchestrates history fetch + recompute
          HistoricalBackfillJobScheduler.cs [NEW] implements IHistoricalBackfillScheduler
        Persistence/
          Repositories/
            NetWorthSnapshotRepository.cs [MODIFY] implement DeleteAllByUserIdAsync
      WealthModule.cs                     [MODIFY] register new services + job
    
    FinanceSentry.Modules.CryptoSync/
      Infrastructure/
        Binance/
          BinanceHttpClient.cs            [MODIFY] + GetAccountSnapshotAsync
          BinanceAdapterModels.cs         [MODIFY] + BinanceSnapshotResponse models
        History/
          BinanceHistorySource.cs         [NEW] implements IProviderMonthlyHistorySource
      CryptoSyncModule.cs                 [MODIFY] register BinanceHistorySource
    
    FinanceSentry.Modules.BrokerageSync/
      Infrastructure/
        IBKR/
          IBKRGatewayClient.cs            [MODIFY] + GetPerformanceAsync
          IBKRGatewayModels.cs            [MODIFY] + IBKRPerformanceResponse models
        History/
          IBKRHistorySource.cs            [NEW] implements IProviderMonthlyHistorySource
      BrokerageSyncModule.cs              [MODIFY] register IBKRHistorySource
    
    FinanceSentry.Modules.BankSync/
      Infrastructure/
        Monobank/
          History/
            MonobankHistorySource.cs      [NEW] implements IProviderMonthlyHistorySource
      Application/
        Commands/
          ConnectMonobankAccountCommand.cs [MODIFY] inject + call IHistoricalBackfillScheduler
      BankSyncModule.cs                   [MODIFY] register MonobankHistorySource
    
    FinanceSentry.Modules.CryptoSync/
      Application/
        Commands/
          ConnectBinanceCommand.cs        [MODIFY] inject + call IHistoricalBackfillScheduler
    
    FinanceSentry.Modules.BrokerageSync/
      Application/
        Commands/
          ConnectIBKRCommand.cs           [MODIFY] inject + call IHistoricalBackfillScheduler

  tests/
    FinanceSentry.Tests.Unit/
      Wealth/
        HistoricalBackfillJobTests.cs     [NEW]
        NetWorthSnapshotServiceReplaceTests.cs [NEW]
      BankSync/
        MonobankHistorySourceTests.cs     [NEW]
      CryptoSync/
        BinanceHistorySourceTests.cs      [NEW]
      BrokerageSync/
        IBKRHistorySourceTests.cs         [NEW]
```

**Structure Decision**: Backend-only. Follows the existing module structure for Wealth, CryptoSync, BrokerageSync, and BankSync. All new infrastructure files go under `Infrastructure/History/` within each provider module, consistent with existing `Infrastructure/Jobs/` and `Infrastructure/IBKR/` directories.

## Key Design Decisions

### Trigger: connect command handlers (not event bus)

`ConnectBinanceCommandHandler`, `ConnectIBKRCommandHandler`, and `ConnectMonobankAccountCommand` handler each call `IHistoricalBackfillScheduler.ScheduleForUser(userId)` after a successful first connect. This avoids the problem that `AccountSyncCompletedEvent` is only published by BankSync and would require detecting "is this a first sync" from recurring sync events.

### History source abstraction: `IProviderMonthlyHistorySource`

`HistoricalBackfillJob` injects `IEnumerable<IProviderMonthlyHistorySource>`. Each implementation returns `ProviderMonthlyBalance[]` for a given user. If the user has no credential for a provider, that source returns an empty list. The job aggregates all lists grouped by `MonthEnd` into snapshots.

### Replace path: `ReplaceAllSnapshotsAsync`

`NetWorthSnapshotService.ReplaceAllSnapshotsAsync` deletes all snapshots for the user then bulk-inserts the new set inside a single EF transaction. `PersistSnapshotAsync` (no-op-on-exist) is unchanged — it continues to serve the monthly scheduled job.

### Binance snapshot API

`BinanceHttpClient.GetAccountSnapshotAsync(apiKey, apiSecret, startTime, endTime, ct)` calls `GET /sapi/v1/accountSnapshot?type=SPOT`. Returns daily balance snapshots. The `BinanceHistorySource` sums asset USD values from `SnapshotData.Balances` using the existing price ticker (separate `GetAllPricesAsync` call), then picks the last entry per calendar month.

### IBKR performance API

`IBKRGatewayClient.GetPerformanceAsync(accountId, ct)` calls `GET /v1/api/portfolio/{accountId}/performance`. Returns an NAV time series. `IBKRHistorySource` picks the last NAV entry per calendar month. Multi-sub-account users: the existing `IBKRAdapter.GetAccountIdAsync` returns the primary account; sub-account NAV aggregation is deferred.

### Monobank rate limiting

`MonobankHistorySource` uses `MonobankHttpClient.GetStatementsAsync` with 31-day windows. It introduces a `Task.Delay(TimeSpan.FromSeconds(60))` between each window to comply with FR-010. The `MonobankHttpClient` already handles `429` responses with its own retry (0 → 60 → 120s delay) as a safety net.
