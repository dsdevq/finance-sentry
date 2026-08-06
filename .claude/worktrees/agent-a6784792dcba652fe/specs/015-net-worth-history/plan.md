# Implementation Plan: Net Worth History Chart

**Branch**: `015-net-worth-history` | **Date**: 2026-05-02 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/015-net-worth-history/spec.md`

## Summary

Build a net worth history chart on the Dashboard that replaces hardcoded mock data with real monthly snapshots. A `NetWorthSnapshotJob` in `FinanceSentry.Modules.BankSync` captures banking, brokerage, and crypto totals at month-end via a Hangfire recurring job (`0 1 L * *`). A `FirstSyncSnapshotTrigger` (MediatR handler) immediately enqueues an ad-hoc snapshot when a user completes their first sync. Snapshots are persisted via `INetWorthSnapshotService` (Core interface) into a new `FinanceSentry.Modules.NetWorthHistory` module. The frontend extends `DashboardStore` to load history from `GET /api/v1/net-worth/history?range=1y` and adds a range selector (3m / 6m / 1y / all).

## Technical Context

**Language/Version**: C# 13/.NET 9 (backend) · TypeScript 5.x strict / Angular 21.2 (frontend)
**Primary Dependencies**: ASP.NET Core 9, EF Core 9, MediatR, Hangfire · NgRx SignalStore 21.1, @dsdevq-common/ui
**Storage**: PostgreSQL 14 — new `net_worth_snapshots` table in `NetWorthHistoryDbContext`
**Testing**: xUnit (backend) · Vitest + Playwright (frontend)
**Target Platform**: Linux (Docker) server + Angular SPA
**Project Type**: Web application (modular monolith + SPA)
**Performance Goals**: Snapshot job completes within 5 seconds per user; history endpoint returns instantly (pre-computed)
**Constraints**: Immutable snapshots; one per (userId, month-end date); USD currency only in v1
**Scale/Scope**: Per-user monthly snapshots; up to ~13 entries for "all time" in first year

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Modular Monolith | ✅ | New `FinanceSentry.Modules.NetWorthHistory`; snapshot job in BankSync calls `INetWorthSnapshotService` (Core interface) — no direct module-to-module reference |
| II. Code Quality | ✅ | ESLint gate + zero `dotnet build` warnings per file |
| III. Multi-Source Integration | ✅ | Snapshots aggregate banking (IAggregationService), brokerage (IBrokerageHoldingsReader), and crypto (ICryptoHoldingsReader) |
| IV. AI Analytics | N/A | No AI in v1 |
| V. Security | ✅ | All queries scoped to `userId` from JWT; snapshot job scoped per-user |
| VI. Frontend State | ✅ | `DashboardStore` page-scoped; 5-file SignalStore split extended with history state |
| VI.5 File Organisation | ✅ | Frontend models/types in dedicated model file; no inline definitions |
| Versioning | ✅ | Backend 0.7.0 → 0.8.0 (new endpoint); frontend 0.7.0 → 0.8.0 |

**Post-design re-check**: No violations. Snapshot job in BankSync depends only on Core (`INetWorthSnapshotService`); NetWorthHistory module depends only on Core (implements the interface). No circular references.

## Project Structure

### Documentation (this feature)

```text
specs/015-net-worth-history/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── net-worth-history-rest-api.md
└── tasks.md
```

### Source Code

```text
backend/src/
├── FinanceSentry.Core/
│   └── Interfaces/
│       └── INetWorthSnapshotService.cs           [NEW]
│
├── FinanceSentry.Modules.NetWorthHistory/         [NEW MODULE]
│   ├── API/
│   │   ├── Controllers/
│   │   │   └── NetWorthHistoryController.cs
│   │   └── Responses/
│   │       ├── NetWorthSnapshotDto.cs
│   │       └── NetWorthHistoryResponse.cs
│   ├── Application/
│   │   ├── Queries/
│   │   │   └── GetNetWorthHistoryQuery.cs
│   │   └── Services/
│   │       └── NetWorthSnapshotService.cs        [implements INetWorthSnapshotService]
│   ├── Domain/
│   │   ├── NetWorthSnapshot.cs
│   │   └── Repositories/
│   │       └── INetWorthSnapshotRepository.cs
│   ├── Infrastructure/
│   │   └── Persistence/
│   │       ├── NetWorthHistoryDbContext.cs
│   │       ├── NetWorthHistoryDbContextFactory.cs
│   │       └── Repositories/
│   │           └── NetWorthSnapshotRepository.cs
│   ├── Migrations/
│   ├── NetWorthHistoryModule.cs
│   └── FinanceSentry.Modules.NetWorthHistory.csproj
│
├── FinanceSentry.Modules.BankSync/
│   ├── Application/EventHandlers/
│   │   └── FirstSyncSnapshotTrigger.cs           [NEW: handles first sync → snapshot]
│   └── Infrastructure/Jobs/
│       ├── NetWorthSnapshotJob.cs                 [NEW: monthly snapshot algorithm]
│       └── HangfireSetup.cs                       [MODIFY: register net-worth-snapshot job]
│
└── FinanceSentry.API/
    ├── Program.cs                                 [MODIFY: NetWorthHistoryModule, DbContext, DI, migration]
    └── FinanceSentry.API.csproj                   [MODIFY: bump 0.7.0 → 0.8.0]

