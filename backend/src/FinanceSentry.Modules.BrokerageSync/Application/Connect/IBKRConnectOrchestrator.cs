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
        // Idempotency: if the user already has a session mid-flight (they
        // impatiently clicked Connect a second time while the first one was
        // still spawning IBeam / waiting on 2FA), return the existing id so
        // the frontend keeps polling the SAME session. Without this, click 2
        // would race click 1: it hits credentialRepo, sees IsActive=true from
        // click 1's Reactivate, and short-circuits with IBKR_DUPLICATE — which
        // surfaces a misleading "already connected" toast to the user.
        var existing = sessionStore.FindActiveByUser(userId);
        if (existing is not null)
        {
            logger.LogInformation(
                "IBKR connect: user {UserId} already has an in-flight session {SessionId}; returning that id instead of starting a new one",
                userId, existing.Value);
            return existing.Value;
        }

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
