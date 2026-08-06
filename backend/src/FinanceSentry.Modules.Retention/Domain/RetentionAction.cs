namespace FinanceSentry.Modules.Retention.Domain;

/// <summary>The retention decision for a table (feature 024, FR-001).</summary>
public enum RetentionAction
{
    /// <summary>Delete rows older than the window.</summary>
    Purge,

    /// <summary>Replace old fine-grained rows with coarser aggregates beyond the window (US3).</summary>
    Downsample,

    /// <summary>Never automatically removed — an explicit keep-forever decision.</summary>
    Keep,
}
