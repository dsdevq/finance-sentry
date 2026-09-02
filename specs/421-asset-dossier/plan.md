# Implementation Plan: Asset Dossier

**Branch**: `goal/fs-421-asset-dossier-2026-08-31` | **Date**: 2026-09-02 | **Spec**: spec.md

## Summary

Add `GET /api/v1/research/assets/{symbol}/dossier` (US1) and the `/assets/:symbol` Angular page
(US2) that compose existing per-ticker reads (thesis, valuation, analyst actions, news, earnings,
radar signals, and position/tax-lots) into a single coherent view — the full pre-answer gather
that Ledger already uses, now surfaced in the UI. US3 (Ledger's read, on-demand AI + cache) is a
separate subsequent PR.

## Technical Context

**Language/Version**: .NET 10 / C# 14 (backend), Angular 21.2 / TypeScript strict (frontend)

**Primary Dependencies**: ASP.NET Core, EF Core, MediatR-lite CQRS, NgRx SignalStore

**Storage**: PostgreSQL (existing schemas; no new tables in US1)

**Testing**: xUnit + WebApplicationFactory (backend contract tests), Vitest + Playwright (frontend)

**Target Platform**: Linux container via Docker Compose

**Constraints**: Zero `dotnet build` warnings; ESLint zero-error; `--no-verify` commit in sandbox
(NODE_AUTH_TOKEN absent; CI enforces full frontend gate)

## Constitution Check

- Modular monolith: cross-module data via port interfaces in Integration layer ✓
- Code quality: zero warnings gate ✓
- UI library rule: no raw components; `cmn-*` from `@lifekit-hq/ui` ✓
- State management: NgRx SignalStore; component is declarative ✓
- File organization: one concept per file ✓

## Project Structure

### Documentation

```text
specs/421-asset-dossier/
├── spec.md
├── plan.md        ← this file
└── tasks.md
```

### Backend (US1)

```text
backend/src/FinanceSentry.Modules.Research/
├── Domain/Ports/
│   ├── IHoldingTaxLotsReader.cs     NEW — port for brokerage tax lot detail
│   └── IAssetSignalReader.cs        NEW — port for per-ticker radar signals
├── API/Responses/
│   └── AssetDossierResult.cs        NEW — aggregate response + section DTOs
└── Application/Queries/
    └── GetAssetDossierQuery.cs      NEW — fan-out query handler
    └── AssetDossierController.cs    NEW — GET /research/assets/{symbol}/dossier

backend/src/FinanceSentry.Integration/
├── HoldingTaxLotsAdapter.cs         NEW — IHoldingTaxLotsReader impl (BrokerageSync)
├── AssetSignalAdapter.cs            NEW — IAssetSignalReader impl (Radar)
└── CrossModulePortRegistration.cs   MODIFIED — register two new adapters

backend/tests/FinanceSentry.Tests.Integration/
└── Research/AssetDossierContractTests.cs  NEW — shape + auth tests
```

### Frontend (US2 — next session)

```text
frontend/src/app/
├── shared/enums/app-route/app-route.enum.ts   MODIFIED — add AssetDossier route
└── modules/assets/
    ├── models/dossier/dossier.model.ts         NEW — mirror of backend DTOs
    ├── services/dossier.service.ts             NEW — HTTP service
    ├── store/
    │   ├── dossier.state.ts
    │   ├── dossier.computed.ts
    │   ├── dossier.methods.ts
    │   ├── dossier.effects.ts
    │   └── dossier.store.ts
    └── pages/asset-dossier/
        ├── asset-dossier.component.ts
        └── asset-dossier.component.html
```

## Slice-by-Slice Notes

### US1 — Aggregate Read Endpoint

Files touched: Research/Domain/Ports (2 new), Research/API/Responses (1 new),
Research/Application/Queries (1 new), Research/API/Controllers (1 new),
Integration (2 new adapters, 1 modified registration, 1 modified csproj),
Tests.Integration (1 new test class).

Key design decisions:
- `IBookFiguresService` (Core) gives the base position without a cross-module port — Research
  already depends on Core.
- `IHoldingTaxLotsReader` (new port) gives IBKR tax lot detail; Radar module needs no port for
  signals because `IAssetSignalReader` (new port) wraps `ListSignalsQuery`.
- All 7 sub-queries fan out via `Task.WhenAll`; each wrapped in try/catch so one failing source
  never 500s the whole response.
- Thesis lookup: filter `GetThesesQuery` result by ticker client-side (small list, user-scoped).
- Analyst actions: `GetAnalystActionsQuery` with Ticker filter, last 90 days, limit 20.
- Earnings: `GetEarningsCalendarQuery` with Tickers=[symbol], next 90 days, pick nearest.
- Signals: `IAssetSignalReader` returns last 30 days, limit 50; UI renders sparkline + latest.
- Integration csproj gains `BrokerageSync` reference (needed by `HoldingTaxLotsAdapter`).

### US2 — Dossier UI Page (next session)

Files touched: AppRoute enum, new `assets` feature module (models, service, store, page component),
app.routes.ts, holdings component (add click navigation).

Playwright spec: click holding → dossier renders; navigate back.

### US3 — Ledger's Read (future session)

Separate PR. Design TBD; likely: POST /research/assets/{symbol}/ledgers-read →
triggers agent, persists to new `asset_ledger_reads` table, GET returns cached.
