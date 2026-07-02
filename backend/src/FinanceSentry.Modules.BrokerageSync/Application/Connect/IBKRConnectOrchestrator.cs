using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

public sealed class IBKRConnectOrchestrator(
    IServiceScopeFactory scopeFactory,
    IIBKRConnectSessionStore sessionStore,
    ILogger<IBKRConnectOrchestrator> logger) : IIBKRConnectOrchestrator
{
    public Guid Start(Guid userId, string username, string password)
    {
        var (sessionId, sessionToken) = sessionStore.Create(userId);

        // Fire-and-forget: the runner owns the session lifecycle from here.
        // Detached from the HTTP request scope so client disconnects don't
        // cancel the work — that is the whole point of the async flow.
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var runner = scope.ServiceProvider.GetRequiredService<IBKRConnectRunner>();
                await runner.RunAsync(sessionId, userId, username, password, sessionToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "IBKR connect orchestrator crashed for session {SessionId}", sessionId);
                sessionStore.MarkFailed(sessionId, "INTERNAL_ERROR", "Connect orchestrator crashed.");
            }
        }, CancellationToken.None);

        return sessionId;
    }
}
