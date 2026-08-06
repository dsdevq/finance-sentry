namespace FinanceSentry.Modules.Retention.Infrastructure;

using FinanceSentry.Infrastructure.Observability;
using FinanceSentry.Modules.Retention.Domain;
using FinanceSentry.Modules.Retention.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Seeds the retention/backup age gauges from the database at startup (feature 024) so
/// <c>finance_backup_last_verified_*</c> and <c>finance_retention_last_run_age_seconds</c> survive a
/// restart instead of reading empty until the next scheduled run. Best-effort — never blocks startup.
/// </summary>
public sealed class RetentionMetricsPrimer(
    IServiceScopeFactory scopeFactory,
    JobMetrics metrics,
    ILogger<RetentionMetricsPrimer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RetentionDbContext>();

            var lastPurge = await db.RetentionRuns
                .Where(r => r.RunType == RetentionRunType.Purge && r.CompletedAt != null)
                .OrderByDescending(r => r.CompletedAt)
                .Select(r => r.CompletedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (lastPurge is { } purge)
                metrics.SetLastRetentionRun(RetentionRunType.Purge.ToString(), purge);

            var lastVerified = await db.BackupRuns
                .Where(b => b.VerificationStatus == BackupVerificationStatus.Verified && b.VerifiedAt != null)
                .OrderByDescending(b => b.VerifiedAt)
                .Select(b => b.VerifiedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (lastVerified is { } verified)
                metrics.SetLastBackupVerified(verified);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RetentionMetricsPrimer could not seed gauges; they will populate on next run.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
