# Implementation Plan: IPS ↔ Risk Rules Boundary Cleanup

**Branch**: `039-ips-risk-boundary` | **Date**: 2026-08-07 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/039-ips-risk-boundary/spec.md`

## Summary

Two bounded contexts each store an independently-editable copy of the same two policy concepts. Give each concept exactly one home and repoint every reader, with **zero behavioural drift** where the two copies currently agree (SC-002).

- **Target asset-class allocation (+ rebalance/drift band)** → sole home = **IPS** (`InvestmentPolicyStatement`, Research module). The Risk Rule Set drops its `AllocationTargets` copy.
- **Maximum single-position cap** → sole home = **Risk Rule Set** (`MaxPositionWeightPct`, Risk module). The IPS drops its `MaxSinglePositionPct` copy.

Technical approach: the four evaluation readers currently split across the two records get repointed to the single home via **read-only cross-module ports** (Principle I contracts, not direct DbContext coupling). Two EF migrations (one per DbContext, same physical Postgres DB) reconcile existing values into the single home before dropping the duplicate columns — with a documented, deterministic, idempotent reconciliation rule. The three write/read contracts (2 MCP tools + 1 REST endpoint pair) drop the moved fields. Correctness is proven by **characterization tests** that snapshot current drift verdicts, cap-enforcement outcomes, and candidate scores and assert byte-for-byte equality after the move.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend only — no frontend changes)
**Primary Dependencies**: ASP.NET Core, EF Core 10 (Npgsql), `FinanceSentry.Core.Cqrs` (hand-rolled `ICommand`/`IQuery` — **no MediatR**), `ModelContextProtocol` (existing `FinanceSentry.Mcp` project). No new NuGet packages.
**Storage**: PostgreSQL 14 — a single physical DB hosting both schemas: `research` (`ResearchDbContext`, table `investment_policy_statements`) and `risk` (`RiskDbContext`, table `risk_rule_sets`). Next migrations: Research **M012**, Risk **M002**.
**Testing**: xUnit (backend). Unit tests for reconciliation rules; characterization tests for zero-drift; contract tests for the changed REST endpoint (mandatory per constitution).
**Target Platform**: Linux server (Docker); production on VPS self-hosted runner.
**Project Type**: Backend modular monolith — two existing modules (Research, Risk); no new module (FR-015).
**Performance Goals**: N/A — structural cleanup; evaluation paths unchanged in cost (one extra cross-schema read per evaluation, already within a request/job boundary).
**Constraints**: Zero behavioural drift where copies agree (SC-002); idempotent migration (SC-004, FR-012); no value lost/reset/fabricated (SC-003, FR-008–FR-011); no new user-facing capability, module, or behaviour (FR-015).
**Scale/Scope**: Single active user in production; reconciliation conflicts unlikely but the rule is implemented for correctness. 2 entities, 4 evaluation readers, 2 new cross-module read ports, 2 EF migrations, 3 contracts.

### Confirmed current state (from code map)

| Concept | IPS home (Research) | Risk home | Unit mismatch |
|---|---|---|---|
| Target allocation | `AllocationTargets: List<AllocationTarget(AssetClass, TargetPct, MinPct, MaxPct)>` + `RebalancingRule(AbsoluteBandPct, RelativeBandPct, …)` (`Default = 5/25`) | `AllocationTargets: List<AllocationTargetEntry(AssetClass, TargetPct, DriftBandPct)>` (`allocation_targets_json`) | IPS `TargetPct` = **whole percent** + min/max band; Risk `TargetPct` = **fraction (0,1]** + symmetric `DriftBandPct` |
| Position cap | `MaxSinglePositionPct` (`numeric(6,2)`, **no unit validation on save**) | `MaxPositionWeightPct` (`numeric(9,6)`, validated **fraction (0,1]**, enforced) | IPS cap unit is **ambiguous**; Risk cap is a validated fraction |

**Readers (the repoint targets):**
1. **Allocation drift — Research**: `GetAllocationDriftQuery` reads IPS (→ MCP `get_allocation_vs_target`). *Already reads the future single home.* No repoint; verify only.
2. **Allocation drift — Risk**: `RiskEvaluationService.ComputeRawViolations` (lines 218–240) reads `RiskRuleSet.AllocationTargets`, emits `RiskRuleKeys.AllocationDrift` in the compliance report (via `RiskCheckJob`). **Must repoint to IPS.**
3. **Position-cap enforcement — Risk**: `RiskEvaluationService` (compliance lines 158–175; pre-trade proposal 74–88) reads `RiskRuleSet.MaxPositionWeightPct`. *Already reads the future single home.* No repoint; verify only.
4. **Opportunity scoring — Research**: `ScoreCandidateCommand.BuildIpsFit` (lines 165–167) reads `ips.MaxSinglePositionPct`. **Must repoint to Risk cap** (through a read port).

**Contracts:**
- MCP `save_ips`/`get_ips` (`FinanceSentry.Mcp`, `SaveIpsCommand`/`GetIpsQuery`, `IpsDto`) — drop `MaxSinglePositionPct`. **MCP-only, no REST.**
- MCP `save_risk_rules`/`get_risk_rules` + REST `PUT`/`GET /risk/rules` (`SaveRiskRuleSetCommand`/`GetRiskRuleSetQuery`, `RiskRuleSetDto`, `SaveRiskRulesRequest`) — drop `AllocationTargets`. **No frontend consumer** (grep-verified) — no SPA breakage; backend API version bump still required by constitution.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. Modular Monolith / domain interfaces | **PASS** | Cross-module reads go through **read-only ports** (`IPositionCapSource` in Research.Domain, `IAllocationPolicySource` in Risk.Domain). The two modules do **not** reference each other (verified) — so the adapters live in the **composition root `FinanceSentry.API`** (which already references both) to avoid a cyclic assembly reference; they delegate to the other module's existing `IQueryHandler`. No module references the other's DbContext or concrete internals directly. Sanctioned inter-module contract pattern. |
| II. Code Quality (zero warnings) | **PASS** | Standard gate — `dotnet build backend/` clean after each `.cs` change; unused columns/properties fully removed, not left dangling. |
| III. Multi-Source Integration | **N/A** | No external integration touched. |
| IV. AI-Driven Analytics | **N/A** | No analytics/LLM change; scoring logic preserved, only its cap source repointed. |
| V. Security-First | **PASS** | No auth/token/encryption surface touched; reconciliation writes are user-scoped (per-`UserId` records). |
| VI. Frontend State & Composition | **N/A** | No frontend changes (FR-015; grep-confirmed no SPA consumer). |
| Testing Discipline | **PASS (with obligations)** | Contract test required for the changed `PUT`/`GET /risk/rules` (schema now excludes `AllocationTargets`). Unit tests for reconciliation rule branches. Characterization tests for zero-drift (SC-002). |
| Versioning & Tagging | **PASS (with obligations)** | Backend API contract changes (`RiskRuleSetDto`/`SaveRiskRulesRequest` field removal) → **backend version bump + tag** in the same PR. Field removal is breaking-shaped; classify per Versioning Policy (no live client mitigates impact). MCP contract change flagged for agent-config owner (FR-014). |

**No violations requiring Complexity Tracking.** The two cross-module read ports are the standard modular-monolith contract mechanism, not added complexity.

## Project Structure

### Documentation (this feature)

```text
specs/039-ips-risk-boundary/
├── plan.md              # This file
├── research.md          # Phase 0 — reconciliation rule, unit normalization, repoint strategy, zero-drift proof method
├── data-model.md        # Phase 1 — entity deltas, port contracts, migration reconciliation logic
├── quickstart.md        # Phase 1 — how to verify the cleanup end-to-end
├── contracts/           # Phase 1 — MCP tool, REST, and cross-module port contract deltas
│   ├── mcp-tools.md
│   ├── rest-endpoints.md
│   └── cross-module-ports.md
└── tasks.md             # Phase 2 (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/src/
├── FinanceSentry.Modules.Research/
│   ├── Domain/
│   │   ├── InvestmentPolicyStatement.cs          # DROP MaxSinglePositionPct (entity + IpsDto mapping)
│   │   └── Ports/IPositionCapSource.cs           # NEW read-only port: current position cap (fraction)
│   ├── Application/
│   │   ├── Commands/SaveIpsCommand.cs            # DROP MaxSinglePositionPct param + mapping
│   │   ├── Commands/ScoreCandidateCommand.cs     # REPOINT BuildIpsFit → IPositionCapSource
│   │   └── Queries/GetIpsQuery.cs                # DROP MaxSinglePositionPct from projection
│   ├── API/Responses/IpsDto.cs                   # DROP MaxSinglePositionPct
│   ├── Infrastructure/
│   │   ├── Persistence/ResearchDbContext.cs      # DROP column mapping
│   │   └── Migrations/…M012_ReconcileAndDropIpsPositionCap.cs # NEW: reconcile cap→retained Risk col, then drop IPS cap (order-independent)
│   └── (module self-registers IPositionCapSource consumer via IModuleRegistrar)
│
├── FinanceSentry.Modules.Risk/
│   ├── Domain/
│   │   ├── RiskRuleSet.cs                         # DROP AllocationTargets (+ AllocationTargetEntry usage)
│   │   └── Ports/IAllocationPolicySource.cs       # NEW read-only port (+ AllocationDriftTarget): IPS allocation, translated
│   ├── Application/
│   │   ├── Commands/SaveRiskRuleSetCommand.cs     # DROP AllocationTargets param + validation + mapping
│   │   ├── Queries/GetRiskRuleSetQuery.cs         # DROP AllocationTargets from RiskRuleSetDto
│   │   └── Services/RiskEvaluationService.cs      # REPOINT drift check → IAllocationPolicySource
│   ├── API/Controllers/RiskController.cs          # DROP AllocationTargets from SaveRiskRulesRequest
│   ├── Infrastructure/
│   │   ├── Persistence/RiskDbContext.cs           # DROP allocation_targets_json mapping
│   │   └── Migrations/…M002_ReconcileAndDropAllocation.cs # NEW: reconcile allocation→retained IPS col, then drop allocation column (order-independent)
│
├── FinanceSentry.API/                             # composition root — references BOTH modules (adapters live here to avoid a cyclic assembly ref)
│   ├── Integration/RiskPositionCapSource.cs       # NEW: implements Research IPositionCapSource via Risk GetRiskRuleSetQuery
│   ├── Integration/IpsAllocationPolicySource.cs   # NEW: implements Risk IAllocationPolicySource via Research GetIpsQuery (R4 translation)
│   └── Integration/CrossModulePortRegistration.cs # NEW: AddCrossModulePorts() DI extension, called from Program.cs
│
└── FinanceSentry.Mcp/Tools/
    ├── SaveIpsTool.cs                             # DROP maxSinglePositionPct param
    └── SaveRiskRulesTool.cs                       # DROP allocationTargets param

