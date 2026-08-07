# Phase 1 Data Model: IPS ↔ Risk Rules Boundary Cleanup

Structural cleanup — no new persisted entities (FR-015). This documents the **field deltas**, the **new read-port shapes**, and the **migration reconciliation logic**.

---

## Entity deltas

### `InvestmentPolicyStatement` (Research, schema `research`, table `investment_policy_statements`)

| Field | Type | Change |
|---|---|---|
| `AllocationTargets` | `List<AllocationTarget(AssetClass, TargetPct, MinPct, MaxPct)>` (jsonb) | **KEEP** — sole home of target allocation |
| `RebalancingRule` | `RebalancingRule(AbsoluteBandPct, RelativeBandPct, Cadence, ContributionsFirst)` (jsonb, `Default = 5/25`) | **KEEP** — sole home of the drift/rebalance band |
| `MaxSinglePositionPct` | `decimal?` (`numeric(6,2)`) | **REMOVE** — moves to Risk `MaxPositionWeightPct` |
| *(all other IPS fields: Goals, horizons, risk tolerance/capacity, ContributionPlan, SellDiscipline, CoolingOffDays, Exclusions, ReviewCadence, …)* | — | **KEEP unchanged** |

Removal touches: entity property, EF mapping in `ResearchDbContext` (L161), `IpsDto` (L21), `SaveIpsCommand` param (L22) + handler mapping (L49) + DTO build (L66), `GetIpsQuery` projection, MCP `SaveIpsTool` param (L31), and the `ScoreCandidateCommand` reader (repointed, not just removed).

### `RiskRuleSet` (Risk, schema `risk`, table `risk_rule_sets`)

| Field | Type | Change |
|---|---|---|
| `MaxPositionWeightPct` | `decimal?` (`numeric(9,6)`, fraction (0,1]) | **KEEP** — sole home of the single-position cap |
| `AllocationTargets` | `List<AllocationTargetEntry(AssetClass, TargetPct, DriftBandPct)>` (jsonb `allocation_targets_json`) | **REMOVE** — allocation moves to IPS |
| `MaxSleeveWeightPct`, `MinCashBufferPct`, `MaxLossPerThesisPct`, `MaxNewPositionPct`, `TurnoverBudgetPerQuarter` | (existing) | **KEEP unchanged** — `MaxNewPositionPct` is a distinct concept (new-position sizing), NOT the single-position cap |

Removal touches: entity property + nested `AllocationTargetEntry` record (if unused elsewhere), EF mapping in `RiskDbContext` (`allocation_targets_json`, L39–45 incl. `ValueComparer`), `RiskRuleSetDto` (L16), `SaveRiskRuleSetCommand` param (L16) + per-target validation (L57–70) + mapping, `GetRiskRuleSetQuery`, REST `SaveRiskRulesRequest` (L117), MCP `SaveRiskRulesTool` param (L25), and the `RiskEvaluationService` drift reader (repointed).

---

## New read ports (cross-module contracts, Principle I)

### `IAllocationPolicySource` — owned by `Risk.Domain.Ports`

```csharp
// Risk reads the single home of allocation (IPS) to run its drift check.
public interface IAllocationPolicySource
{
    // Returns the current user's allocation targets, already translated into the
    // fraction TargetPct + symmetric DriftBandPct tuple the Risk drift comparator uses.
    // Empty list when the user has no IPS / no allocation targets.
    Task<IReadOnlyList<AllocationDriftTarget>> GetAllocationTargetsAsync(Guid userId, CancellationToken ct);
}

public readonly record struct AllocationDriftTarget(string AssetClass, decimal TargetPct, decimal DriftBandPct);
// TargetPct and DriftBandPct are FRACTIONS (0,1], to match book weights.
```

