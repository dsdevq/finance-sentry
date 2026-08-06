# Implementation Plan: Subscriptions Detection

**Branch**: `014-subscriptions` | **Date**: 2026-05-02 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/014-subscriptions/spec.md`

## Summary

Build a subscription detection system that automatically identifies recurring charges from transaction history. Detection runs nightly via a Hangfire job (`SubscriptionDetectionJob`) in `FinanceSentry.Modules.BankSync` — it has direct access to `ITransactionRepository` and calls `ISubscriptionDetectionResultService` (Core interface) to persist results into `FinanceSentry.Modules.Subscriptions`. The detection algorithm groups debit transactions by normalised merchant name, filters for ≥3 occurrences with consistent monthly (28–35 day) or annual (351–379 day) intervals and < 20% amount variance. The frontend subscriptions scaffold (store, component) already exists with mock data; this feature replaces mock data with real API calls and aligns the model to the detect/dismiss/restore/potentially-cancelled lifecycle.

## Technical Context

**Language/Version**: C# 13/.NET 9 (backend) · TypeScript 5.x strict / Angular 21.2 (frontend)
**Primary Dependencies**: ASP.NET Core 9, EF Core 9, MediatR, Hangfire · NgRx SignalStore 21.1, @dsdevq-common/ui
**Storage**: PostgreSQL 14 — new `detected_subscriptions` table in `SubscriptionsDbContext`
**Testing**: xUnit (backend) · Vitest + Playwright (frontend)
**Target Platform**: Linux (Docker) server + Angular SPA
**Project Type**: Web application (modular monolith + SPA)
**Performance Goals**: Nightly detection completes within 30 seconds per user; page load returns pre-computed results instantly
**Constraints**: ≥ 3 occurrences required; amount variance < 20%; dismissals persist across nightly re-runs
**Scale/Scope**: Per-user analysis of up to 13 months of transaction history

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Modular Monolith | ✅ | New `FinanceSentry.Modules.Subscriptions`; detection job in BankSync calls `ISubscriptionDetectionResultService` (Core interface) — no direct module-to-module reference |
| II. Code Quality | ✅ | ESLint gate + zero `dotnet build` warnings per file |
| III. Multi-Source Integration | ✅ | Detection runs across all connected providers (Plaid, Monobank, and future providers) via the unified transaction table |
| IV. AI Analytics | N/A | No AI in v1 (fuzzy matching deferred) |
| V. Security | ✅ | All queries scoped to `userId` from JWT; detection job scoped per-user |
| VI. Frontend State | ✅ | `SubscriptionsStore` page-scoped; 5-file SignalStore split already scaffolded |
| VI.5 File Organisation | ✅ | Frontend in canonical layout; backend follows Alerts/Budgets module patterns |
| Versioning | ✅ | Backend minor version bump for new endpoints; frontend minor version bump |

**Post-design re-check**: No violations. Detection job in BankSync depends only on Core (`ISubscriptionDetectionResultService`); Subscriptions module depends only on Core (implements the interface). No circular references.

## Project Structure

### Documentation (this feature)

```text
specs/014-subscriptions/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── subscriptions-rest-api.md
└── tasks.md
```

### Source Code

```text
backend/src/
├── FinanceSentry.Core/
│   └── Interfaces/
│       └── ISubscriptionDetectionResultService.cs    [NEW]
│
├── FinanceSentry.Modules.Subscriptions/              [NEW MODULE]
│   ├── API/
│   │   ├── Controllers/
│   │   │   └── SubscriptionsController.cs
│   │   └── Responses/
│   │       ├── SubscriptionDto.cs
│   │       ├── SubscriptionsListResponse.cs
│   │       └── SubscriptionSummaryResponse.cs
│   ├── Application/
│   │   ├── Commands/
│   │   │   ├── DismissSubscriptionCommand.cs
│   │   │   └── RestoreSubscriptionCommand.cs
│   │   ├── Queries/
│   │   │   ├── GetSubscriptionsQuery.cs
│   │   │   └── GetSubscriptionSummaryQuery.cs
│   │   └── Services/
│   │       └── SubscriptionDetectionResultService.cs [implements ISubscriptionDetectionResultService]
│   ├── Domain/
│   │   ├── DetectedSubscription.cs
│   │   ├── SubscriptionStatus.cs
│   │   └── Repositories/
│   │       └── IDetectedSubscriptionRepository.cs
│   ├── Infrastructure/
│   │   └── Persistence/
│   │       ├── SubscriptionsDbContext.cs
│   │       ├── SubscriptionsDbContextFactory.cs
│   │       └── Repositories/
│   │           └── DetectedSubscriptionRepository.cs
│   ├── Migrations/
│   ├── SubscriptionsModule.cs
│   └── FinanceSentry.Modules.Subscriptions.csproj
│
├── FinanceSentry.Modules.BankSync/
│   ├── Application/Services/
│   │   └── MerchantNameNormalizer.cs                 [NEW: static normalizer]
│   └── Infrastructure/Jobs/
│       ├── SubscriptionDetectionJob.cs               [NEW: nightly detection algorithm]
│       └── HangfireSetup.cs                          [MODIFY: register subscription-detection job]
│
└── FinanceSentry.API/
    ├── Program.cs                                    [MODIFY: SubscriptionsModule, DbContext, DI, migration]
    └── FinanceSentry.API.csproj                     [MODIFY: bump minor version]

