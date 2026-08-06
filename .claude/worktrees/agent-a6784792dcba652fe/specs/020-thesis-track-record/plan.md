# Implementation Plan: Thesis Track Record

**Branch**: `020-thesis-track-record` | **Date**: 2026-07-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/020-thesis-track-record/spec.md`

## Summary

Add an append-only `ThesisEvent` log and a weekly Hangfire snapshot job to the existing
`FinanceSentry.Modules.Research` module (v0, ships right after 017 per the 2026-07-07
resequencing). Every thesis lifecycle write (`SaveThesisCommand` create/update, and the
break/unbreak path 017 adds to `InvestmentThesis`) is decorated so an event is appended
with subject + benchmark (SPY) prices sourced from the existing `IMarketDataService`,
never blocking the originating write on a quote failure. A new `ThesisPerformanceCalculator`
computes absolute/benchmark/excess return between any two events (or event→latest) purely
from persisted prices, net of a configurable cost/tax model. Three new MCP tools —
`get_track_record`, `get_thesis_performance`, `list_thesis_events` — expose the read side;
a fourth, `get_postmortem_packet`, compiles the decision-journal-annotated terminal-event
history for Denys's periodic process review. Candidate (019) hook points are wired now but
inert until 019 ships — no candidate rows exist yet, so `SubjectType=Candidate` paths are
implemented but untestable end-to-end until then.

## Technical Context

**Language/Version**: C# 13 / .NET 9
**Primary Dependencies**: ASP.NET Core 9, EF Core 9, Hangfire, `FinanceSentry.Core.Cqrs` (hand-rolled `ICommand(Handler)`/`IQuery(Handler)` — **no MediatR**, none exists anywhere in this codebase), existing `IMarketDataService` (Yahoo-backed, in `FinanceSentry.Modules.Research`)
**Storage**: PostgreSQL 14 — new `thesis_events` table added to the existing `ResearchDbContext` (same DB context as `InvestmentThesis`, `Watchlist`, `Ips`; new migration `M004_ThesisEvents`)
**Testing**: xUnit (backend only — this feature has no frontend surface per spec `[OUT OF SCOPE]`); MCP tool contract tests in `FinanceSentry.Mcp.Tests/ContractTests`
**Target Platform**: Linux (Docker) server — backend/MCP only, no SPA changes
**Project Type**: Backend module extension (existing `FinanceSentry.Modules.Research`) + MCP tools — **no frontend work**
**Performance Goals**: Event capture adds <50ms to thesis/candidate write paths (SC-005); `get_track_record` and `get_thesis_performance` are simple aggregate queries over an indexed table, expected <200ms
**Constraints**: Append-only (FR-009, no update/delete on `ThesisEvent`); quote failures never block the originating write (FR-003); v0 prices come from `IMarketDataService` live quotes, not persisted 018 daily bars (018 doesn't exist yet — upgrade path noted, not built now)
**Scale/Scope**: One user (Denys) in practice; single-digit theses/candidates growing slowly — no scale concerns

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Modular Monolith | ✅ | `ThesisEvent` capture lives inside `FinanceSentry.Modules.Research` (same module that owns `InvestmentThesis`) — no new module, no cross-module coupling. `IMarketDataService` is already the domain-defined interface for price lookups; capture code depends on it, never on `YahooMarketDataService` directly. 019 candidate hooks reference only `Domain` types inside Research, not a concrete 019 implementation (019 doesn't exist yet). |
| II. Code Quality | GATE | Zero `dotnet build` warnings required per file per constitution — enforced during /speckit.implement, not at plan time. |
| III. Multi-Source Integration | N/A | No external bank/broker/crypto integration in this feature. |
| IV. AI Analytics | N/A | Deterministic tier 1 only, per spec (`interpretation... stays with Ledger`); no AI/LLM call in this feature. |
| V. Security | ✅ | All new queries/commands scoped by `UserId` (via `SubjectId` → owning thesis/candidate's `UserId`, resolved server-side, never a client-supplied filter). MCP tools resolve `userId` from `IIdentityResolver` exactly like `SaveThesisTool`. |
| VI. Frontend State & Composition | N/A | No frontend changes — spec explicitly scopes UI beyond MCP/REST as out of v1. |
| Testing Discipline | GATE | Contract tests required for `get_track_record`/`get_thesis_performance`/`list_thesis_events`/`get_postmortem_packet` tool shapes (`FinanceSentry.Mcp.Tests/ContractTests/ToolNameContractTests.cs` pattern extends automatically since it reflects over `[McpServerToolType]`); unit tests required for `ThesisPerformanceCalculator` (pure math, SC-001) and pending-price backfill logic. |
| Versioning & Tagging | ✅ | Backend-only change → bump `FinanceSentry.API.csproj` `0.9.0` → `0.10.0`; tag `backend-v0.10.0` after merge. No frontend version bump (no frontend files touched). |

**Post-design re-check**: No violations. `ThesisEvent` capture is additive (event-append decorator around existing `SaveThesisCommand` handler and the 017 break/unbreak write path) — it does not modify `InvestmentThesis` schema (per spec Key Entities: "No changes to `InvestmentThesis`, 017, or 019 schemas"). `ThesisEventRepository` depends only on `ResearchDbContext` (already in-module). No circular references introduced.

## Project Structure

### Documentation (this feature)

```text
specs/020-thesis-track-record/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── tasks.md
```

(No `contracts/` REST directory — this feature exposes MCP tools only, no new REST controller. Tool request/response shapes are documented inline in `data-model.md` instead of a separate contracts file, matching how 017's spec treats MCP-only surfaces.)

### Source Code

```text
backend/src/
├── FinanceSentry.Modules.Research/
│   ├── Domain/
│   │   ├── ThesisEvent.cs                              [NEW: SubjectType, SubjectId, EventType, Timestamp, SubjectPrice?, BenchmarkPrice?, PricesPending, DecisionNote?]
│   │   ├── ThesisSubjectType.cs                         [NEW: enum Thesis | Candidate]
│   │   ├── ThesisEventType.cs                           [NEW: enum Created|Broken|Unbroken|Closed|Promoted|Rejected|Expired|Snapshot]
│   │   └── Repositories/
│   │       └── IThesisEventRepository.cs                [NEW: AppendAsync, ListAsync(subjectId?), ListPendingAsync, ListForPeriodAsync]
│   │
│   ├── Application/
│   │   ├── Services/
│   │   │   ├── IThesisEventRecorder.cs                  [NEW: RecordAsync(subjectType, subjectId, eventType, ticker, decisionNote?) — the hook point 017/019 call]
│   │   │   ├── ThesisEventRecorder.cs                   [NEW: implements above; calls IMarketDataService for prices, catches quote failures → PricesPending=true]
│   │   │   ├── IThesisPerformanceCalculator.cs           [NEW: pure math contract]
│   │   │   └── ThesisPerformanceCalculator.cs            [NEW: absolute/benchmark/excess return, net-of-cost/tax, hit-rate classification — SC-001, no I/O]
│   │   ├── Commands/
│   │   │   └── SaveThesisCommand.cs                     [MODIFY: handler calls IThesisEventRecorder.RecordAsync(Created) after persist, only on first create — no behavior change to existing return shape]
│   │   └── Queries/
│   │       ├── GetTrackRecordQuery.cs                    [NEW]
│   │       ├── GetThesisPerformanceQuery.cs              [NEW]
│   │       ├── ListThesisEventsQuery.cs                  [NEW]
│   │       └── GetPostmortemPacketQuery.cs               [NEW]
│   │
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── ResearchDbContext.cs                      [MODIFY: add DbSet<ThesisEvent>, entity config, indexes]
│   │   │   └── Repositories/
│   │   │       └── ThesisEventRepository.cs              [NEW]
│   │   └── Jobs/
│   │       └── ThesisTrackRecordSnapshotJob.cs           [NEW: weekly — snapshot every active thesis/candidate, backfill pending prices]
│   │
│   ├── Migrations/
│   │   └── <ts>_M004_ThesisEvents.cs                     [NEW]
│   │
│   └── ResearchModule.cs                                 [MODIFY: register IThesisEventRecorder, IThesisPerformanceCalculator, IThesisEventRepository, ThesisTrackRecordSnapshotJob recurring job]
│
├── FinanceSentry.Mcp/
│   └── Tools/
│       ├── GetTrackRecordTool.cs                         [NEW]
│       ├── GetThesisPerformanceTool.cs                   [NEW]
│       ├── ListThesisEventsTool.cs                       [NEW]
│       └── GetPostmortemPacketTool.cs                    [NEW]
│
└── FinanceSentry.API/
    └── FinanceSentry.API.csproj                          [MODIFY: bump 0.9.0 → 0.10.0]

