# Implementation Plan: Thesis Break Monitor

**Branch**: `017-thesis-monitor` | **Date**: 2026-07-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/017-thesis-monitor/spec.md`

## Summary

Deterministic, tier-1 backend service that evaluates every active `InvestmentThesis`'s
`InvalidationTriggers` against reported fundamentals (EDGAR) and daily price closes (Yahoo)
on a Hangfire schedule and on-demand via MCP. On an unbroken→broken transition it sets
`BrokenAt`/`BrokenReason` and raises exactly one domain `ThesisBroken` alert through the
existing Alerts module; on a cleared condition it un-breaks and resolves the alert. No LLM,
no new time-series persistence, no notification delivery. Reuses existing `InvestmentThesis`
schema (extends the `ThesisInvalidationTrigger` jsonb record with `ProxyTicker`,
`ConsecutivePeriods`, `PeriodType`), the Research module's `JobRegistrar`, and the Core
`IAlertGeneratorService` contract.

## Technical Context

**Language/Version**: C# 13 / .NET 9
**Primary Dependencies**: ASP.NET Core 9, EF Core 9, Hangfire, Serilog, `ModelContextProtocol` (MCP SDK); no new NuGet packages
**Storage**: PostgreSQL 14 — existing `research.theses` table; migration **M004** rewrites the `invalidation_triggers` jsonb shape and backfills the two seeded theses. No new tables required (optional `ThesisMonitorRun` observability table deferred to P2).
**Testing**: xUnit (`backend/tests/FinanceSentry.Modules.Research.Tests` for the evaluator; `FinanceSentry.Mcp.Tests` for tool parity/attribute contracts)
**Target Platform**: Linux server (Docker Compose stack)
**Project Type**: Backend module in the modular monolith (Research module + Core interface + Alerts const)
**Performance Goals**: SC-004 — a full user thesis-set run completes in < 2 minutes (bounded by EDGAR/Yahoo fetch latency; fundamentals cached 12h, CIK map 24h)
**Constraints**: Deterministic evaluation (SC-001); zero false breaks on missing/insufficient data (SC-002); exactly one alert per transition (SC-003); no messaging dependency in the module (SC-007)
**Scale/Scope**: Single-digit theses per user today (2 seeded); designed for tens per user. Closed 12-metric vocabulary.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate (constitution) | Status | Notes |
|---|---|---|
| I. Modular monolith — no cross-module coupling | PASS | Research reaches Alerts only through `Core.Interfaces.IAlertGeneratorService`; EDGAR/Yahoo already behind `ISecEdgarService`/`IMarketDataService`. No direct module reference. |
| I. External integration behind domain interface | PASS | Price-history added as a new method on existing `IMarketDataService`; no new concrete adapter leaks into module logic. |
| II. Zero-warning `dotnet build` | GATE | Enforced per-file; run `dotnet build backend/` after each `.cs`. |
| II. CQRS via Core.Cqrs | PASS | On-demand path is an `ICommand`/`ICommandHandler` (`RunThesisMonitorCommand`) + `IQuery` (`ListThesisBreaksQuery`), matching module convention (hand-rolled Core.Cqrs, not MediatR). |
| Testing — unit >80% on business logic, Test-First | GATE | Evaluator is pure/deterministic → unit-tested first (consecutive-period, YoY, proxy, div-by-zero, price-drawdown). |
| Testing — MCP tool parity/attribute contract | GATE | Add parity facts for `run_thesis_monitor` + `list_thesis_breaks` in `ToolParityTests`; structural attribute test auto-covers. |
| Testing — REST endpoint contract | N/A | No new REST endpoint (MCP + Hangfire surface only). If a controller is added, a contract test ships in the same PR. |
| Migration convention `M00x_Name` + per-module history | PASS | `M004_ThesisTriggerV2` in `Modules.Research/Migrations`. |
| Versioning/tagging | CONDITIONAL | No REST contract change → no `FinanceSentry.API.csproj` bump required unless a controller is added. |

**No violations.** Complexity Tracking table omitted.

## Project Structure

### Documentation (this feature)

```text
specs/017-thesis-monitor/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (MCP tool schemas)
│   ├── run_thesis_monitor.md
│   └── list_thesis_breaks.md
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/src/
├── FinanceSentry.Core/
│   └── Interfaces/
│       └── IAlertGeneratorService.cs         # + GenerateThesisBreakAlertAsync, ResolveThesisBreakAlertAsync
├── FinanceSentry.Modules.Alerts/
│   ├── Domain/AlertType.cs                    # + const string ThesisBroken
│   └── Application/Services/AlertGeneratorService.cs  # impl of the two new methods
└── FinanceSentry.Modules.Research/
    ├── Domain/
    │   ├── InvestmentThesis.cs                # extend ThesisInvalidationTrigger record; add PeriodType enum
    │   └── ThesisMonitor/                     # new: metric vocabulary, evaluator, verdict types
    │       ├── ThesisMetric.cs               # closed vocabulary (const strings / enum)
    │       ├── TriggerVerdict.cs             # Breached | Held | NonEvaluable(+reason)
    │       └── ThesisBreakEvaluator.cs       # pure deterministic core
    ├── Application/
    │   ├── Services/IMarketDataService.cs     # + GetDailyClosesAsync
    │   ├── Services/YahooMarketDataService.cs # impl (retain bar series from chart response)
    │   ├── Commands/RunThesisMonitorCommand.cs + Handler
    │   ├── Queries/ListThesisBreaksQuery.cs   + Handler
    │   └── Validation/ThesisTriggerVocabulary.cs  # save-path vocabulary guard (FR-012)
    ├── Infrastructure/Jobs/ThesisMonitorJob.cs # Hangfire ExecuteAsync
    ├── Migrations/…_M004_ThesisTriggerV2.cs    # jsonb reshape + seeded-thesis backfill
    └── ResearchModule.cs                       # register job in JobRegistrar + AddScoped

backend/src/FinanceSentry.Mcp/Tools/
├── RunThesisMonitorTool.cs                     # [McpServerTool(Name="run_thesis_monitor")]
└── ListThesisBreaksTool.cs                     # [McpServerTool(Name="list_thesis_breaks")]

backend/tests/
├── FinanceSentry.Modules.Research.Tests/ThesisMonitor/  # evaluator unit tests (Test-First)
└── FinanceSentry.Mcp.Tests/IntegrationTests/ToolParityTests.cs  # + 2 parity facts
```

**Structure Decision**: Backend-only feature living primarily in the **Research module**, with
two surgical touches to shared surfaces (`Core.Interfaces.IAlertGeneratorService`,
`Modules.Alerts` const + impl). The deterministic evaluator is isolated under
`Domain/ThesisMonitor/` as pure logic so it is unit-testable without EF/HTTP. The on-demand
path uses the module's Core.Cqrs convention; MCP tools are thin wrappers over the command/query.

## Complexity Tracking

*No constitution violations — table intentionally empty.*
