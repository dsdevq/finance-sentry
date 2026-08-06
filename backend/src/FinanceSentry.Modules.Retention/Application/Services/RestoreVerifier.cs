namespace FinanceSentry.Modules.Retention.Application.Services;

using FinanceSentry.Infrastructure.Observability;
using FinanceSentry.Modules.Retention.Application;
using FinanceSentry.Modules.Retention.Domain;
using FinanceSentry.Modules.Retention.Infrastructure.Backup;
using FinanceSentry.Modules.Retention.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

/// <summary>
/// Proves the latest backup restores (feature 024, US2 / FR-006). Downloads it, restores into a fresh,
/// uniquely-named scratch database, runs read-only sanity checks there, then drops the scratch DB. The
/// scratch database is the isolation boundary: <c>pg_restore</c> and every check target only it, so the
/// production database is never written (spec edge case). Flips the artifact's <see cref="BackupRun"/> to
/// Verified/Failed and records a <see cref="BackupRunKind.RestoreVerify"/> drill row.
/// </summary>
public sealed class RestoreVerifier(
    RetentionDbContext db,
    IBackupStore store,
    PgDumpRunner runner,
    JobMetrics metrics,
    IOptions<BackupOptions> options,
    ILogger<RestoreVerifier> logger)
{
    private const int MinExpectedTables = 20;
    private readonly BackupOptions _options = options.Value;

    public async Task RunAsync(CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
        {
            logger.LogWarning("RestoreVerifyJob skipped: R2/age backup is not configured.");
            return;
        }

        var latest = await LatestArtifactAsync(ct);
        if (latest is null)
        {
            logger.LogWarning("RestoreVerifyJob: no backup artifacts found to verify.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var scratchDb = $"restore_verify_{now:yyyyMMddHHmmss}";
        var localPath = Path.Combine(Path.GetTempPath(), $"verify-{Guid.NewGuid():N}.dump.age");
        var drill = new BackupRun
        {
            Kind = BackupRunKind.RestoreVerify,
            CreatedAt = now,
            ArtifactKey = latest.Key,
            Encrypted = true,
        };

        try
        {
            await store.DownloadToFileAsync(latest.Key, localPath, ct);
            await runner.CreateDatabaseAsync(scratchDb, ct);
            await runner.DecryptAndRestoreAsync(localPath, scratchDb, ct);
            await VerifyRestoredAsync(scratchDb, ct);

            drill.VerificationStatus = BackupVerificationStatus.Verified;
            drill.VerifiedAt = DateTimeOffset.UtcNow;
            await MarkArtifactAsync(latest.Key, BackupVerificationStatus.Verified, drill.VerifiedAt, null, ct);
            metrics.SetLastBackupVerified(drill.VerifiedAt.Value);
            logger.LogInformation("RestoreVerifyJob: {Key} restored and verified into {Scratch}.", latest.Key, scratchDb);
        }
        catch (Exception ex)
        {
            drill.VerificationStatus = BackupVerificationStatus.Failed;
            drill.Error = ex.Message;
            await MarkArtifactAsync(latest.Key, BackupVerificationStatus.Failed, null, ex.Message, ct);
            logger.LogError(ex, "RestoreVerifyJob: verification of {Key} FAILED.", latest.Key);
            db.BackupRuns.Add(drill);
            await db.SaveChangesAsync(CancellationToken.None);
            throw; // surface to Hangfire → failure metrics + Telegram alert (023).
        }
        finally
        {
            await SafeDropAsync(scratchDb, ct);
            TryDelete(localPath);
        }

        db.BackupRuns.Add(drill);
        await db.SaveChangesAsync(ct);
    }

    private async Task<BackupObject?> LatestArtifactAsync(CancellationToken ct)
    {
        var daily = await store.ListAsync("daily/", ct);
        var weekly = await store.ListAsync("weekly/", ct);
        return daily.Concat(weekly).MaxBy(o => o.LastModified);
    }

    private async Task VerifyRestoredAsync(string scratchDb, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(runner.ScratchConnectionString(scratchDb));
        await conn.OpenAsync(ct);

        var tableCount = await ScalarAsync(conn,
            "SELECT count(*)::int FROM information_schema.tables WHERE table_schema NOT IN " +
            "('pg_catalog','information_schema')", ct);
        if (tableCount < MinExpectedTables)
            throw new InvalidOperationException(
                $"Restored DB has only {tableCount} tables (< {MinExpectedTables}); restore looks incomplete.");

        // Touch a core user table — a broken restore would fail to resolve the relation.
        _ = await ScalarAsync(conn, "SELECT count(*)::int FROM \"bank_sync\".\"Transactions\"", ct);
    }

    private static async Task<int> ScalarAsync(NpgsqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is int i ? i : 0;
    }

    private async Task MarkArtifactAsync(
        string key, BackupVerificationStatus status, DateTimeOffset? verifiedAt, string? error, CancellationToken ct)
    {
        await db.BackupRuns
            .Where(b => b.Kind == BackupRunKind.Backup && b.ArtifactKey == key)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.VerificationStatus, status)
                .SetProperty(b => b.VerifiedAt, verifiedAt)
                .SetProperty(b => b.Error, error), ct);
    }

    private async Task SafeDropAsync(string scratchDb, CancellationToken ct)
    {
        try { await runner.DropDatabaseAsync(scratchDb, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to drop scratch DB {Scratch}.", scratchDb); }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best-effort */ }
    }
}
