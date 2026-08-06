# Implementation Plan: Alerts System

**Branch**: `012-alerts-system` | **Date**: 2026-05-02 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/012-alerts-system/spec.md`

## Summary

Build an alerts system that surfaces sync failures, low-balance threshold breaches, and unusual spend patterns to the user. Backend: new `FinanceSentry.Modules.Alerts` module with its own `AlertsDbContext`, REST endpoints, and a cross-cutting `IAlertGeneratorService` (defined in Core) injected into existing sync jobs. Alert generation for low-balance and sync-failure is event-driven (post-sync); unusual-spend runs as a nightly Hangfire job. Frontend: upgrade the existing mock-data AlertsStore to a real API-backed root-scoped store; wire unread count to the sidebar badge.

## Technical Context

**Language/Version**: C# 13/.NET 9 (backend) · TypeScript 5.x strict / Angular 21.2 (frontend)
**Primary Dependencies**: ASP.NET Core 9, EF Core 9, MediatR, Hangfire · NgRx SignalStore 21.1, @dsdevq-common/ui
**Storage**: PostgreSQL 14 — new `alerts` table in `AlertsDbContext` with its own migrations
**Testing**: xUnit / Hangfire (backend) · Vitest + Playwright (frontend)
**Target Platform**: Linux (Docker) server + Angular SPA
**Project Type**: Web application (modular monolith + SPA)
**Performance Goals**: Alerts visible within one sync cycle; sidebar count updates without page reload
**Constraints**: Zero duplicate unresolved alerts per (user, type, reference); 90-day auto-purge
**Scale/Scope**: Low-volume per-user data; max ~50 active alerts per user at any time

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Modular Monolith | ✅ | New `FinanceSentry.Modules.Alerts`; cross-module communication via `IAlertGeneratorService` interface in Core — no direct module-to-module references |
| II. Code Quality | ✅ | ESLint gate + zero `dotnet build` warnings enforced per file |
| III. Multi-Source Integration | ✅ | Alerts cover BankSync, CryptoSync, and BrokerageSync providers |
| IV. AI Analytics | N/A | No AI involvement in this feature |
| V. Security | ✅ | All queries scoped to `userId` extracted from JWT; no cross-user data leakage |
| VI. Frontend State | ✅ | AlertsStore already follows 5-file SignalStore split; will be made root-scoped per App-wide store rule |
| VI.5 File Organisation | ✅ | No inline interfaces; model/service/store/page in canonical locations |
| Versioning | ✅ | Backend `0.7.0 → 0.8.0` (new endpoints); Frontend `0.7.0 → 0.8.0` (new store/UI wiring) |

**Post-design re-check**: No violations. `IAlertGeneratorService` in Core does not create a Core→Alerts dependency (dependency inversion: Alerts implements the interface, consumers depend on the abstraction).

## Project Structure

### Documentation (this feature)

```text
specs/012-alerts-system/
├── plan.md              # This file
├── research.md          # Phase 0 decisions
├── data-model.md        # Entity schema + interface definitions
├── quickstart.md        # Dev setup and verification
├── contracts/
│   └── alerts-rest-api.md   # REST endpoint contracts
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code

