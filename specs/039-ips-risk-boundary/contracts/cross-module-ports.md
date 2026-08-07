# Contract: Cross-Module Read Ports (internal, Principle I)

New internal contracts letting one module read the other's single home without coupling to its persistence. Read-only; resolved by DI at the composition root. Adapters delegate to the other module's **existing** query handler (not its DbContext/repository).

**Placement (load-bearing)**: `FinanceSentry.Modules.Research` and `FinanceSentry.Modules.Risk` do **not** reference each other (verified). Putting an adapter inside either module's Infrastructure would create a cyclic assembly reference (Risk→Research **and** Research→Risk) that won't compile. Therefore **both adapters live in `FinanceSentry.API/Integration/`** — the host references both modules — and are DI-registered via `AddCrossModulePorts()` in `Program.cs`. `FinanceSentry.Mcp` also references both and is an acceptable alternate home, but the API composition root is canonical.

## `IAllocationPolicySource` (consumed by Risk, implemented in the host via Research)

- **Owner (port)**: `FinanceSentry.Modules.Risk.Domain.Ports`
- **Consumer**: `RiskEvaluationService` allocation-drift check (replaces its read of `RiskRuleSet.AllocationTargets`)
- **Adapter**: `FinanceSentry.API.Integration.IpsAllocationPolicySource` → Research `IQueryHandler<GetIpsQuery, IpsDto?>`

```csharp
Task<IReadOnlyList<AllocationDriftTarget>> GetAllocationTargetsAsync(Guid userId, CancellationToken ct);
public readonly record struct AllocationDriftTarget(string AssetClass, decimal TargetPct, decimal DriftBandPct);
// TargetPct & DriftBandPct are FRACTIONS (0,1] — matches BookSnapshot weights.
```

**Translation (adapter)** — IPS whole-percent/min-max/rule → fraction target + symmetric band (see research.md R4):
- `TargetPct = ips.TargetPct / 100`
- `DriftBandPct = ((MaxPct − MinPct) / 2) / 100` when `MinPct/MaxPct > 0`
- else `DriftBandPct = Max(rule.AbsoluteBandPct, ips.TargetPct · rule.RelativeBandPct / 100) / 100`
- empty when no IPS / no targets → drift check emits nothing (unchanged absent-value behaviour, FR-007)

**Contract behaviour**: for equivalent inputs the repointed drift check MUST emit the **same** `RiskRuleKeys.AllocationDrift` violations (key, actual, target, excessUsd, severity) it emitted reading the Risk copy (FR-004, SC-002).

## `IPositionCapSource` (consumed by Research, implemented in the host via Risk)

- **Owner (port)**: `FinanceSentry.Modules.Research.Domain.Ports`
- **Consumer**: `ScoreCandidateCommand.BuildIpsFit` (replaces its read of `ips.MaxSinglePositionPct`)
- **Adapter**: `FinanceSentry.API.Integration.RiskPositionCapSource` → Risk `IQueryHandler<GetRiskRuleSetQuery, RiskRuleSetDto?>`

```csharp
Task<decimal?> GetMaxPositionWeightAsync(Guid userId, CancellationToken ct);
// FRACTION (0,1], or null if unset.
```

**Contract behaviour**: `withinConcentration = currentWeight is null || cap is null || currentWeight <= cap` — a fraction-vs-fraction comparison consistent with Risk enforcement. Where the IPS and Risk caps agreed (same normalized fraction), the score is identical to before (FR-006, SC-002). Where they disagreed, the single Risk source now governs (intended correction — documented, validated against live data per research.md R7).

## No cycle

Each module depends only on an interface it **owns** (in its own Domain). The concrete adapters live in the **host (`FinanceSentry.API/Integration/`)**, which already references both modules and their Application query contracts (the same references MCP tools hold). No module references the other → no compile-time assembly cycle. DI wiring (`AddCrossModulePorts()`) lives at the composition root.
