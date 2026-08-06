# Implementation Plan: Companion-Mode Data Layer

**Branch**: `030-companion-data-layer` | **Date**: 2026-07-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/030-companion-data-layer/spec.md`

## Summary

Give Ledger (the OpenClaw finance advisor) market-wide "companion" data it currently lacks: (P1) nightly analyst-actions ingestion from two free sources (MarketBeat daily ratings sweep + Yahoo `upgradeDowngradeHistory` per universe ticker) with logical dedup and source attribution; (P2) a `get_valuation_snapshot` MCP tool computing current metrics from Yahoo `quoteSummary` with trailing-P/E 5-year history reconstructed from EDGAR EPS × Yahoo closes (missing history flagged, never fabricated, snapshots persisted to accrue history); (P3) a `news_sources` registry enabling source-per-thesis registration (TrendForce → DRAM thesis) plus market-wide default feeds, with articles thesis-tagged. All query-side (no new push channels); ingestion failures surface via the existing `IAlertGeneratorService` sync-failure path. `CandidateSource` gains `Ledger` so companion ideas enter the existing opportunity pipeline.

## Technical Context

**Language/Version**: C# 13 / .NET 9 (backend only — no frontend changes)
**Primary Dependencies**: ASP.NET Core 9, EF Core 9 (Npgsql), Hangfire, `FinanceSentry.Core.Cqrs` (hand-rolled `ICommand`/`IQuery` — no MediatR), `ModelContextProtocol` SDK, **AngleSharp (new, MIT)** for HTML parsing (MarketBeat table, TrendForce page)
**Storage**: PostgreSQL 14 — existing `ResearchDbContext` (schema `research`), migration **M008_CompanionDataLayer** adding `analyst_actions`, `analyst_universe_members`, `news_sources`, `valuation_snapshots`; alter `news_articles` (+`ThesisIds` jsonb). Research migrations M001–M007 exist; next is M008.
**Testing**: xUnit — unit tests (dedup, P/E series math, universe sync, tagging, failure counters) + external-contract tests (Yahoo JSON shapes, MarketBeat/TrendForce HTML fixtures)
**Target Platform**: Linux server (Docker), consumed via `FinanceSentry.Mcp` (stdio + HTTP transports)
**Project Type**: modular-monolith backend module extension (Research) + MCP tool surface
**Performance Goals**: nightly ingestion completes < 10 min for ~550-ticker universe at 6-concurrent Yahoo throttle; `get_valuation_snapshot` < 5 s warm, < 15 s cold (EDGAR + quoteSummary fetches)
**Constraints**: free public sources only (no paid APIs); per-source failure isolation; no fabricated financial values (nulls + flags); no new push/notification channels
**Scale/Scope**: ~500-ticker seed universe + holdings/watchlist/candidates; ~2 sources × nightly; single user in practice (user-scoping only where data is user-owned — candidates; market data tables are global like `news_articles`)

## Constitution Check

*GATE: evaluated against constitution v1.3.1 — PASS (pre-Phase-0 and re-checked post-Phase-1).*

| Principle | Status | Notes |
|---|---|---|
| I. Modular monolith + domain interfaces | PASS | All new external access behind domain interfaces: `IAnalystActionsSource` (MarketBeat/Yahoo impls in Infrastructure), `IValuationDataService`, `INewsPageSource`. No module references a concrete adapter; DI-registered in `ResearchModule` |
| II. Code quality (zero warnings) | PASS (process) | `dotnet build backend/` zero-warning gate after every file; `/csharp-quality` sweep before PR |
| III. Multi-source integration resilience | PASS | Per-source isolation: one source failing never blocks the other (spec edge case); failure counters + alert at 2 consecutive; `[DisableConcurrentExecution]` on the job; every source treated as unreliable-by-default |
| IV. AI-driven analytics | PASS | The entire feature exists to fuel Ledger's (LLM) advisory output; snapshots cached/persisted |
| V. Security | PASS | No new secrets; market data is global (existing precedent: `news_articles`, `quote_cache`); user-owned data (candidates) stays user-scoped; no data leaves the system (query-side only) |
| VI. Frontend discipline | N/A | No frontend changes |
| Testing discipline | PASS | External-contract tests for every new external source (constitution-mandated); no REST endpoints → no REST contract tests; TDD at handler level |
| Versioning | PASS (process) | Backend `<Version>` bump in `FinanceSentry.API.csproj` in the PR; `backend-v*` tag on merge |

**New dependency justification (AngleSharp)**: HTML sources (MarketBeat, TrendForce) require structural parsing so that markup drift becomes a *visible* source failure (FR-009) instead of silent corruption. MIT-licensed, zero transitive dependencies. Recorded in Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/030-companion-data-layer/
├── plan.md              # This file
├── research.md          # Phase 0 — 10 resolved decisions (sources, parsing, history math, universe, alerting)
├── data-model.md        # Phase 1 — 4 new tables, 1 alter, enums, domain interfaces
├── quickstart.md        # Phase 1 — per-story verification commands
├── contracts/
│   └── mcp-tools.md     # Phase 1 — 4 new + 2 extended MCP tools; external source contracts
└── tasks.md             # Phase 2 (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
backend/src/FinanceSentry.Modules.Research/
├── Domain/
│   ├── AnalystAction.cs, AnalystActionType.cs
│   ├── AnalystUniverseMember.cs, UniverseReason.cs
│   ├── NewsSource.cs, NewsSourceKind.cs
│   ├── ValuationSnapshot.cs
│   ├── Opportunity/CandidateSource.cs            # + Ledger value (edit)
│   └── Interfaces/
│       ├── IAnalystActionsSource.cs
│       ├── IValuationDataService.cs
│       ├── INewsPageSource.cs
│       └── (repos: IAnalystActionRepository, IAnalystUniverseRepository,
│                INewsSourceRepository, IValuationSnapshotRepository)
├── Application/
│   ├── Queries/  GetAnalystActionsQuery.cs, GetValuationSnapshotQuery.cs,
│   │             ListNewsSourcesQuery.cs (+ SearchMarketNewsQuery thesis filter edit)
│   ├── Commands/ RegisterThesisSourceCommand.cs
│   └── Services/ ValuationHistoryService.cs      # EDGAR EPS × closes → trailing P/E series
│                 AnalystUniverseService.cs        # seed ∪ holdings ∪ watchlist ∪ candidates sync
├── Infrastructure/
│   ├── Sources/  MarketBeatAnalystActionsSource.cs, YahooAnalystActionsSource.cs,
│   │             YahooValuationDataService.cs, TrendForcePageSource.cs
│   ├── Jobs/     AnalystActionsIngestionJob.cs   # nightly 01:00 UTC (+ valuation capture)
│   │             NewsIngestionJob.cs             # edit: iterate news_sources
│   ├── Persistence/ (DbContext DbSets + repositories + M008 migration WITH Designer)
│   └── Resources/ sp500-constituents.json        # checked-in seed
├── ResearchModule.cs                             # DI, named HttpClients, JobRegistrar edits

backend/src/FinanceSentry.Mcp/Tools/
├── GetAnalystActionsTool.cs, GetValuationSnapshotTool.cs,
├── RegisterThesisSourceTool.cs, ListNewsSourcesTool.cs
└── (SearchMarketNewsTool edit: thesisId param)

backend/tests/ (module test project, mirroring existing layout)
├── contract/  Yahoo shapes (live-tolerant), MarketBeat + TrendForce fixtures
└── unit/      dedup, P/E series, universe sync, tagging, failure counters
```

**Structure Decision**: extend the existing Research module (entities/queries already colocated there: theses, news, candidates); no new module — analyst actions and valuation are research-domain concerns and share the DbContext, Yahoo clients, and job registrar. MCP tools follow assembly auto-discovery (no manual registration).

## Migration discipline (lesson 2026-07-20)

M008 MUST be generated with its `.Designer.cs` (or carry `[DbContext]`/`[Migration]` attributes) — the M007 outage was caused by a hand-written migration EF could not discover. Verify post-implementation: `SELECT "MigrationId" FROM public.__ef_migrations_history_research` contains M008 after container start.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| New NuGet package (AngleSharp) | Structural HTML parsing of MarketBeat/TrendForce so markup drift fails loudly (FR-009) | Regex/string parsing hides drift as silently-empty results — exactly the failure mode the spec forbids |
