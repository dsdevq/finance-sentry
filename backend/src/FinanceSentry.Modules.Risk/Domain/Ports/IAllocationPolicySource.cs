namespace FinanceSentry.Modules.Risk.Domain.Ports;

/// <summary>
/// Read-only cross-module port (feature 039). Allocation drift is evaluated against the target
/// allocation held in its single home — the Investment Policy Statement (Research module) — not a
/// copy on the Risk rule set. The concrete adapter lives in the composition root
/// (FinanceSentry.API) so the Risk and Research modules never reference each other.
/// Targets are already translated into the fraction <see cref="AllocationDriftTarget.TargetPct"/> +
/// symmetric <see cref="AllocationDriftTarget.DriftBandPct"/> the drift comparator uses, matching
/// BookSnapshot position weights.
/// </summary>
public interface IAllocationPolicySource
{
    Task<IReadOnlyList<AllocationDriftTarget>> GetAllocationTargetsAsync(Guid userId, CancellationToken ct);
}

/// <summary>
/// A target weight for an asset class, ready for drift evaluation.
/// <paramref name="TargetPct"/> and <paramref name="DriftBandPct"/> are fractions in (0,1].
/// </summary>
public readonly record struct AllocationDriftTarget(string AssetClass, decimal TargetPct, decimal DriftBandPct);
