using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Mcp.Abstractions;

public sealed class LocalMcpSessionRefreshService(
    LocalMcpSession localSession,
    ILogger<LocalMcpSessionRefreshService> logger) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timer = new PeriodicTimer(RefreshInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await localSession.RefreshIfNeededAsync(cancellationToken: stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Local MCP session refresh loop failed.");
        }
    }
}
