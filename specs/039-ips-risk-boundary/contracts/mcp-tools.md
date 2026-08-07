# Contract: MCP Tools (agent-facing)

Project `FinanceSentry.Mcp`. These are the agent's read/write surface for policy. **No behaviour change beyond field removal.**

## `save_ips` (`SaveIpsTool` → `SaveIpsCommand`)

**Removed input parameter:**

| Param | Before | After |
|---|---|---|
| `maxSinglePositionPct: decimal?` | present (L31) | **REMOVED** |
| `allocationTargets: IReadOnlyList<AllocationTarget>` | present (L22) | **KEPT** (allocation stays in IPS) |

The position cap is no longer settable via `save_ips`. Attempting to pass it: parameter absent from the tool signature (compile-time gone).

## `get_ips` (`GetIpsTool` → `IpsDto`)

**Removed response field:** `MaxSinglePositionPct` (was `IpsDto` L21). `AllocationTargets` + `RebalancingRule` remain.

## `save_risk_rules` (`SaveRiskRulesTool` → `SaveRiskRuleSetCommand`)

**Removed input parameter:**

| Param | Before | After |
|---|---|---|
| `allocationTargets: IReadOnlyList<AllocationTargetEntry>?` | present (L25) | **REMOVED** |
| `maxPositionWeightPct: decimal?` | present (L19) | **KEPT** (cap's new sole home) |
| `maxNewPositionPct: decimal?` | present (L23) | **KEPT** (distinct concept) |

The target allocation is no longer settable via `save_risk_rules`.

## `get_risk_rules` (`GetRiskRulesTool` → `RiskRuleSetDto`)

**Removed response field:** `AllocationTargets` (was `RiskRuleSetDto` L16). `MaxPositionWeightPct` + other caps remain.

## FR-014 — Agent-config flag

The following moved fields MUST be called out in the change record for the Ledger persona/agent-config owner (update performed separately on OpenClaw, out of scope here):

| Concept | Old MCP home | New MCP home |
|---|---|---|
| Single-position cap | `save_ips.maxSinglePositionPct` | `save_risk_rules.maxPositionWeightPct` |
| Target allocation | `save_risk_rules.allocationTargets` | `save_ips.allocationTargets` |

**Contract test obligation (SC-005)**: assert each moved field appears under exactly one tool; the removed param is absent from the other tool's signature.
