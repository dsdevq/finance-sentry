namespace FinanceSentry.Modules.Retention.Infrastructure.Jobs;

using System.Security.Cryptography;
using FinanceSentry.Modules.Retention.Application;
using FinanceSentry.Modules.Retention.Domain;
using FinanceSentry.Modules.Retention.Infrastructure.Backup;
using FinanceSentry.Modules.Retention.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Nightly off-host backup (feature 024, US2): custom-format <c>pg_dump</c> → age-encrypt → upload to R2,
/// record a <see cref="BackupVerificationStatus.Pending"/> <see cref="BackupRun"/>, then prune old
/// artifacts so the backup store stays bounded. No-ops with a warning when R2/age is unconfigured.
/// </summary>
[AutomaticRetry(Attempts = 0)]
public sealed class BackupJob(
    RetentionDbContext db,
    IBackupStore store,
    PgDumpRunner runner,
    IOptions<BackupOptions> options,
    ILogger<BackupJob> logger)
{
    private readonly BackupOptions _options = options.Value;

    public async Task RunAsync(CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
        {
            logger.LogWarning("BackupJob skipped: R2/age backup is not configured (BACKUP_* unset).");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var fileName = BackupNaming.FileName(now);
        var key = BackupNaming.KeyFor(now, _options.WeeklyOn);
        var localPath = Path.Combine(Path.GetTempPath(), fileName);

        try
        {
            await runner.DumpAndEncryptAsync(localPath, ct);
            var size = new FileInfo(localPath).Length;
            var sha = await ComputeSha256Async(localPath, ct);

            await using (var stream = File.OpenRead(localPath))
                await store.PutAsync(key, stream, ct);

            db.BackupRuns.Add(new BackupRun
            {
                Kind = BackupRunKind.Backup,
                CreatedAt = now,
                ArtifactKey = key,
                SizeBytes = size,
                Sha256 = sha,
                Encrypted = true,
                VerificationStatus = BackupVerificationStatus.Pending,
            });
            await db.SaveChangesAsync(ct);

            logger.LogInformation("BackupJob uploaded {Key} ({Size} bytes).", key, size);

            await PruneAsync(BackupNaming.DailyPrefix, _options.RetainDaily, ct);
            await PruneAsync(BackupNaming.WeeklyPrefix, _options.RetainWeekly, ct);
        }
        catch (Exception ex)
        {
            db.BackupRuns.Add(new BackupRun
            {
                Kind = BackupRunKind.Backup,
                CreatedAt = now,
                ArtifactKey = key,
                Encrypted = true,
                VerificationStatus = BackupVerificationStatus.Failed,
                Error = ex.Message,
            });
            await db.SaveChangesAsync(CancellationToken.None);
            throw; // surface to Hangfire → failure metrics + consecutive-failure Telegram alert (023).
        }
        finally
        {
            TryDelete(localPath);
        }
    }

    private async Task PruneAsync(string prefix, int keep, CancellationToken ct)
    {
        var objects = await store.ListAsync(prefix, ct); // newest first
        var stale = objects.Skip(keep).ToList();
        foreach (var obj in stale)
        {
            await store.DeleteAsync(obj.Key, ct);
            await db.BackupRuns.Where(b => b.ArtifactKey == obj.Key).ExecuteDeleteAsync(ct);
        }
        if (stale.Count > 0)
            logger.LogInformation("BackupJob pruned {Count} artifacts under {Prefix}.", stale.Count, prefix);
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexStringLower(hash);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best-effort */ }
    }
}
