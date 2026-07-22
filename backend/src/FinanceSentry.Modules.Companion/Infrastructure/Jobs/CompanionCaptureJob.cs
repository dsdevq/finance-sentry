namespace FinanceSentry.Modules.Companion.Infrastructure.Jobs;

using FinanceSentry.Modules.Companion.Application.Services;
using Hangfire;

/// <summary>
/// Frequent capture pass (feature 031, US2): polls detectors for new material events and records them
/// with a mode-based disposition. Overlap-protected. Every 1 min so realtime dispatch meets the 60s bar.
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 120)]
public sealed class CompanionCaptureJob(ICompanionEventCapture capture)
{
    [AutomaticRetry(Attempts = 0)]
    public Task ExecuteAsync(CancellationToken ct = default) => capture.CaptureAsync(ct);
}
