namespace FinanceSentry.Modules.Alerts.Infrastructure.Jobs;

using FinanceSentry.Modules.Alerts.Domain.Repositories;
using Microsoft.Extensions.Logging;

public sealed class AlertPurgeJob(
    IAlertRepository alerts,
    ILogger<AlertPurgeJob> logger)
{
    private const int RetentionDays = 90;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-RetentionDays);
        var deleted = await alerts.PurgeOldAsync(cutoff, ct);
        logger.LogInformation("AlertPurgeJob deleted {Count} alerts older than {Cutoff:O}", deleted, cutoff);
    }
}