frontend/src/app/
├── modules/bank-sync/
│   ├── models/dashboard/
│   │   └── dashboard.model.ts                    [MODIFY: add NetWorthSnapshotDto, NetWorthHistoryResponse, HistoryRange]
│   ├── services/
│   │   └── bank-sync.service.ts                  [MODIFY: add getNetWorthHistory(range)]
│   └── store/dashboard/
│       ├── dashboard.state.ts                    [MODIFY: add netWorthHistory, historyRange, historyLoading, historyError]
│       ├── dashboard.computed.ts                 [MODIFY: update netWorthHistoryData from state; add isHistoryLoading, historyErrorMessage]
│       ├── dashboard.methods.ts                  [MODIFY: add setNetWorthHistory, setHistoryRange, setHistoryLoading, setHistoryError]
│       ├── dashboard.effects.ts                  [MODIFY: add loadNetWorthHistory rxMethod]
│       └── dashboard.store.ts                    [no change]
├── pages/dashboard/
│   └── dashboard.component.ts                    [MODIFY: add range selector; bind to store.historyRange()]
└── core/
    └── errors/
        └── error-messages.registry.ts            [MODIFY: add INVALID_RANGE]
```

## Complexity Tracking

No constitution violations. No complexity tracking required.

---

## Implementation Phases (for /speckit.tasks)

### Phase 1 — Backend foundation

- Define `INetWorthSnapshotService` in Core
- Scaffold `FinanceSentry.Modules.NetWorthHistory` project + csproj + references
- `NetWorthSnapshot` domain entity
- `INetWorthSnapshotRepository` + `NetWorthSnapshotRepository`
- `NetWorthHistoryDbContext` + migration M001
- `NetWorthSnapshotService` (persist + HasSnapshotForCurrentMonth logic)
- Register in `Program.cs`

### Phase 2 — Snapshot job (US1 data generation)

- `NetWorthSnapshotJob` (monthly job: enumerate users, gather balances, persist via service)
- `FirstSyncSnapshotTrigger` (MediatR handler: detect first sync per user, enqueue job)
- Register `net-worth-snapshot` recurring Hangfire job (last day of month cron)
- Unit tests for snapshot job logic

### Phase 3 — REST endpoint (US1 read)

- `GetNetWorthHistoryQuery` + handler
- Response DTOs (`NetWorthSnapshotDto`, `NetWorthHistoryResponse`)
- `NetWorthHistoryController`
- Contract tests for `GET /api/v1/net-worth/history`
- Version bumps (backend 0.7.0 → 0.8.0)

### Phase 4 — Frontend wiring (US1 + US3)

- Update `dashboard.model.ts` (add DTOs + `HistoryRange` type)
- Add `getNetWorthHistory` to `bank-sync.service.ts`
- Update store: state, computed, methods, effects
- Update `dashboard.component.ts` (range selector; wire to store; remove mock import)
- Add `INVALID_RANGE` to error registry
- Version bump (frontend 0.7.0 → 0.8.0)

### Phase 5 — QA

- Trigger snapshot job via Hangfire dashboard; verify snapshot appears in DB
- `GET /api/v1/net-worth/history` returns real data
- Dashboard chart renders real data point; range selector changes displayed months
- Disconnect an account; verify past snapshots unchanged (US2)
- Empty state shown when no snapshots exist (new test user)
