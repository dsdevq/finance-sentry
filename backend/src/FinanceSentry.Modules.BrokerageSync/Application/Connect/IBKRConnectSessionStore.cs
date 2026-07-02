using System.Collections.Concurrent;

namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

public sealed class IBKRConnectSessionStore : IIBKRConnectSessionStore, IDisposable
{
    private static readonly TimeSpan TerminalRetention = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<Guid, SessionEntry> _sessions = new();
    private readonly object _lock = new();

    public (Guid SessionId, CancellationToken Token) Create(Guid userId)
    {
        SweepExpired();

        var sessionId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        var entry = new SessionEntry
        {
            Id = sessionId,
            UserId = userId,
            Status = IBKRConnectStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Cts = cts,
        };

        _sessions[sessionId] = entry;
        return (sessionId, cts.Token);
    }

    public Guid? FindActiveByUser(Guid userId)
    {
        foreach (var entry in _sessions.Values)
        {
            if (entry.UserId != userId)
                continue;
            lock (entry.SyncRoot)
            {
                if (!IsTerminal(entry.Status))
                    return entry.Id;
            }
        }
        return null;
    }

    public IBKRConnectSessionSnapshot? Get(Guid sessionId, Guid userId)
    {
        if (!_sessions.TryGetValue(sessionId, out var entry) || entry.UserId != userId)
            return null;

        lock (entry.SyncRoot)
        {
            return new IBKRConnectSessionSnapshot(
                entry.Id,
                entry.Status,
                entry.ErrorCode,
                entry.ErrorMessage,
                entry.Result,
                entry.CreatedAt,
                entry.UpdatedAt);
        }
    }

    public bool Cancel(Guid sessionId, Guid userId)
    {
        if (!_sessions.TryGetValue(sessionId, out var entry) || entry.UserId != userId)
            return false;

        lock (entry.SyncRoot)
        {
            if (IsTerminal(entry.Status))
                return false;
        }

        try
        {
            entry.Cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Race with cleanup — session already finalized.
            return false;
        }

        return true;
    }

    public void TransitionTo(Guid sessionId, IBKRConnectStatus status)
    {
        if (!_sessions.TryGetValue(sessionId, out var entry))
            return;

        lock (entry.SyncRoot)
        {
            if (IsTerminal(entry.Status))
                return;
            entry.Status = status;
            entry.UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MarkCompleted(Guid sessionId, ConnectIBKRResult result)
    {
        if (!_sessions.TryGetValue(sessionId, out var entry))
            return;

        lock (entry.SyncRoot)
        {
            entry.Status = IBKRConnectStatus.Completed;
            entry.Result = result;
            entry.UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MarkFailed(Guid sessionId, string errorCode, string errorMessage)
    {
        if (!_sessions.TryGetValue(sessionId, out var entry))
            return;

        lock (entry.SyncRoot)
        {
            entry.Status = IBKRConnectStatus.Failed;
            entry.ErrorCode = errorCode;
            entry.ErrorMessage = errorMessage;
            entry.UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MarkCancelled(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var entry))
            return;

        lock (entry.SyncRoot)
        {
            entry.Status = IBKRConnectStatus.Cancelled;
            entry.UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Dispose()
    {
        foreach (var entry in _sessions.Values)
        {
            try { entry.Cts.Dispose(); } catch { /* nothing safe to log at Dispose */ }
        }
        _sessions.Clear();
    }

    private void SweepExpired()
    {
        if (!Monitor.TryEnter(_lock))
            return;

        try
        {
            var cutoff = DateTime.UtcNow - TerminalRetention;
            foreach (var (id, entry) in _sessions)
            {
                bool remove;
                lock (entry.SyncRoot)
                {
                    remove = IsTerminal(entry.Status) && entry.UpdatedAt < cutoff;
                }

                if (remove && _sessions.TryRemove(id, out var removed))
                {
                    try { removed.Cts.Dispose(); } catch { }
                }
            }
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    private static bool IsTerminal(IBKRConnectStatus status) =>
        status is IBKRConnectStatus.Completed
            or IBKRConnectStatus.Failed
            or IBKRConnectStatus.Cancelled;

    private sealed class SessionEntry
    {
        public required Guid Id { get; init; }
        public required Guid UserId { get; init; }
        public required IBKRConnectStatus Status { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public ConnectIBKRResult? Result { get; set; }
        public required DateTime CreatedAt { get; init; }
        public required DateTime UpdatedAt { get; set; }
        public required CancellationTokenSource Cts { get; init; }
        public object SyncRoot { get; } = new();
    }
}