backend/tests/
├── …Research.Tests/  # reconciliation unit tests, ScoreCandidate characterization
├── …Risk.Tests/      # reconciliation unit tests, RiskEvaluationService drift+cap characterization, /risk/rules contract test
```

**Structure Decision**: Existing modular-monolith layout; two existing modules edited, no new module (FR-015). The only structurally new artifacts are two `Ports/` interfaces + two `Infrastructure/Adapters/` implementations — the sanctioned way (Principle I) for one module to read another's data without coupling to its persistence. The reconciliation runs inside the two EF migrations because both schemas share one physical Postgres DB (cross-schema SQL is available in a single migration transaction).

## Key Design Decisions (detail in research.md)

1. **Two read ports, symmetric — adapters in the host.** Risk's drift check reads IPS allocation via `IAllocationPolicySource` (port in Risk.Domain). Research's scoring reads the Risk cap via `IPositionCapSource` (port in Research.Domain). The two modules don't reference each other (verified), so both **adapters live in `FinanceSentry.API/Integration/`** (the composition root references both modules) and delegate to the other module's existing `IQueryHandler` (`GetIpsQuery` / `GetRiskRuleSetQuery`) — no new query logic, no cyclic assembly reference. Registered via `AddCrossModulePorts()` in Program.cs.

2. **Allocation shape/unit translation at the Risk drift boundary.** IPS stores whole-percent `TargetPct` + `MinPct/MaxPct` + `RebalancingRule`; Risk's drift comparator expects fraction `TargetPct` + symmetric `DriftBandPct` against fractional book weights. The migration encodes the old Risk per-sleeve `DriftBandPct` reversibly into IPS `MinPct/MaxPct` (`Min = (target−band)`, `Max = (target+band)`, in IPS units) so the repointed Risk reader can recover the exact band and reproduce identical `AllocationDrift` verdicts. Exact translation rule + rounding is pinned in research.md.

3. **Position-cap unit normalization.** IPS cap is unit-ambiguous (no save validation); Risk cap is a validated fraction. Reconciliation normalizes any IPS value to Risk's fraction unit (value `> 1` ⇒ treat as whole percent ⇒ ÷100) before applying the stricter-wins rule, and logs the normalization + any discarded value. Repointing scoring to the fractional cap makes its `currentWeight (fraction) ≤ cap` comparison correct and consistent with enforcement.

4. **Zero-drift scope is honest.** SC-002 guarantees identical results **where the two copies agree**. Where they disagree today (e.g., IPS cap null-and-permissive but Risk cap set), consolidation to the single source *is* the intended correction and can change a score/verdict — this is documented as expected, not a regression, and validated against the real user's live data to record exactly what (if anything) changes.

5. **Migration is idempotent, lossless, and order-independent.** Each migration reconciles the concept whose column it drops — Risk M002 moves allocation into the retained IPS column then drops the Risk allocation column; Research M012 moves the cap into the retained Risk column then drops the IPS cap column. Neither reads a column the other drops, so the two EF contexts apply in any order with no data-loss risk. Guarded so a second run is a no-op; one-side-empty keeps the populated side; both-empty fabricates nothing; discarded values logged (FR-011).

## Complexity Tracking

> No Constitution Check violations. Table intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
