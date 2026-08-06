# Implementation Plan: Market Structure Scanner + Radar Signal Log

**Branch**: `018-market-structure` | **Date**: 2026-07-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/018-market-structure/spec.md`

## Summary

Introduce a **new `FinanceSentry.Modules.Radar` module** that (1) ingests and persists daily
OHLCV+adjclose bars for a configurable universe, (2) computes deterministic market-structure
metrics (returns, relative strength, sector rotation, breadth, unusual-move z-scores, extension)
as pure functions over the persisted bars, and (3) owns the shared append-only **`radar_signals`**
platform table that every Radar scanner (017 follow-ups, 019, future scanners) writes to via a
Core `IRadarSignalWriter` interface. The module launches in **log-only calibration mode** (signals
recorded, zero alerts) and only raises domain Alerts (new `AlertType.MarketStructure`, held tickers
only) after thresholds are set from observed distributions + a ≥5-year historical replay. Six MCP
read tools expose structure and signals. No LLM, no channel delivery, no paid APIs — tier 1.

## Technical Context

**Language/Version**: C# 13 / .NET 9
**Primary Dependencies**: ASP.NET Core 9, EF Core 9 (Npgsql), Hangfire, Serilog, `FinanceSentry.Core.Cqrs` (hand-rolled `ICommand`/`IQuery` + handlers — **no MediatR**), `ModelContextProtocol` (MCP SDK). No new NuGet packages (Yahoo chart endpoint is plain REST the existing client already calls).
**Storage**: PostgreSQL 14 — **new `RadarDbContext`** (schema `radar`, history table `__ef_migrations_history_radar`), migration `M001_InitialSchema` creating `daily_bars`, `radar_signals`, `radar_universe_members`. No changes to existing contexts.
**Testing**: xUnit — new `backend/tests/FinanceSentry.Modules.Radar.Tests` for the pure computation core + ingestion idempotency; parity/allowlist in `FinanceSentry.Mcp.Tests`.
**Target Platform**: Linux (Docker) server — backend + MCP only, no SPA.
**Project Type**: New backend module in the modular monolith.
**Performance Goals**: SC-003 — daily ingest+compute+emit < 5 min for a 100-ticker universe (Yahoo batched with per-request throttle; bars persisted so compute is local).
**Constraints**: All metrics pure over persisted bars (FR-012); append-only signal log (FR-007); log-only launch (FR-015); reads never trigger ingestion (FR-011); staleness flagged never silently trusted (FR-017); zero paid/channel deps (SC-005).
**Scale/Scope**: Universe ~30–100 tickers (SPY + 11 SPDR sectors + SMH seed + holdings + watchlist), ~300 bars each; single primary user (Denys).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Gate | Status | Notes |
|---|---|---|
| I. Modular monolith; external integration behind domain interface | PASS | New self-contained `Modules.Radar`. Yahoo history goes behind a **new thin `IMarketHistorySource`** (Core) so a fallback provider (Stooq) is swappable — Yahoo is never a hard-wired SPOF. Cross-module writes use Core interfaces only: `IRadarSignalWriter` (Radar impl) for other scanners; `IWatchlistReader`/`IBrokerageHoldingsReader` (Core) for the universe. No module→module concrete references. |
| I. No cross-module coupling for universe | PASS | `IBrokerageHoldingsReader` already in Core. **Add `IWatchlistReader` to Core** (impl in Research) so Radar reads watchlist tickers without depending on the Research module's internal `IWatchlistRepository`. |
| II. Zero-warning `dotnet build` | GATE | Enforced per-file. |
| II. CQRS via Core.Cqrs | PASS | All six read tools back onto `IQuery`/`IQueryHandler`; scanner runs are commands/jobs. |
| III. Multi-source integration, graceful failure | PASS | Ingestion is per-ticker isolated (one ticker's failure never aborts the run; recorded in run summary — FR-002/US1.3); freshness watchdog raises an Alert on stale/failed data (FR-017). |
| IV. AI analytics | N/A | Deterministic tier 1 only; interpretation stays with Ledger. |
| V. Security / user-scoping | PASS | Bars and market-structure are global (SPY is SPY); **held-ticker** signals and holdings-derived universe membership are user-scoped via `IBrokerageHoldingsReader(userId)` and the alerting path resolves the owning user. MCP tools resolve `userId` from `IIdentityResolver`. |
| VI. Frontend discipline | N/A | No frontend in v1 (spec out-of-scope). |
| Testing — unit >80% on computation core, Test-First | GATE | Every metric is a pure function unit-tested incl. short-history/zero-volume/missing-day edges (SC-001); historical-replay validation (SC-002) reuses the same pure functions. |
| Testing — MCP tool parity/allowlist | GATE | Add 6 names to `ToolNameContractTests` allowlist (33→39) + parity facts; register Radar services/fakes in `ToolParityTests`. |
| Testing — REST endpoint contract | N/A | No REST endpoint (MCP + Hangfire surface). |
| Migration convention `M00x_Name` + per-module history | PASS | Radar owns `M001_InitialSchema`; history table `__ef_migrations_history_radar`. Central edit: register `MigrateContext<RadarDbContext>` in `FinanceSentry.API/Migrations/MigrationExtensions.cs`. |
| Versioning/tagging | CONDITIONAL | No REST contract change → no API `<Version>` bump required. If a controller is later added, bump + contract test in same PR. |

**No violations.** Complexity Tracking omitted.

## Project Structure

### Documentation (this feature)

```text
specs/018-market-structure/
├── plan.md          # This file
├── research.md      # Phase 0 — decisions (module boundary, history source, calibration, dedup)
├── data-model.md    # Phase 1 — DailyBar, RadarSignal, RadarUniverseMember, DTOs, metric defs
├── quickstart.md    # Phase 1 — run/ingest/compute/query steps
├── contracts/       # Phase 1 — the 6 MCP tool contracts
└── tasks.md         # Phase 2 (/speckit.tasks — NOT created here)
```

### Source Code (new module + surgical Core/central edits)

```text
backend/src/
├── FinanceSentry.Core/Interfaces/
│   ├── IRadarSignalWriter.cs          [NEW: AppendSignalAsync(...) + RadarSignalRequest record — cross-module append point]
│   ├── IMarketHistorySource.cs        [NEW: GetDailyBarsAsync(ticker, since, ct) → IReadOnlyList<DailyBarData>; swappable provider]
│   └── IWatchlistReader.cs            [NEW: ListTickersAsync(userId, ct) — Core read of Research watchlist for the universe]
├── FinanceSentry.Modules.Research/
│   └── Application/Services/WatchlistReader.cs   [NEW: impl of IWatchlistReader over IWatchlistRepository; registered in ResearchModule]
├── FinanceSentry.Core/Interfaces/IAlertGeneratorService.cs  [MODIFY: + GenerateMarketStructureAlertAsync + freshness alert methods]
├── FinanceSentry.Modules.Alerts/
│   ├── Domain/AlertType.cs             [MODIFY: + const MarketStructure]
│   └── Application/Services/AlertGeneratorService.cs  [MODIFY: impl new methods + silence windows]
└── FinanceSentry.Modules.Radar/                 [NEW MODULE]
    ├── RadarModule.cs                 # ModuleRegistrar + JobRegistrar + AddRadarModule (DbContext, repos, services, jobs, IRadarSignalWriter, IJobRegistrar singleton)
    ├── Domain/
    │   ├── DailyBar.cs                # Ticker, Date, Open, High, Low, Close, AdjClose, Volume
    │   ├── RadarSignal.cs             # Timestamp, Scanner, SignalType, Severity, SubjectType, Subject, DedupKey, Payload(jsonb), PayloadVersion, UserId?
    │   ├── RadarUniverseMember.cs     # Ticker, Kind, Source, Active
    │   ├── Enums.cs                   # SignalSeverity(info|notable|alerted), UniverseKind, SignalSubjectType, ScannerMode
    │   ├── Repositories/              # IDailyBarRepository, IRadarSignalRepository, IRadarUniverseRepository
    │   └── MarketStructure/           # PURE computation core (no I/O)
    │       ├── ReturnMath.cs          # returns over N trading days; RS vs benchmark
    │       ├── MovingAverages.cs      # 20/50/200 MA; extension from 50MA
    │       ├── Volatility.cs          # 63-day σ; today z-score; volume ratio
    │       ├── SectorRotation.cs      # rank sectors by RS; rank deltas
    │       ├── Breadth.cs             # % above 20/50/200 MA
    │       └── MarketStructureCalculator.cs  # composes the above per ticker
    ├── Application/
    │   ├── Services/
    │   │   ├── IRadarUniverseService.cs / RadarUniverseService.cs  # universe = seed ∪ holdings(equity) ∪ watchlist; auto-sync membership
    │   │   ├── RadarSignalWriter.cs   # impl IRadarSignalWriter (dedup on DedupKey + silence window for notable+; append-only)
    │   │   └── SignalThresholds.cs    # config-bound thresholds (FR-008) + ScannerMode (log-only vs alerting, FR-015)
    │   ├── Commands/
    │   │   ├── IngestDailyBarsCommand.cs        # per-run, per-ticker isolated; run summary
    │   │   ├── ComputeMarketStructureCommand.cs # emits structure signals + held-ticker alerts (mode-gated)
    │   │   └── RunHistoricalValidationCommand.cs # FR-016 one-off replay over persisted bars
    │   └── Queries/                   # GetMarketStructure, GetRelativeStrength, GetSectorRotation, GetMarketBreadth, ListSignals, GetRadarSummary
    ├── Infrastructure/
    │   ├── Persistence/               # RadarDbContext, RadarDbContextFactory, Repositories/*
    │   ├── MarketData/YahooMarketHistorySource.cs  # impl IMarketHistorySource: full OHLCV+adjclose from /v8/finance/chart
    │   └── Jobs/                      # RadarIngestionJob (daily post-close), RadarComputeJob, RadarFreshnessWatchdogJob
    └── Migrations/                    # …_M001_InitialSchema.cs + RadarDbContextModelSnapshot.cs

