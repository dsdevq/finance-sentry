namespace FinanceSentry.Modules.Retention.Infrastructure.Jobs;

using FinanceSentry.Modules.Retention.Application.Services;
using Hangfire;

/// <summary>
/// Weekly restore drill (feature 024, US2 / FR-006). Delegates to <see cref="RestoreVerifier"/>.
/// <c>[AutomaticRetry(Attempts = 0)]</c> — a failed drill should alert, not silently retry.
/// </summary>
[AutomaticRetry(Attempts = 0)]
public sealed class RestoreVerifyJob(RestoreVerifier verifier)
{
    public Task RunAsync(CancellationToken ct = default) => verifier.RunAsync(ct);
}
