namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

/// <summary>
/// In-memory session tracker for async IBKR connect. Singleton-scoped.
/// Not durable across API restarts — a connect session that was in-flight
/// when the process died is not recoverable; the frontend will observe a
/// missing sessionId on its next poll and restart the flow.
/// </summary>
public interface IIBKRConnectSessionStore
{
    /// <summary>Create a new Pending session bound to the user, with its own
    /// CancellationTokenSource. Returns the session id + a linked token the
    /// orchestrator should observe.</summary>
    (Guid SessionId, CancellationToken Token) Create(Guid userId);

    /// <summary>Fetch a snapshot. Returns null when the session doesn't
    /// exist or belongs to a different user (never reveal cross-user state).</summary>
    IBKRConnectSessionSnapshot? Get(Guid sessionId, Guid userId);

    /// <summary>Trigger the session's cancellation token. Returns false if
    /// the session doesn't exist, belongs to a different user, or is already
    /// in a terminal state.</summary>
    bool Cancel(Guid sessionId, Guid userId);

    /// <summary>Update non-terminal status (Spawning / AwaitingAuth / Syncing).</summary>
    void TransitionTo(Guid sessionId, IBKRConnectStatus status);

    /// <summary>Terminal: success.</summary>
    void MarkCompleted(Guid sessionId, ConnectIBKRResult result);

    /// <summary>Terminal: caller-observable error.</summary>
    void MarkFailed(Guid sessionId, string errorCode, string errorMessage);

    /// <summary>Terminal: cancelled by caller or shutdown.</summary>
    void MarkCancelled(Guid sessionId);
}