backend/src/FinanceSentry.API/
├── FinanceSentry.API.csproj                     [MODIFY: ProjectReference → Modules.Radar so assembly loads for reflection discovery]
└── Migrations/MigrationExtensions.cs            [MODIFY: MigrateContext<RadarDbContext>(sp, logger) + using]

backend/src/FinanceSentry.Mcp/
├── FinanceSentry.Mcp.csproj                     [MODIFY: ProjectReference → Modules.Radar for tool DI]
└── Tools/                                       [NEW ×6: GetMarketStructureTool, GetRelativeStrengthTool, GetSectorRotationTool, GetMarketBreadthTool, ListSignalsTool, GetRadarSummaryTool]

backend/tests/
├── FinanceSentry.Modules.Radar.Tests/           [NEW project; add to FinanceSentry.sln]
│   ├── MarketStructure/*                         # pure-function unit tests (SC-001)
│   ├── Ingestion/IngestDailyBarsIdempotencyTests.cs
│   └── HistoricalValidation/ReplayTests.cs       # SC-002 seeded episodes
└── FinanceSentry.Mcp.Tests/
    ├── ContractTests/ToolNameContractTests.cs    [MODIFY: +6 names, 33→39]
    └── IntegrationTests/ToolParityTests.cs       [MODIFY: register Radar services + FakeMarketHistorySource; +6 parity facts]
```

**Structure Decision**: A **new module**, not an extension of Research, because (a) persisting bars is
the deliberate opposite of 017's "no new time-series" rule and deserves its own bounded context, and
(b) `radar_signals` is a shared platform table many scanners write to — burying it in Research would
force every future scanner to couple to Research. The cross-module seam is a Core `IRadarSignalWriter`,
mirroring the proven `IAlertGeneratorService` pattern (defined in Core, implemented in one module,
injected by others with no module reference). Module wiring is auto-discovered
(`IModuleRegistrar`/`IJobRegistrar` reflection); the only required central edits are the
migrate-on-startup registration, two `.csproj` references, and the MCP allowlist.

## Complexity Tracking

*No constitution violations — table intentionally empty.*