```text
backend/src/
├── FinanceSentry.Core/
│   └── Interfaces/
│       └── IAlertGeneratorService.cs          [NEW]
│
├── FinanceSentry.Modules.Alerts/              [NEW MODULE]
│   ├── API/
│   │   ├── Controllers/
│   │   │   └── AlertsController.cs
│   │   └── Responses/
│   │       ├── AlertDto.cs
│   │       ├── AlertsPageResponse.cs
│   │       └── UnreadCountResponse.cs
│   ├── Application/
│   │   ├── Commands/
│   │   │   ├── MarkAlertReadCommand.cs
│   │   │   ├── MarkAllAlertsReadCommand.cs
│   │   │   └── DismissAlertCommand.cs
│   │   ├── Queries/
│   │   │   ├── GetAlertsQuery.cs
│   │   │   └── GetUnreadCountQuery.cs
│   │   └── Services/
│   │       └── AlertGeneratorService.cs       [implements IAlertGeneratorService]
│   ├── Domain/
│   │   ├── Alert.cs
│   │   ├── AlertType.cs
│   │   ├── AlertSeverity.cs
│   │   └── Repositories/
│   │       └── IAlertRepository.cs
│   ├── Infrastructure/
│   │   ├── Jobs/
│   │   │   ├── AlertPurgeJob.cs
│   │   │   └── AlertsHangfireSetup.cs
│   │   └── Persistence/
│   │       ├── AlertsDbContext.cs
│   │       ├── AlertsDbContextFactory.cs
│   │       └── Repositories/
│   │           └── AlertRepository.cs
│   ├── Migrations/
│   ├── AlertsModule.cs
│   └── FinanceSentry.Modules.Alerts.csproj
│
├── FinanceSentry.Modules.BankSync/
│   ├── Domain/Events/
│   │   └── AccountSyncCompletedEvent.cs       [MODIFY: add UserId, Provider, BalanceAfterSync, ErrorCode]
│   ├── Application/Services/
│   │   └── ScheduledSyncService.cs            [MODIFY: inject IAlertGeneratorService, call on success/failure]
│   └── Infrastructure/Jobs/
│       └── UnusualSpendDetectionJob.cs        [NEW: nightly job, queries Transactions, calls IAlertGeneratorService]
│
├── FinanceSentry.Modules.CryptoSync/
│   └── Infrastructure/Jobs/
│       └── BinanceSyncJob.cs                  [MODIFY: inject IAlertGeneratorService, call on failure/success]
│
├── FinanceSentry.Modules.BrokerageSync/
│   └── Infrastructure/Jobs/
│       └── IBKRSyncJob.cs                     [MODIFY: inject IAlertGeneratorService, call on failure/success]
│
└── FinanceSentry.API/
    ├── Program.cs                             [MODIFY: register AlertsModule, DbContext, migrations, Hangfire job]
    └── FinanceSentry.API.csproj              [MODIFY: version 0.7.0 → 0.8.0]

frontend/src/app/
├── modules/alerts/
│   ├── models/alert/
│   │   └── alert.model.ts                     [MODIFY: add dismissed, resolved, resolvedAt fields]
│   ├── services/
│   │   └── alerts.service.ts                  [NEW: HTTP calls for all 5 endpoints]
│   ├── store/alerts/
│   │   ├── alerts.state.ts                    [MODIFY: add pagination, totalCount]
│   │   ├── alerts.computed.ts                 [no change needed]
│   │   ├── alerts.methods.ts                  [MODIFY: add setTotalCount, setPage]
│   │   ├── alerts.effects.ts                  [MODIFY: replace mock with AlertsService calls]
│   │   └── alerts.store.ts                    [MODIFY: add {providedIn: 'root'}]
│   └── pages/alerts/
│       └── alerts.component.ts                [MODIFY: remove providers:[AlertsStore] if present]
└── core/
    ├── shell/
    │   └── app-shell.component.ts             [MODIFY: inject AlertsStore, bind unread count to Bell NavItem]
    └── errors/
        └── error-messages.registry.ts        [MODIFY: add ALERT_NOT_FOUND entry]
```

## Complexity Tracking

No constitution violations. No complexity tracking required.

---

## Implementation Phases (for /speckit.tasks)

### Phase 1 — Backend foundation

- Create `IAlertGeneratorService` in `FinanceSentry.Core`
- Scaffold `FinanceSentry.Modules.Alerts` project + csproj references
- Implement `Alert` domain entity + `AlertType`/`AlertSeverity` enums
- Implement `IAlertRepository` + `AlertRepository`
- Implement `AlertsDbContext` with migration M001 (`alerts` table + indexes)
- Implement `AlertGeneratorService` (deduplication, create/resolve/purge logic)
- Register in `Program.cs` (DbContext, DI, migration block)

### Phase 2 — Alert generation hooks

- Extend `AccountSyncCompletedEvent` with new fields
- Modify `ScheduledSyncService` to call `IAlertGeneratorService` on success/failure
- Modify `BinanceSyncJob` to call `IAlertGeneratorService` on success/failure
- Modify `IBKRSyncJob` to call `IAlertGeneratorService` on success/failure
- Add `UnusualSpendDetectionJob` to BankSync module + register as nightly Hangfire job

### Phase 3 — REST endpoints

- Implement `GetAlertsQuery` + handler (paginated, filtered)
- Implement `GetUnreadCountQuery` + handler
- Implement `MarkAlertReadCommand`, `MarkAllAlertsReadCommand`, `DismissAlertCommand` + handlers
- Implement `AlertsController` with all 5 endpoints
- Version bump: `FinanceSentry.API.csproj` 0.7.0 → 0.8.0
- Add `AlertPurgeJob` + register as monthly Hangfire job

### Phase 4 — Frontend wiring

- Update `alert.model.ts` (add dismissed/resolved fields)
- Create `alerts.service.ts` (5 API methods)
- Update `alerts.store.ts`: root-scoped, real API calls, pagination state
- Update `app-shell.component.ts`: inject AlertsStore, bind unread count badge to Bell nav item
- Add `ALERT_NOT_FOUND` to `error-messages.registry.ts`
- Version bump: `frontend/package.json` 0.7.0 → 0.8.0

### Phase 5 — QA

- Playwright end-to-end: alerts page loads, mark read, dismiss, mark all read
- Verify sidebar badge reflects unread count
- Trigger a sync and verify low-balance alert appears (set threshold above account balance)
- Verify no duplicate unresolved alerts created on repeated syncs
