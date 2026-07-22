# Quickstart / Verification: MCP Tool Surface Refinement

Backend/MCP only. Zero-warning build throughout.

## US2 — nothing broken

1. **DI-resolution smoke test** (CI gate): `dotnet test tests/FinanceSentry.Mcp.Tests` → `ToolResolutionTests` constructs every `[McpServerToolType]` from the real service graph; all resolve. Deleting a handler registration (e.g. reverting the #297 Companion fix) MUST turn this red.
2. **Companion fix confirmed** (deployed server): invoke `get_pending_companion_events` with a valid identity → returns a normal result (empty list is a pass), not a 500. This proves 031/#297 on the live MCP server.

## US3 — merge + enrichment

3. `ToolNameContractTests` passes with the 55-name surface (no `list_/add_/remove_watchlist`; `watchlist` present).
4. `watchlist` round-trip:
   - `watchlist {"action":"add","ticker":"NVDA"}` → `{action:add, item:{…}}`
   - `watchlist {"action":"list"}` → includes NVDA
   - `watchlist {"action":"remove","itemId":"<id>"}` → `{action:remove, removed:true}`
   - `watchlist {"action":"add"}` (no ticker) → `{error:"…requires a ticker"}`
5. `run_thesis_monitor` → returns `{summary, breaks}`; `list_thesis_breaks` still returns breaks alone and fires no alerts.

## US1 — descriptions carry order

6. Read `get_radar_summary` and the four market drill-down descriptions → entry/drill-down relationship is stated in the tools themselves.
7. Read `promote_candidate` → states the `check_risk_rules` gate.
8. Read `run_thesis_monitor` → points to `list_thesis_breaks` for the read-only path.

## Deploy-side follow-up (out of repo)

9. Once descriptions are self-sufficient, trim the duplicated call-order prose from Ledger's persona on the VPS (agents/finance). This is a persona/deploy change, not a repo task — do it after this ships and the descriptions are confirmed.
