namespace FinanceSentry.Modules.Retention.Domain;

/// <summary>Whether a <see cref="BackupRun"/> row records a backup creation or a restore drill (feature 024).</summary>
public enum BackupRunKind
{
    /// <summary>A nightly encrypted <c>pg_dump</c> uploaded off-host.</summary>
    Backup,

    /// <summary>A restore-verification drill of an existing artifact.</summary>
    RestoreVerify,
}
