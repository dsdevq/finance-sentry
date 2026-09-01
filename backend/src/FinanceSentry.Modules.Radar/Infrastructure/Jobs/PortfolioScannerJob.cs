namespace FinanceSentry.Modules.Radar.Infrastructure.Jobs;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Radar.Application.Commands;
using Hangfire;
using Microsoft.Extensions.Logging;

/// <summary>
/// Feature 043 — daily portfolio-state scanner. Runs after the banking/brokerage sync jobs
/// (02:00 UTC) so the book is fresh when we emit drift, concentration, cash-buffer, and
/// sync-health signals. Delegates to <see cref="ComputePortfolioSignalsCommand"/>.
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class PortfolioScannerJob(
    ICommandHandler<ComputePortfolioSignalsCommand, PortfolioScanSummary> handler,
    ILogger<PortfolioScannerJob> logger)
{
    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var summary = await handler.Handle(new ComputePortfolioSignalsCommand(), ct);
        logger.LogInformation(
            "Portfolio scanner complete: users={Users}, emitted={Emitted}, suppressed={Suppressed}.",
            summary.UsersScanned, summary.SignalsEmitted, summary.SignalsSuppressed);
    }
}
