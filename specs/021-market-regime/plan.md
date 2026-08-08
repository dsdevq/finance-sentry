# Implementation Plan: Market Regime Scanner

**Branch**: `021-market-regime` | **Date**: 2026-08-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/021-market-regime/spec.md`

## Summary

Add a daily, deterministic macro-regime read on two orthogonal, evidence-backed axes — equity volatility (`^VIX`) and the rates/yield curve (FRED `DGS10`/`DGS2`, 10y-2y spread) — implemented **inside the existing Radar module**. A daily Hangfire job fetches VIX (via the shared `IMarketHistorySource`) and the two FRED series (via a new keyless-silent FRED source), classifies each axis into config-driven bands with a pure classifier, persists a `regime_readings` row (Radar migration **M002**), and appends to the shared `radar_signals` log (daily `info` per axis; one `regime_change` `notable` per axis that crosses a band). A `get_market_regime()` MCP tool returns both axes with raw readings and last-change dates. Finally, the 019 opportunity scoring path reads the latest regime through a Core cross-module port (`IMarketRegimeSource`) and applies a deterministic, documented, reversible haircut to a candidate's regime-adjusted structure score — context only, never an action (stay-invested default).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend only — no frontend changes)
**Primary Dependencies**: ASP.NET Core, EF Core 10 (Npgsql), Hangfire, `FinanceSentry.Core.Cqrs` (hand-rolled `ICommand`/`IQuery` — **no MediatR**), `System.Text.Json`, `IHttpClientFactory`, `ModelContextProtocol` (existing `FinanceSentry.Mcp`). **No new NuGet packages** — FRED is plain REST + JSON; VIX reuses the pinned Yahoo client.
**Storage**: PostgreSQL 14 — existing `RadarDbContext` (schema `radar`, history table `__ef_migrations_history_radar`). New table `regime_readings` via Radar migration **M002** (M001 exists). VIX and FRED series are fetched-and-classified, not stored as bars (only the computed reading persists).
**Testing**: xUnit + FluentAssertions + Moq + EF Core InMemory (mirrors `FinanceSentry.Modules.Radar.Tests` / `.Research.Tests`).
**Target Platform**: Linux container (dotnet sdk:10.0 for build/test; aspnet:10.0 runtime).
**Project Type**: Modular monolith backend + MCP tool surface. No UI.
**Performance Goals**: Two scalar time series per day — negligible. One VIX fetch + two FRED fetches per daily run; classification is O(bars) arithmetic.
**Constraints**: Zero `dotnet build` warnings. Migration MUST carry Designer + ModelSnapshot + `[DbContext]`/`[Migration]` attributes (known past failure otherwise). Regime NEVER auto-actions. FRED keyless ⇒ silent no-op.
**Scale/Scope**: One reading/day; two axes; ~10 new source files + 1 migration + ~6 test files; one new MCP tool; one Core port + adapter; one scorecard extension in Research.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Modular Monolith / domain interface boundaries** — PASS. Regime lives in the Radar module. The 019 (Research) consumer reads regime **only** through a Core-defined read-only port `IMarketRegimeSource` (implemented in Radar), so Research never references Radar directly — the same decoupling pattern as `IRadarSignalWriter`/`IMarketHistorySource`. The FRED external API is behind a domain interface (`IYieldCurveSource`), concrete impl registered in Infrastructure and resolved by DI.
- **II. Code Quality Enforcement** — PASS by construction. Every `.cs` compiled with `dotnet build backend/` to zero warnings before commit. File-scoped namespaces, primary constructors, no magic numbers (all thresholds in `RegimeOptions`).
- **III. Multi-source integration / graceful failure** — PASS. VIX outage → volatility axis skipped that day, no fabrication. FRED keyless/unreachable → rates axis silent. One axis failing never aborts the other or the run.
- **IV. AI-Driven Analytics** — N/A directly; the regime is deterministic context surfaced to the AI (Ledger) via MCP, not an LLM-generated artifact. Consistent with the "scorecard facts only / no false precision" roadmap stance.
- **V. Security-First** — PASS. `FRED_API_KEY` is config-bound (`.env.sops`), never logged. No user financial data touched by the macro read; the 019 coupling reads existing user-scoped holdings already inside its handler. MCP tool respects the existing identity resolver.
- **VI. Frontend State & Composition** — N/A. No frontend changes.
- **Testing Discipline** — Unit tests for the pure classifier (both axes, boundaries), FRED source (keyless/parse/latest-pair), signal emission (change vs no-change), and the scoring adjustment. The MCP tool is a thin pass-through over a query handler that is unit-tested. No new REST endpoint (MCP-only), so no REST contract test is required; a tool contract is documented under `contracts/`.

**Result**: PASS. No violations; Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/021-market-regime/
├── spec.md              # Feature spec (done)
├── plan.md              # This file
├── research.md          # Phase 0 — threshold evidence, FRED contract, module-placement, coupling design
├── data-model.md        # Phase 1 — RegimeReading entity, enums, signal shapes, port DTO
├── quickstart.md        # Phase 1 — how to run, configure keys, verify
├── contracts/
│   ├── get_market_regime.md          # MCP tool contract (input/output shape)
│   ├── fred-series-observations.md    # FRED external API contract + sample
│   └── market-regime-source-port.md   # IMarketRegimeSource cross-module port contract
├── checklists/
│   └── requirements.md  # Spec quality checklist (done, all-green)
└── tasks.md             # Phase 2 — /speckit.tasks output (NOT created here)
```

