namespace FinanceSentry.Modules.Retention.Domain;

/// <summary>
/// One backup artifact and its verification lifecycle (feature 024, US2). A <see cref="BackupJob"/>
/// inserts a <see cref="BackupRunKind.Backup"/> row as <see cref="BackupVerificationStatus.Pending"/>;
/// the weekly restore drill flips it to <see cref="BackupVerificationStatus.Verified"/> (setting
/// <see cref="VerifiedAt"/>) or <see cref="BackupVerificationStatus.Failed"/>.
/// </summary>
public sealed class BackupRun
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public BackupRunKind Kind { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Off-host object key, e.g. <c>daily/2026-08-06T02-00-00Z.dump.age</c>.</summary>
    public string? ArtifactKey { get; set; }

    public long? SizeBytes { get; set; }

    /// <summary>SHA-256 of the encrypted artifact, for integrity checks.</summary>
    public string? Sha256 { get; set; }

    public bool Encrypted { get; set; } = true;

    public BackupVerificationStatus VerificationStatus { get; set; } = BackupVerificationStatus.Pending;

    public DateTimeOffset? VerifiedAt { get; set; }

    /// <summary>Failure detail when a backup or restore drill failed. Never contains secrets.</summary>
    public string? Error { get; set; }
}
