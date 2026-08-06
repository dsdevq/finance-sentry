namespace FinanceSentry.Modules.Retention.Domain;

/// <summary>Restore-provability of a backup artifact (feature 024, US2). Powers SC-002.</summary>
public enum BackupVerificationStatus
{
    /// <summary>Created but not yet proven to restore.</summary>
    Pending,

    /// <summary>A restore drill restored it into isolation successfully.</summary>
    Verified,

    /// <summary>A restore drill failed — the artifact is not trustworthy.</summary>
    Failed,
}
