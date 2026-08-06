namespace FinanceSentry.Modules.Retention.Domain;

/// <summary>Terminal result of a <see cref="RetentionRun"/> (feature 024).</summary>
public enum RetentionOutcome
{
    /// <summary>Every policy in the run completed cleanly.</summary>
    Success,

    /// <summary>At least one policy failed but others completed; details in the run's error field.</summary>
    PartialSuccess,

    /// <summary>The run aborted before completing any policy.</summary>
    Failed,
}
