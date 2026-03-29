namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Modules.BankSync.Domain.Repositories;

/// <summary>
/// Coordinates sync requests from multiple trigger sources (webhooks, scheduler, manual).
/// Ensures only one sync runs at a time per account — additional requests are silently dropped.
/// </summary>
public interface ITransactionSyncCoordinator
{
    /// <summary>Trigger a sync initiated by a Plaid webhook notification.</summary>
    Task<SyncResult> TriggerWebhookSyncAsync(Guid accountId, CancellationToken ct = default);

    /// <summary>Trigger a sync initiated by the recurring background scheduler.</summary>
    Task<SyncResult> TriggerScheduledSyncAsync(Guid accountId, CancellationToken ct = default);

    /// <summary>Trigger a sync initiated manually by the user via the API.</summary>
    Task<SyncResult> TriggerManualSyncAsync(Guid accountId, CancellationToken ct = default);
}

/// <inheritdoc />
public class TransactionSyncCoordinator : ITransactionSyncCoordinator
{
    private readonly ISyncJobRepository    _syncJobs;
    private readonly IScheduledSyncService _syncService;

    public TransactionSyncCoordinator(
        ISyncJobRepository    syncJobs,
        IScheduledSyncService syncService)
    {
        _syncJobs    = syncJobs;
        _syncService = syncService;
    }

    /// <inheritdoc />
    public async Task<SyncResult> TriggerWebhookSyncAsync(Guid accountId, CancellationToken ct = default)
    {
        if (await _syncJobs.HasRunningJobAsync(accountId, ct))
            return new SyncResult(false, 0, 0, "SYNC_IN_PROGRESS", "A sync is already in progress for this account.");

        return await _syncService.PerformFullSyncAsync(accountId, webhookTriggered: true, ct: ct);
    }

    /// <inheritdoc />
    public async Task<SyncResult> TriggerScheduledSyncAsync(Guid accountId, CancellationToken ct = default)
    {
        if (await _syncJobs.HasRunningJobAsync(accountId, ct))
            return new SyncResult(false, 0, 0, "SYNC_IN_PROGRESS", "A sync is already in progress for this account.");

        return await _syncService.PerformFullSyncAsync(accountId, webhookTriggered: false, ct: ct);
    }

    /// <inheritdoc />
    public async Task<SyncResult> TriggerManualSyncAsync(Guid accountId, CancellationToken ct = default)
    {
        if (await _syncJobs.HasRunningJobAsync(accountId, ct))
            return new SyncResult(false, 0, 0, "SYNC_IN_PROGRESS", "A sync is already in progress for this account.");

        return await _syncService.PerformFullSyncAsync(accountId, webhookTriggered: false, ct: ct);
    }
}
