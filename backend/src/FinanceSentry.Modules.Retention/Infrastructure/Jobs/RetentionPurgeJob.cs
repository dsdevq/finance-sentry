namespace FinanceSentry.Modules.Retention.Infrastructure.Jobs;

using FinanceSentry.Modules.Retention.Application.Services;
using Hangfire;

/// <summary>
/// Nightly generic retention purge (feature 024, US1). Delegates to <see cref="RetentionPurgeService"/>.
/// <c>[AutomaticRetry(Attempts = 0)]</c>: a purge must not double-run; it is idempotent on its own but
/// re-scheduling is the recovery path, not Hangfire retries.
/// </summary>
[AutomaticRetry(Attempts = 0)]
public sealed class RetentionPurgeJob(RetentionPurgeService service)
{
    /// <param name="dryRun">When true, counts out-of-policy rows and records the run without deleting.</param>
    public Task RunAsync(bool dryRun = false, CancellationToken ct = default)
        => service.RunAsync(dryRun, ct);
}
