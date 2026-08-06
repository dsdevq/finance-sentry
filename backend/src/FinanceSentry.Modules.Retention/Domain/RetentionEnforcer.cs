namespace FinanceSentry.Modules.Retention.Domain;

/// <summary>Who enforces a purge/downsample policy (feature 024, research D2).</summary>
public enum RetentionEnforcer
{
    /// <summary>The generic retention engine in this module runs the deletion.</summary>
    Generic,

    /// <summary>
    /// A pre-existing bespoke module job already governs this table; the generic engine skips it.
    /// The registry still lists it so every table has a documented decision (FR-001).
    /// </summary>
    Bespoke,
}