**Adapter** `FinanceSentry.API.Integration.IpsAllocationPolicySource` (in the host, not the module — the two modules don't reference each other) delegates to Research `GetIpsQuery` and applies the R4 translation:
`TargetPct = ips.TargetPct/100`; `DriftBandPct = ((MaxPct−MinPct)/2)/100` when Min/Max set (`>0`), else `Max(rule.AbsoluteBandPct, ips.TargetPct·rule.RelativeBandPct/100)/100`.

### `IPositionCapSource` — owned by `Research.Domain.Ports`

```csharp
// Research (opportunity scoring) reads the single home of the position cap (Risk).
public interface IPositionCapSource
{
    // Current user's max single-position cap as a FRACTION (0,1], or null if unset.
    Task<decimal?> GetMaxPositionWeightAsync(Guid userId, CancellationToken ct);
}
```

**Adapter** `FinanceSentry.API.Integration.RiskPositionCapSource` (in the host) delegates to Risk `GetRiskRuleSetQuery`, returns `dto?.MaxPositionWeightPct`.

**Repointed scoring**: `BuildIpsFit` becomes `async`, drops `ips.MaxSinglePositionPct`, uses `cap = await capSource.GetMaxPositionWeightAsync(...)`; `withinConcentration = currentWeight is null || cap is null || currentWeight <= cap`. Now a fraction-vs-fraction comparison, consistent with enforcement.

---

## Migration reconciliation logic

Both schemas share one Postgres DB → cross-schema SQL in a single migration. **Each migration reconciles the concept whose column it drops, before dropping it, writing the survivor into the other schema's *retained* column** — so the two migrations are independent and apply in any order (no data-loss ordering risk):

- **Research M012** reconciles the **position cap** (reads IPS cap, writes retained Risk cap, then drops the IPS cap column).
- **Risk M002** reconciles the **allocation** (reads Risk allocation, writes retained IPS allocation, then drops the Risk allocation column).

### Position cap — in Research M012 (→ retained Risk `MaxPositionWeightPct`)
```
for each user with an IPS and/or RiskRuleSet current row:
    ipsCap  = ips.MaxSinglePositionPct           -- may be NULL, unit-ambiguous
    riskCap = risk.MaxPositionWeightPct           -- may be NULL, fraction
    ipsCapNorm = ipsCap is null ? null
                 : ipsCap > 1 ? ipsCap/100 : ipsCap        -- normalize to fraction (log)
    survivor = min(present values of {ipsCapNorm, riskCap})  -- stricter (lower) wins
    if survivor is distinct from riskCap:  risk.MaxPositionWeightPct = survivor   -- idempotent guard
    log(survivor, discarded, rule="stricter-cap-wins", normalizationApplied?)
    -- both null → leave NULL (fabricate nothing)
```

### Allocation — in Risk M002 (→ retained IPS `AllocationTargets`)
```
for each user:
    if ips has allocation targets:            -- IPS wins (intent authoritative)
        log(discarded = risk.allocation_targets_json if non-empty, rule="ips-allocation-wins")
        -- no write; IPS already holds the survivor
    elif risk.allocation_targets_json non-empty:   -- one-side-empty → copy Risk→IPS reversibly
        for each entry(assetClass, targetPct[frac], driftBandPct[frac]):
            ips.AllocationTargets += AllocationTarget(
                assetClass,
                targetPct*100,
                (targetPct-driftBandPct)*100,     -- MinPct
                (targetPct+driftBandPct)*100)     -- MaxPct
        log(rule="risk-allocation-migrated-to-ips")
    else:  -- both empty → fabricate nothing
        noop
    -- second run: ips already populated → IPS-wins branch → no further write (idempotent)
```

**Rounding**: match existing `numeric` scales; characterization tests assert no drift.

**Down()**: re-add dropped columns as nullable/empty (one-way data consolidation documented; single home retains reconciled value).

---

## Reconciliation outcome (audit record, migration-time only — not a persisted product entity)

Per user + moved concept: `{ concept, survivingValue, chosenBy(rule), discardedValue?, normalizationApplied? }` written to the migration log / Serilog (FR-011). Not stored as a domain table.

---

## Validation rules preserved

- Risk `SaveRiskRuleSetCommand.Validate` keeps its `(0,1]` range checks for the retained caps; the per-target allocation validation (L57–70) is **removed** with the field.
- IPS save keeps all existing behaviour minus the cap param.
- Absent-value/default fallback per reader is unchanged (FR-007): drift with no targets → no drift; cap null → permissive (as before at each single home).
