# Contract: REST Endpoints

Only the Risk module exposes REST for these concepts. **IPS has no REST surface** (MCP-only) — no REST change on the IPS side.

## `PUT /risk/rules` (`RiskController.SaveRules` → `SaveRiskRuleSetCommand`)

**Request DTO `SaveRiskRulesRequest` (L110–117):**

| Field | Before | After |
|---|---|---|
| `AllocationTargets: IReadOnlyList<AllocationTargetEntry>?` | present (L117) | **REMOVED** |
| `MaxPositionWeightPct: decimal?` | present (L111) | **KEPT** |
| `MaxNewPositionPct`, `MaxSleeveWeightPct`, `MinCashBufferPct`, `MaxLossPerThesisPct`, `TurnoverBudgetPerQuarter` | present | **KEPT** |

Posting `AllocationTargets` after the change: field absent from the DTO → ignored by the model binder (SC-005: no silent acceptance into the wrong home).

## `GET /risk/rules` (`RiskController.GetRules` → `RiskRuleSetDto`)

**Response**: `AllocationTargets` removed; caps retained.

## Versioning & Tagging (constitution)

- Backend REST request/response schema changed → **`FinanceSentry.API.csproj` `<Version>` bump + git tag** in the same PR.
- Field removal is breaking-shaped. **No frontend/SPA consumer exists** (grep-verified across `frontend/src`) — record the absence of live clients in the release notes; classify the bump per the Versioning Policy accordingly.

## Contract test obligation

New/updated contract test for `PUT`/`GET /risk/rules`:
1. `GET` response schema does **not** contain `allocationTargets`.
2. `PUT` with a body that includes `allocationTargets` succeeds and the value is **not** persisted anywhere (ignored, not routed to allocation).
3. `PUT`/`GET` round-trip of the retained caps is unchanged (status codes + schema).
