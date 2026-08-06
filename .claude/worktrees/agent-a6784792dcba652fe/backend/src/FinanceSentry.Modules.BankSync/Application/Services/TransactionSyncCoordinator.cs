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
public class TransactionSyncCoordinator(
    ISyncJobRepository syncJobs,
    IBankAccountRepository accounts,
    IScheduledSyncService syncService) : ITransactionSyncCoordinator
{
    private readonly ISyncJobRepository _syncJobs = syncJobs;
    private readonly IBankAccountRepository _accounts = accounts;
    private readonly IScheduledSyncService _syncService = syncService;

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

        // An account whose provider consent has expired/been revoked cannot sync until the user
        // reconnects. Skip it in the recurring scheduler so it stops failing every cycle; the reconnect
        // flow (manual/webhook path) clears the state via MarkActive. Manual syncs are unaffected.
        var account = await _accounts.GetByIdAsync(accountId, ct);
        if (account?.SyncStatus == "reauth_required")
            return new SyncResult(false, 0, 0, "ITEM_LOGIN_REQUIRED", "Account requires reconnection; scheduled sync skipped.");

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
