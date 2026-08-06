namespace FinanceSentry.Modules.Retention.Application;

/// <summary>
/// Off-host backup configuration (feature 024, US2), bound from the <c>Backup:</c> section. Secrets
/// (R2 keys, age keypair) arrive via <c>BACKUP_*</c> environment variables mapped into this section.
/// When <see cref="IsConfigured"/> is false the backup jobs no-op with a warning (dev/local safe).
/// </summary>
public sealed class BackupOptions
{
    public const string SectionName = "Backup";

    /// <summary>UTC hour the nightly backup runs.</summary>
    public int BackupHourUtc { get; set; } = 2;

    /// <summary>Cloudflare R2 S3-compatible endpoint, e.g. https://&lt;account&gt;.r2.cloudflarestorage.com.</summary>
    public string? R2Endpoint { get; set; }

    public string? R2Bucket { get; set; }

    public string? R2AccessKey { get; set; }

    public string? R2SecretKey { get; set; }

    /// <summary>age public recipient used to encrypt dumps before upload.</summary>
    public string? AgeRecipient { get; set; }

    /// <summary>age secret identity — only used by the restore-verify job to decrypt.</summary>
    public string? AgeIdentity { get; set; }

    /// <summary>Number of newest daily artifacts to retain (older pruned).</summary>
    public int RetainDaily { get; set; } = 30;

    /// <summary>Number of newest weekly artifacts to retain.</summary>
    public int RetainWeekly { get; set; } = 8;

    /// <summary>ISO day-of-week (1=Mon..7=Sun) a backup is also promoted to the weekly set.</summary>
    public int WeeklyOn { get; set; } = 7;

    /// <summary>True only when R2 + age encryption are fully configured.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(R2Endpoint)
        && !string.IsNullOrWhiteSpace(R2Bucket)
        && !string.IsNullOrWhiteSpace(R2AccessKey)
        && !string.IsNullOrWhiteSpace(R2SecretKey)
        && !string.IsNullOrWhiteSpace(AgeRecipient);
}
