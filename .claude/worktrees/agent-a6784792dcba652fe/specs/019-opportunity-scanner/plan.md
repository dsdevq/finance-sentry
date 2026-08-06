# Implementation Plan: Opportunity Scanner

**Branch**: `019-opportunity-scanner` | **Date**: 2026-07-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/019-opportunity-scanner/spec.md`

## Summary

Deterministic scoring of opportunity candidates from Denys's convictions (v1), with a lifecycle
`Active → Promoted | Rejected | Expired`. A `score_candidate(ticker)` call produces an explainable
**scorecard** — a structure sub-score (from 018), a fundamentals sub-score (from EDGAR/017 metric
concepts), a crowding classification (from 018 extension + volume), and factual IPS-fit — with **no
composite single number** (FR-007). Promotion turns a candidate into a monitored `InvestmentThesis`
(017) with deterministically prefilled invalidation triggers, **gated by 022's risk policy**
(refuses an oversized bet, overridable explicitly). Every candidate — including rejects — is retained
and price-stamped through 020's event recorder so hit rates and counterfactuals are measurable. Lives
in the **Research module** (which already owns thesis, fundamentals, watchlist, IPS, thesis-events).
Two new Core seams keep module boundaries: `IMarketStructureReader` (Radar impl) and `IRiskPolicyGate`
(Risk impl). Tier 1: computes and stores; Ledger interprets.

## Technical Context

**Language/Version**: C# 13 / .NET 9
**Primary Dependencies**: ASP.NET Core 9, EF Core 9, `FinanceSentry.Core.Cqrs` (hand-rolled ICommand/IQuery — no MediatR), `ModelContextProtocol` (MCP SDK). No new NuGet packages.
**Storage**: PostgreSQL 14 — existing `ResearchDbContext` (schema `research`), migration **M006_OpportunityCandidates** adding `opportunity_candidates` + `candidate_scores`. (Research migrations: M001–M005 exist; next is M006.)
**Testing**: xUnit — `FinanceSentry.Modules.Research.Tests` for the pure scoring functions (SC-001) + promote/reject/expire handlers; `FinanceSentry.Mcp.Tests` allowlist/parity.
**Target Platform**: Linux (Docker) — backend + MCP only, no SPA.
**Project Type**: Backend feature extending the Research module + two Core interfaces (impls in Radar/Risk).
**Performance Goals**: SC-002 — `score_candidate` end-to-end (live 018 structure + EDGAR) < 10s.
**Constraints**: Deterministic + fully explainable, every sub-score cites inputs/periods/`FormulaVersion` (FR-002); **no composite score** (FR-007); no LLM/randomness; not-evaluable is labeled, never neutral-faked; promotion runs the 022 gate (FR-011b); never executes trades / no channel delivery (FR-014).
**Scale/Scope**: Single user; handful of candidates. v1 = conviction scoring + promote/reject/expire.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Gate | Status | Notes |
|---|---|---|
| I. Modular monolith; no cross-module coupling | PASS | Candidate lives in Research. Reads 018 structure via **new Core `IMarketStructureReader`** (Radar impl); calls 022 via **new Core `IRiskPolicyGate`** (Risk impl) — never referencing the Radar/Risk modules directly. Emits signals via existing Core `IRadarSignalWriter`; records events via in-module `IThesisEventRecorder`; alerts via Core `IAlertGeneratorService`. |
| II. Zero-warning `dotnet build` | GATE | Enforced per file. |
| II. CQRS via Core.Cqrs | PASS | `score_candidate`/`list_candidates` are queries/commands; promote/reject/expire are commands. |
| IV. AI analytics | N/A (intentional) | Deterministic tier 1 by spec — interpretation is Ledger's; no LLM in any scoring path. |
| V. Security / user-scoping | PASS | All candidate rows + reads user-scoped; MCP tools resolve `userId` via `IIdentityResolver`. |
| VI. Frontend discipline | N/A | No frontend in v1. |
| Testing — pure scoring unit-tested, Test-First | GATE | Structure/fundamentals/crowding normalizers + trigger-prefill are pure functions; identical inputs → identical scorecards incl. partial/not-evaluable (SC-001). |
| Testing — MCP allowlist/parity | GATE | 4 new tool names (43→47); register new injected interfaces in `ToolParityTests`. |
| Testing — REST contract | N/A | MCP + in-process only; no new REST endpoint. |
| Migration `M00x_Name` + history | PASS | `M006_OpportunityCandidates` in Research context. |
| Versioning/tagging | CONDITIONAL | No REST contract change → no API `<Version>` bump. |

**No violations.** Complexity Tracking omitted.

## Project Structure

```text
backend/src/
├── FinanceSentry.Core/Interfaces/
│   ├── IMarketStructureReader.cs      [NEW: GetStructureAsync(ticker,ct) → MarketStructureSnapshot (Core DTO); cross-module structure read]
│   └── IRiskPolicyGate.cs             [NEW: CheckProposalAsync(userId, ticker, proposedUsd, override, ct) → RiskGateVerdict (Core DTO)]
├── FinanceSentry.Modules.Radar/
│   └── Application/Services/MarketStructureReader.cs   [NEW: impl IMarketStructureReader over IStructureQueryService; registered in RadarModule]
├── FinanceSentry.Modules.Risk/
│   └── Application/Services/RiskPolicyGate.cs          [NEW: impl IRiskPolicyGate over CheckRiskRulesQueryHandler; registered in RiskModule]
├── FinanceSentry.Modules.Alerts/
│   ├── Domain/AlertType.cs            [MODIFY: + const Opportunity]
│   └── Application/Services/AlertGeneratorService.cs   [MODIFY: + GenerateOpportunityAlertAsync]
├── FinanceSentry.Core/Interfaces/IAlertGeneratorService.cs [MODIFY: + GenerateOpportunityAlertAsync]
└── FinanceSentry.Modules.Research/
    ├── Domain/
    │   ├── OpportunityCandidate.cs    [NEW: Id,UserId,Ticker,Source(User|Scan),Status,CreatedAt,ExpiresAt,PromotedThesisId?,RejectedReason?,NominationReasons(jsonb)]
    │   ├── CandidateScore.cs          [NEW, append-only: CandidateId,ScoredAt,StructureScore?,FundamentalsScore?,CrowdingClass,IpsFit(jsonb),Evidence(jsonb),FormulaVersion — NO composite]
    │   ├── Opportunity/               [NEW enums: CandidateSource, CandidateStatus, CrowdingClass(Early|Normal|Extended)]
    │   ├── Scoring/                    # PURE scoring core (no I/O)
    │   │   ├── StructureScorer.cs     # TickerStructure → 0–100 by fixed piecewise rules (RS, sector, breakout) + evidence
    │   │   ├── FundamentalsScorer.cs  # FundamentalFact series → 0–100 (rev YoY, margin level+trend, EPS YoY) + evidence; not-evaluable
    │   │   ├── CrowdingClassifier.cs  # extension + volume ratio → Early|Normal|Extended by config thresholds
    │   │   ├── FundamentalMath.cs     # shared margin/YoY + concept-name mapping extracted from ThesisBreakEvaluator (reuse, not duplicate)
    │   │   └── TriggerPrefill.cs      # deterministic proposed triggers from scorecard (FR-011 formula)
    │   └── Repositories/ (ICandidateRepository, ICandidateScoreRepository)
    ├── Application/
    │   ├── Services/WatchlistReader.cs (existing) …
    │   ├── Commands/ (ScoreCandidateCommand, PromoteCandidateCommand, RejectCandidateCommand, ExpireCandidatesCommand)
    │   └── Queries/ (ListCandidatesQuery)
    ├── Infrastructure/
    │   ├── Persistence/ (ResearchDbContext [MODIFY +2 DbSets/config], Repositories/*)
    │   └── Jobs/CandidateExpiryJob.cs [NEW: daily — expire candidates past TTL with a final score snapshot]
    ├── Migrations/…_M006_OpportunityCandidates.cs
    └── ResearchModule.cs              [MODIFY: register candidate repos, scorers, expiry job, IMarketStructureReader/IRiskPolicyGate consumers]

backend/src/FinanceSentry.Mcp/Tools/  [NEW ×4]
├── ScoreCandidateTool.cs      # score_candidate(ticker)
├── ListCandidatesTool.cs      # list_candidates(status?, source?)
├── PromoteCandidateTool.cs    # promote_candidate(id, triggers?) — runs the 022 gate
└── RejectCandidateTool.cs     # reject_candidate(id, reason)

backend/tests/
├── FinanceSentry.Modules.Research.Tests/Opportunity/   [NEW: scorer + handler tests]
└── FinanceSentry.Mcp.Tests/ (allowlist 43→47; parity: register IMarketStructureReader, IRiskPolicyGate, candidate repos)
```

**Structure Decision**: Extend **Research** (owns every in-process dependency: thesis, EDGAR, watchlist,
IPS, thesis-events). The only cross-module needs — read market structure (018) and check risk policy
(022) — are added as **Core interfaces implemented in Radar/Risk**, mirroring the `IRadarSignalWriter`
/ `IBrokenThesisReader` seams already established, so Research never references those modules. The
fundamentals sub-score **reuses** 017's concept-mapping/margin/YoY logic by extracting a shared
`FundamentalMath` helper (no silent duplication of the concept table).

## Scope (v1) & explicit deferrals

- **In v1**: US1 (conviction `score_candidate`), US3 (promote/reject/expire). 4 MCP tools.
- **Deferred to v2 (per spec US2/FR-008)**: `scan_opportunities` machine scan — gated on 018's
  calibration/historical validation. Not built now; the 5th tool ships with it.
- **Deferred to v1.1 (documented)**: FR-006b "what's priced in"/valuation-history and FR-006c
  base-rate annotation — both require a seeded reference-class/valuation dataset; deferring avoids
  inventing authoritative base-rate numbers (consistent with the "don't be source of truth for
  taxonomies" principle). The scorecard leaves an extensible `Evidence` slot for them.

## Complexity Tracking

*No constitution violations — table intentionally empty.*
