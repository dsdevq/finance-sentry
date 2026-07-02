namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

/// <summary>
/// Fire-and-forget entry point for async IBKR connect. The controller calls
/// <see cref="Start(Guid, string, string)"/> and returns immediately with the
/// session id; the actual work runs on a background task and updates the
/// session store as it progresses.
/// </summary>
public interface IIBKRConnectOrchestrator
{
    Guid Start(Guid userId, string username, string password);
}
