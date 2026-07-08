# Implementation Plan: Risk Rules

**Branch**: `022-risk-rules` | **Date**: 2026-07-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/022-risk-rules/spec.md`

## Summary

Add a new backend-only module, `FinanceSentry.Modules.Risk`, that stores a versioned per-user `RiskRuleSet` (max position weight, max sleeve weight, min cash buffer, max loss per thesis, max new-position size, turnover budget, allocation targets) and runs a daily Hangfire check of the live book (read via the existing `IBrokerageHoldingsReader` / `ICryptoHoldingsReader` / `IBankingAccountsReader` domain interfaces in `FinanceSentry.Core`) against that rule set. Violations are deterministic pure-function facts — no LLM, no composite score — written to a new `AlertType.PolicyViolation` via the existing `IAlertGeneratorService` contract, with an acknowledged-violation flow so the pre-existing ~46% DRAM concentration becomes a tracked remediation, not daily noise. Three MCP tools (`check_risk_rules`, `get_risk_rules`, `save_risk_rules`) expose the rule set and a promotion-time `Allowed | Refused` verdict that 019's `promote_candidate` (and Ledger) will call before any position is proposed. The module keeps its own `HoldingSnapshot` history (point-in-time quantity captures on each check run) because no existing module persists holdings history — turnover-budget counting and the add-to-broken-thesis flag (via 017's `IThesisRepository`, read-only) are computed from that history. This is the practitioner layer the 2026-07-07 review found missing, and 019's promote flow depends on it as a hard gate.

## Technical Context

**Language/Version**: C# 13 / .NET 9 (backend only — no frontend changes; MCP surface only, no Angular UI for v1)
**Primary Dependencies**: ASP.NET Core 9, EF Core 9, Hangfire, `FinanceSentry.Core.Cqrs` (hand-rolled `IQuery`/`IQueryHandler`, `ICommand`/`ICommandHandler` — **not** MediatR, per current constitution/CQRS convention), `ModelContextProtocol.Server` (MCP tools, existing `FinanceSentry.Mcp` project)
**Storage**: PostgreSQL 14 — new module, own `RiskDbContext`, migration `M001_InitialSchema` adding `risk_rule_sets`, `policy_violation_acks`, `holding_snapshots` tables. No changes to existing module schemas.
**Testing**: xUnit — unit tests for the pure evaluation functions (`RiskEvaluationService`, `TurnoverTracker`) covering the seeded 46%-DRAM/25%-cap scenario, the acknowledged-violation path, and the stale-book path; contract tests for the 3 new MCP tools in `FinanceSentry.Mcp.Tests` (agreed tool surface goes 27 → 30) and REST contract tests for the new `RiskController` endpoints.
**Target Platform**: Linux server (Docker), existing Hangfire dashboard for the scheduled job
**Project Type**: Backend module extension (modular monolith) — no frontend work in this feature
**Performance Goals**: Daily check completes in well under a minute for a single-user book of ~20 positions; `check_risk_rules(proposal)` MCP call returns in <200ms (in-memory arithmetic over an already-fetched book, no external calls)
**Constraints**: Deterministic only — **no LLM call anywhere in the evaluation path**, **no composite/blended risk score** (facts + named rule only, per ROADMAP's rejection of false-precision scoring); MUST NOT execute or touch brokerage trades (no execution surface exists in this codebase); MUST NOT deliver to Telegram/external channels directly (Alerts + MCP only, Ledger owns delivery)
**Scale/Scope**: Single developer, single user's book (~$15k, ~5–15 positions across BrokerageSync/CryptoSync/BankSync); one `RiskRuleSet` version chain per user; violations list bounded by position count

## Constitution Check

*GATE: Must pass before implementation. Re-checked after Phase 1 design below.*

| Principle | Status | Notes |
|---|---|---|
| I — Modular Monolith + domain interfaces | ✅ PASS | New `FinanceSentry.Modules.Risk` is self-contained (own `RiskDbContext`, own migrations). Reads other modules' data exclusively through existing `FinanceSentry.Core.Interfaces` readers (`IBrokerageHoldingsReader`, `ICryptoHoldingsReader`, `IBankingAccountsReader`) and `IThesisRepository` (Research module — already read cross-module by `GetAllocationDriftQueryHandler`, so this is an established precedent, not a new coupling pattern). Writes go through `IAlertGeneratorService` (Core interface, implemented in Alerts) — no direct reference to `FinanceSentry.Modules.Alerts` internals. |
| II — Code Quality | ✅ PASS | Zero-`dotnet build`-warning gate enforced per file; no frontend files touched this feature |
| III — Multi-Source Integration | ✅ PASS | Evaluates the book across all three synced sources (bank/brokerage/crypto) uniformly, degrading gracefully (try/catch per source, matching `GetAllocationDriftQueryHandler`'s established pattern) when one sync is unavailable — surfaced as a staleness flag, never a silent auto-clear |
| IV — AI Analytics | ⚠️ Documented deviation | This feature is deliberately **non-AI** by spec (FR-008): deterministic checks only, no LLM in the evaluation path, no composite score. This is Radar's tier-1 layer (017/018/022 deterministic scanners); AI reasoning lives in Ledger (tier 2, outside this repo) per `ROADMAP.md`. Flagged here rather than silently marked "N/A" because Principle IV's default is AI-backed analytics — this feature is an intentional, spec-mandated exception. |
| V — Security | ✅ PASS | All queries/tables scoped by `UserId`; MCP tools resolve `userId` via `IIdentityResolver`, same pattern as `GetAllocationVsTargetTool` |
| VI — Frontend State/Composition | N/A | No frontend changes in this feature (backend + MCP only) |
| Testing Discipline | ✅ PASS | Test-First: xUnit unit tests for evaluation logic written before handlers; REST contract tests per new endpoint; MCP contract test updated for the 3 new tools |
| Versioning | ✅ PASS | Backend API version bump required (new endpoints + MCP tools); no frontend version bump (no frontend changes) |

**Post-design re-check**: No new violations found after Phase 1 design. The one documented deviation (Principle IV, deterministic-by-design) is intentional and load-bearing per the feature spec and ROADMAP.

## Project Structure

### Documentation (this feature)

```text
specs/022-risk-rules/
├── plan.md              # This file
├── research.md          # Phase 0 decisions
├── data-model.md        # Phase 1 entity schema
├── quickstart.md        # Dev setup and manual verification
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
backend/src/
├── FinanceSentry.Core/
│   └── Interfaces/
│       └── IAlertGeneratorService.cs                          [MODIFIED] add GeneratePolicyViolationAlertAsync / ResolvePolicyViolationAlertAsync
│
├── FinanceSentry.Modules.Alerts/
│   ├── Domain/AlertType.cs                                    [MODIFIED] add PolicyViolation const
│   └── Application/Services/AlertGeneratorService.cs          [MODIFIED] implement the two new interface methods
│
├── FinanceSentry.Modules.Risk/                                [NEW MODULE]
│   ├── RiskModule.cs                                          module DI registration (mirrors WealthModule.cs / AlertsModule.cs)
│   ├── API/
│   │   ├── Controllers/
│   │   │   └── RiskController.cs                              GET/PUT /risk/rules, GET /risk/compliance, POST /risk/violations/{id}/acknowledge
│   │   └── Responses/
│   │       ├── RiskRuleSetDto.cs
│   │       ├── ComplianceReportDto.cs
│   │       └── RiskVerdictDto.cs
│   ├── Application/
│   │   ├── Commands/
│   │   │   ├── SaveRiskRuleSetCommand.cs                      validates ranges, appends new version
│   │   │   └── AcknowledgeViolationCommand.cs                  remediation note + worsening step
│   │   ├── Queries/
│   │   │   ├── GetRiskRuleSetQuery.cs
│   │   │   └── CheckRiskRulesQuery.cs                          no-arg compliance report OR (ticker, amount) proposal verdict
│   │   └── Services/
│   │       ├── IRiskEvaluationService.cs                       pure function: (BookSnapshot, RiskRuleSet, ack state) -> ComplianceReport
│   │       ├── RiskEvaluationService.cs
│   │       ├── ITurnoverTracker.cs                             pure function over HoldingSnapshot deltas -> discretionary trade count/quarter
│   │       ├── TurnoverTracker.cs
│   │       ├── IBookSnapshotReader.cs                          aggregates the three Core holdings/account readers into one BookSnapshot, tolerating per-source failure (staleness flag)
│   │       └── BookSnapshotReader.cs
│   ├── Domain/
│   │   ├── RiskRuleSet.cs                                      versioned entity (FR-001)
│   │   ├── PolicyViolationAck.cs                                (FR-003)
│   │   ├── HoldingSnapshot.cs                                   point-in-time (symbol, quantity, usdValue) capture, own history
│   │   ├── BookSnapshot.cs                                       in-memory aggregate passed to RiskEvaluationService (not persisted directly)
│   │   ├── PolicyViolation.cs                                    record: RuleKey, Subject, Observed, Limit, ExcessUsd, Status (New/Acknowledged/Worsened)
│   │   ├── RiskVerdict.cs                                        record: Allowed/Refused, RuleKey?, MaxCompliantSizeUsd?, Headroom facts
│   │   ├── Exceptions/
│   │   │   └── RiskRuleSetNotFoundException.cs
│   │   └── Repositories/
│   │       ├── IRiskRuleSetRepository.cs
│   │       ├── IPolicyViolationAckRepository.cs
│   │       └── IHoldingSnapshotRepository.cs
│   ├── Infrastructure/
│   │   ├── Jobs/
│   │   │   ├── RiskCheckJob.cs                                  daily, after-sync Hangfire job (mirrors NetWorthSnapshotJob)
│   │   │   └── RiskCheckJobScheduler.cs                         mirrors NetWorthSnapshotJobScheduler
│   │   └── Persistence/
│   │       ├── RiskDbContext.cs
│   │       ├── RiskDbContextFactory.cs
│   │       └── Repositories/
│   │           ├── RiskRuleSetRepository.cs
│   │           ├── PolicyViolationAckRepository.cs
│   │           └── HoldingSnapshotRepository.cs
│   └── Migrations/
│       └── <timestamp>_M001_InitialSchema.cs                    risk_rule_sets, policy_violation_acks, holding_snapshots
│
├── FinanceSentry.Mcp/Tools/
│   ├── CheckRiskRulesTool.cs                                    [NEW] check_risk_rules
│   ├── GetRiskRulesTool.cs                                      [NEW] get_risk_rules
│   └── SaveRiskRulesTool.cs                                     [NEW] save_risk_rules
│
└── FinanceSentry.API/
    ├── Program.cs                                                [MODIFIED] register RiskModule, RiskCheckJob recurring schedule
    └── FinanceSentry.API.csproj                                  [MODIFIED] version bump (minor — new endpoints + MCP tools)

