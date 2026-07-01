using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

/// <summary>
/// Runs the IBeam reconciler once shortly after the API comes up so every
/// active credential has its per-user gateway container running again — even
/// after a full compose down/up.
/// </summary>
public sealed class IBeamStartupReconcilerHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<IBeamStartupReconcilerHostedService> logger) : BackgroundService
{
    // Small delay so DB migrations + Hangfire setup finish before we touch
    // credentials.
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var reconciler = scope.ServiceProvider.GetRequiredService<IIBeamReconciler>();
            await reconciler.ReconcileAllAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down before the startup reconcile finished — fine.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "IBeam startup reconciliation failed.");
        }
    }
}
