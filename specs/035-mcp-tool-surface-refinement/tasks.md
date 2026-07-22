---
description: "Task list for MCP Tool Surface Refinement — shape over count"
---

# Tasks: MCP Tool Surface Refinement — Shape Over Count

**Input**: Design docs from `/specs/035-mcp-tool-surface-refinement/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/mcp-tools.md

**Tests**: The DI-resolution smoke test (US2) is the safety artifact — it converts the #297 missing-handler bug class into a red test. The tool-name contract test is updated with the surface change. Both mandatory. Backend/MCP only; no new external source.

**Organization**: US1 descriptions (P1) · US2 nothing-broken (P1) · US3 merge + enrichment (P2). MVP = US1 + US2 (descriptions + a proven-working surface). All in `backend/src/FinanceSentry.Mcp` + `backend/tests/FinanceSentry.Mcp.Tests`.

## Phase 1: Setup
- [ ] T001 Confirm branch `035-mcp-tool-surface-refinement`; baseline `dotnet build FinanceSentry.sln` + `dotnet test tests/FinanceSentry.Mcp.Tests` green (containerized SDK).

## Phase 2: Foundational (blocks US2)
- [ ] T002 Extract the inline `RegisterShared` local function in `backend/src/FinanceSentry.Mcp/Program.cs` into a reusable `internal static class McpServiceRegistration` (`RegisterShared(IServiceCollection, IConfiguration, Assembly[] moduleAssemblies, Assembly mcpAssembly)`); `Program.cs` calls it for both transports. **Pure refactor, zero behavior change** — build must stay zero-warning and existing MCP tests green.

**Checkpoint**: registration reusable from tests; nothing else changed.

## Phase 3: US1 — descriptions carry call-order (P1) 🎯 MVP
- [ ] T003 [US1] Audit all ~57 tool `[Description]`s in `backend/src/FinanceSentry.Mcp/Tools/`; produce the entry→follow-up workflow map (start from research.md D1). Record which Ledger-persona prose each edit makes redundant (feeds the deploy-side follow-up, T020).
- [ ] T004 [P] [US1] `GetRadarSummaryTool.cs` — description states it is the FIRST call for any market/narration question.
- [ ] T005 [P] [US1] `GetMarketStructureTool.cs`, `GetMarketBreadthTool.cs`, `GetRelativeStrengthTool.cs`, `GetSectorRotationTool.cs` — each description says "drill-down; call `get_radar_summary` first for the overview."
- [ ] T006 [P] [US1] `PromoteCandidateTool.cs` — description states it runs `check_risk_rules` as a hard gate before proposing (019).
- [ ] T007 [US1] Sweep the remaining tools for implied ordering (candidate flow, analytics pair already done in 033) and add a one-line sequencing sentence where warranted. `run_thesis_monitor`'s ordering sentence is finalized in T015 (enrichment) to avoid churn.

**Checkpoint**: every entry/follow-up tool states its role in its own description (SC-001).

## Phase 4: US2 — every advertised tool works (P1)
- [ ] T008 [US2] Add `backend/tests/FinanceSentry.Mcp.Tests/ContractTests/ToolResolutionTests.cs` — build a `ServiceCollection`, call `McpServiceRegistration.RegisterShared(...)` with the real module + mcp assemblies, then for every `[McpServerToolType]` in the MCP assembly resolve/construct it via the provider and assert success.
- [ ] T009 [US2] Run the resolution test; if any tool fails to construct (missing handler registration — the #297 class), fix the registration and re-run until green. Confirms Companion tools now resolve.
- [ ] T010 [US2] Record in `quickstart.md` the deployed-server check: invoke `get_pending_companion_events` with a valid identity → normal result, not a 500 (verify after #297 deploy lands on the VPS).

**Checkpoint**: 100% of tools construct from the real graph (SC-003); companion endpoint proven (SC-004).

## Phase 5: US3 — watchlist merge + thesis enrichment (P2)
- [ ] T011 [US3] Create `WatchlistToolResult` DTO (+ `ForList`/`ForAdd`/`ForRemove`/`Invalid` factories; null members omitted) per data-model.md, beside the tool (or `API/Responses/` if that is the Mcp convention).
- [ ] T012 [US3] Create `backend/src/FinanceSentry.Mcp/Tools/WatchlistTool.cs` — `[McpServerTool(Name="watchlist")]`, `action` ∈ {list,add,remove}, inject `GetWatchlistQuery`/`AddWatchlistItemCommand`/`RemoveWatchlistItemCommand` handlers + `IIdentityResolver`; validate required params per action; return `WatchlistToolResult`. **Delete** `ListWatchlistTool.cs`, `AddToWatchlistTool.cs`, `RemoveFromWatchlistTool.cs`.
- [ ] T013 [US3] Create `ThesisMonitorResult` DTO (`Summary` + `Breaks`) per data-model.md.
- [ ] T014 [US3] Enrich `RunThesisMonitorTool.cs` — also inject the `ListThesisBreaksQuery` handler; after the monitor command, query breaks and return `ThesisMonitorResult`; update its description (T007's deferred sentence) to point at `list_thesis_breaks` for the read-only path. Leave `ListThesisBreaksTool.cs` untouched.
- [ ] T015 [US3] Update `ToolNameContractTests.cs` agreed surface: remove `list_watchlist`/`add_to_watchlist`/`remove_from_watchlist`, add `watchlist` (→ 55); update the "because" message to 55.

**Checkpoint**: surface is 55; all prior watchlist ops reachable via `watchlist`; `run_thesis_monitor` returns `{summary,breaks}`; `list_thesis_breaks` still a pure read; contract + resolution tests green (SC-005/SC-006).

## Phase 6: Polish
- [ ] T016 `/csharp-quality` sweep; `dotnet build FinanceSentry.sln` zero warnings.
- [ ] T017 Run `dotnet test tests/FinanceSentry.Mcp.Tests` — contract (55) + resolution tests green; walk the `quickstart.md` watchlist round-trip + thesis enrichment scenarios.
- [ ] T018 Record the deploy-side follow-up (out of repo): trim the now-redundant call-order prose from Ledger's persona on the VPS once descriptions are confirmed self-sufficient.

## Dependencies
- Setup → Foundational (T002) blocks US2 (T008).
- US1 is independent (pure string edits) — can run alongside anything.
- US3: T012 removes tools → T015 (contract update) must land in the same change; T014 finalizes the run_thesis_monitor sentence deferred from T007.
- **MVP = US1 + US2.** Descriptions + a provably-working surface deliver most of the value; the merges (US3) are a bounded P2.

## Notes
- No new NuGet, no DB/migration, no frontend. Merged/enriched tools reuse existing Research handlers.
- The `action` param on `watchlist` is the one accepted shape deviation (plan Complexity Tracking) — one resource, closed 3-action set.
- Read/write boundaries (IPS, risk rules, companion cluster) and the Radar six are deliberately NOT merged (FR-007/FR-008).