backend/tests/
├── FinanceSentry.Modules.Research.Tests/                 [existing or new test project — verify at implement time]
│   ├── Unit/
│   │   ├── ThesisPerformanceCalculatorTests.cs           [NEW: SC-001 — splits/pending/not-evaluable determinism]
│   │   └── ThesisEventRecorderTests.cs                   [NEW: quote-failure → pending, never throws]
│   └── Jobs/
│       └── ThesisTrackRecordSnapshotJobTests.cs          [NEW: backfill + weekly snapshot logic]
└── FinanceSentry.Mcp.Tests/
    ├── ContractTests/                                    [existing — ToolNameContractTests auto-covers new tools via reflection]
    └── GetTrackRecordToolTests.cs                        [NEW]
```

**Note on 017/019 coupling**: 017 (break/unbreak) and 019 (candidate promote/reject/expire) have specs but no plan/tasks/implementation yet as of this writing. This plan wires `IThesisEventRecorder.RecordAsync` as the call site those features will invoke; the `Broken`/`Unbroken`/`Promoted`/`Rejected`/`Expired` event types and `SubjectType.Candidate` are modeled now (per spec FR-001/FR-002 — "hook the existing write paths... via domain events or repository decorators") but only the `Created` path (via `SaveThesisCommand`) is exercisable end-to-end until 017/019 ship. This is called out explicitly rather than silently deferred.

## Complexity Tracking

No constitution violations. No complexity tracking required.

---

## Implementation Phases (for /speckit.tasks)

### Phase 1 — Foundational (event log + persistence)

- `ThesisSubjectType`, `ThesisEventType` enums
- `ThesisEvent` domain entity
- `IThesisEventRepository` + `ThesisEventRepository`
- `ResearchDbContext` DbSet + config; migration `M004_ThesisEvents`
- Register in `ResearchModule.cs`

### Phase 2 — User Story 1 (price-stamped lifecycle events)

- `IThesisEventRecorder` / `ThesisEventRecorder` (quote lookup via `IMarketDataService`, pending-on-failure)
- Wire `SaveThesisCommand` handler to call `RecordAsync(Created)` on first create
- Unit tests: recorder never throws on quote failure; `Created` event recorded once per thesis (idempotency)
- `ThesisTrackRecordSnapshotJob` (weekly Hangfire job — backfill pending prices; append `Snapshot` events for active theses/candidates)

### Phase 3 — User Story 2 (performance vs benchmark)

- `IThesisPerformanceCalculator` / `ThesisPerformanceCalculator` (pure function: two `ThesisEvent`s or latest bar → absolute/benchmark/excess return; net-of-cost/tax per FR-007b; not-evaluable path)
- `GetThesisPerformanceQuery` + handler
- `GetThesisPerformanceTool` (MCP)
- Unit tests: SC-001 determinism, net-of-friction math, not-evaluable ticker

### Phase 4 — User Story 3 (aggregate track record)

- `GetTrackRecordQuery` + handler (counts, hit rate, avg/median excess return, split by source/status, low-sample caveat at <30 closed records per FR-007)
- `GetTrackRecordTool` (MCP)
- `ListThesisEventsQuery` + `ListThesisEventsTool` (MCP)
- Contract tests for all three tools (naming/shape via existing `ToolNameContractTests` reflection pattern)

### Phase 5 — Decision journal & post-mortem packet (FR-008b/c)

- Add optional `DecisionNote` to `IThesisEventRecorder.RecordAsync` and `ThesisEvent`
- `GetPostmortemPacketQuery` + handler (terminal events + notes + counterfactuals for a period)
- `GetPostmortemPacketTool` (MCP)

### Phase 6 — Polish

- Version bump (backend 0.9.0 → 0.10.0)
- Update `.specify` Active Technologies / Recent Changes if tracked in CLAUDE.md-adjacent files
- QA: manual quickstart.md walkthrough (create thesis → Created event with prices; simulate quote outage → pending → job backfills; query all four tools)