frontend/src/app/
├── modules/subscriptions/
│   ├── models/subscription/
│   │   └── subscription.model.ts                    [MODIFY: align to spec (status, cadence, amounts)]
│   ├── services/
│   │   └── subscriptions.service.ts                 [NEW: 4 HTTP methods]
│   ├── store/subscriptions/
│   │   ├── subscriptions.state.ts                   [MODIFY: summary state, hasInsufficientHistory]
│   │   ├── subscriptions.computed.ts                [MODIFY: update computed for new model fields]
│   │   ├── subscriptions.methods.ts                 [MODIFY: dismiss/restore mutations]
│   │   ├── subscriptions.effects.ts                 [MODIFY: replace mock with API; add dismiss/restore]
│   │   └── subscriptions.store.ts                   [no change]
│   └── pages/subscriptions/
│       └── subscriptions.component.ts               [MODIFY: rename cancel→dismiss; add restore handler]
└── core/
    └── errors/
        └── error-messages.registry.ts               [MODIFY: add SUBSCRIPTION_NOT_FOUND]
```

## Complexity Tracking

No constitution violations. No complexity tracking required.

---

## Implementation Phases (for /speckit.tasks)

### Phase 1 — Backend foundation

- Define `ISubscriptionDetectionResultService` in Core
- Scaffold `FinanceSentry.Modules.Subscriptions` project + csproj + references
- `DetectedSubscription` domain entity + `SubscriptionStatus` constants
- `IDetectedSubscriptionRepository` + `DetectedSubscriptionRepository`
- `SubscriptionsDbContext` + migration M001
- `SubscriptionDetectionResultService` (upsert + potentially-cancelled logic)
- Register in `Program.cs`

### Phase 2 — Detection algorithm (US1 data generation)

- `MerchantNameNormalizer` static utility in BankSync
- `SubscriptionDetectionJob` (nightly, iterates all users, runs detection algorithm, calls service)
- Register `subscription-detection` recurring Hangfire job
- Unit tests for `MerchantNameNormalizer` and detection algorithm core logic

### Phase 3 — REST endpoints (US1)

- `GetSubscriptionsQuery` + handler
- `GetSubscriptionSummaryQuery` + handler
- Response DTOs
- `SubscriptionsController`
- Contract tests for all 4 endpoints
- Version bumps

### Phase 4 — Dismiss / Restore (US2)

- `DismissSubscriptionCommand` + handler
- `RestoreSubscriptionCommand` + handler
- Contract tests for PATCH dismiss / restore

### Phase 5 — Frontend wiring (US1 + US2 + US3)

- Update `subscription.model.ts`
- Create `subscriptions.service.ts`
- Update store (state, computed, methods, effects)
- Update `subscriptions.component.ts` (dismiss/restore flow; insufficient-history banner)
- Add error codes to registry

### Phase 6 — QA

- Playwright: verify subscriptions page shows real detection results (empty until job runs)
- Trigger detection job via Hangfire dashboard; verify results appear
- Dismiss a subscription → disappears; restore it → reappears
- Verify summary card shows correct monthly total
