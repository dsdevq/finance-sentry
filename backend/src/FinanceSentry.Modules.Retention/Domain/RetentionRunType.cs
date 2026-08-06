namespace FinanceSentry.Modules.Retention.Domain;

/// <summary>What a <see cref="RetentionRun"/> did (feature 024).</summary>
public enum RetentionRunType
{
    /// <summary>Hard-deleted out-of-policy rows in bounded batches.</summary>
    Purge,

    /// <summary>Replaced fine-grained history with coarser aggregates.</summary>
    Downsample,
}