### Source Code (repository root)

```text
backend/src/FinanceSentry.Core/Interfaces/
└── IMarketRegimeSource.cs          # NEW — Core cross-module port + MarketRegimeSnapshot DTO (read-only)

backend/src/FinanceSentry.Modules.Radar/
├── Domain/
│   ├── Regime/
│   │   ├── RegimeReading.cs         # NEW — persisted entity (both axes + raw drivers)
│   │   ├── RegimeEnums.cs           # NEW — VolatilityRegime, RatesRegime, RegimeTrend enums
│   │   ├── RegimeClassifier.cs      # NEW — pure deterministic band classification (both axes)
│   │   └── YieldObservation.cs      # NEW — parsed (date,value) FRED point + latest-pair helper
│   └── RadarConstants.cs            # EDIT — add RadarScanners.MarketRegime + regime signal types
├── Application/
│   ├── Services/
│   │   └── RegimeOptions.cs         # NEW — config-bound thresholds (section "Regime")
│   ├── Commands/
│   │   └── ComputeMarketRegimeCommand.cs   # NEW — fetch+classify+persist+emit signals
│   └── Queries/
│       └── RegimeQueries.cs         # NEW — GetMarketRegimeQuery → RegimeStateDto
├── Domain/Repositories/
│   └── IRegimeReadingRepository.cs  # NEW — persist/read latest + prior
├── Infrastructure/
│   ├── MarketData/
│   │   ├── IYieldCurveSource.cs     # NEW — domain interface for the rates source
│   │   └── FredYieldCurveSource.cs  # NEW — keyless-silent FRED REST client
│   ├── Persistence/
│   │   ├── RadarDbContext.cs        # EDIT — add DbSet<RegimeReading> + mapping
│   │   └── Repositories/RegimeReadingRepository.cs  # NEW
│   └── Jobs/
│       └── RegimeComputeJob.cs      # NEW — daily Hangfire job
├── Migrations/
│   ├── <ts>_M002_RegimeReadings.cs          # NEW (EF-generated: Up/Down)
│   ├── <ts>_M002_RegimeReadings.Designer.cs # NEW (EF-generated — attributes present)
│   └── RadarDbContextModelSnapshot.cs       # EDIT (EF-updated)
├── RadarModule.cs                  # EDIT — register options/source/repo/job/HttpClient + IMarketRegimeSource

backend/src/FinanceSentry.API/Integration/
└── (adapter is registered by RadarModule; the port impl lives in Radar — no API glue needed
    unless a boundary adapter is cleaner. See research decision.)

backend/src/FinanceSentry.Mcp/Tools/
└── GetMarketRegimeTool.cs          # NEW — [McpServerTool(Name="get_market_regime")]

backend/src/FinanceSentry.Modules.Research/
├── Application/Services/OpportunityOptions.cs   # EDIT — add regime-haircut constants
├── Domain/Scoring/
│   ├── RegimeScoreAdjuster.cs      # NEW — pure regime→structure-score adjustment
│   └── CandidateScorecard.cs       # EDIT — add RegimeContext record + field
└── Application/Commands/ScoreCandidateCommand.cs  # EDIT — inject IMarketRegimeSource, apply adjuster

backend/tests/FinanceSentry.Modules.Radar.Tests/Regime/
├── RegimeClassifierTests.cs        # NEW — band boundaries, trend, inversion
├── FredYieldCurveSourceTests.cs    # NEW — keyless no-op, "." parse, latest-pair
└── ComputeMarketRegimeTests.cs     # NEW — change vs no-change signal emission, persistence

backend/tests/FinanceSentry.Modules.Research.Tests/Regime/
└── RegimeScoreAdjusterTests.cs     # NEW — haircut applied vs not, no-data passthrough, clamp
```

**Structure Decision**: Modular monolith. Regime is a self-contained area **inside** the Radar module (schema `radar`), reusing the shared `radar_signals` log, `IRadarSignalWriter`, `IMarketHistorySource`, `RadarDbContext`, and the module's Hangfire/config wiring — per the roadmap's "later scanners plug into the same signal log as small independent features." The only cross-module contact is the Core port `IMarketRegimeSource`, which the Research (019) module consumes to keep the two modules decoupled (Principle I).

## Complexity Tracking

No constitution violations — table intentionally empty.