backend/tests/
├── FinanceSentry.Tests/Risk/
│   ├── RiskEvaluationServiceTests.cs                             pure-function unit tests: 46%/25% seeded scenario, ack path, worsening step, stale-book flag
│   ├── TurnoverTrackerTests.cs                                   unit tests: trade counting from HoldingSnapshot deltas, quarter rollover
│   ├── SaveRiskRuleSetCommandTests.cs                             validation ranges, version append
│   ├── RiskRulesContractTests.cs                                 REST contract tests for RiskController endpoints
│   └── AddToBrokenThesisTests.cs                                 quantity-increase-after-break vs before-break ordering
└── FinanceSentry.Mcp.Tests/
    └── ContractTests/ToolNameContractTests.cs                     [MODIFIED] AgreedToolSurface 27 → 30 (+check_risk_rules, get_risk_rules, save_risk_rules)
```

**Dependency on 017/018/019**: 022 does not block on 017 (thesis-broken state) or 018 (signal log) shipping first — FR-006 (add-to-broken-thesis) degrades to "no theses to check" if `IThesisRepository` has no broken records yet, and per the spec's Key Entities note, signals go to the Alerts module only until 018's `radar_signals` log exists; the check logic itself is independent of the log. 022 is a hard **gate** for 019 (`promote_candidate` must call `check_risk_rules` before any promotion) — 019 cannot ship its promote flow before 022's `CheckRiskRulesQuery` proposal path exists.

## Complexity Tracking

No constitution violations requiring justification beyond the documented Principle IV deviation (deterministic-by-design, see Constitution Check above). No new NuGet packages beyond what's already referenced by sibling modules (EF Core, Hangfire, ModelContextProtocol.Server — all existing dependencies). One new top-level project (`FinanceSentry.Modules.Risk`), consistent with the established one-module-per-domain pattern.
