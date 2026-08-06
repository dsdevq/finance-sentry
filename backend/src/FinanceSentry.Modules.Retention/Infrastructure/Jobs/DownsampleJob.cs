namespace FinanceSentry.Modules.Retention.Infrastructure.Jobs;

using FinanceSentry.Modules.Retention.Application;
using FinanceSentry.Modules.Retention.Application.Services;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Gated downsampling of history tables (feature 024, US3, P2). Off by default
/// (<c>Retention:Downsample:Enabled</c>) because it irreversibly compacts history — enable only after
/// validating aggregates on a copy. No-ops (logs) when disabled.
/// </summary>
[AutomaticRetry(Attempts = 0)]
public sealed class DownsampleJob(
    DownsampleService service,
    IOptions<RetentionOptions> options,
    ILogger<DownsampleJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        if (!options.Value.Downsample.Enabled)
        {
            logger.LogInformation("DownsampleJob skipped: Retention:Downsample:Enabled is false.");
            return;
        }

        await service.RunAsync(ct);
    }
}
