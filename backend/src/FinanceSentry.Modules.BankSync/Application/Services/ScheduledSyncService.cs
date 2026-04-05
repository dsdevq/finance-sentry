namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Infrastructure.Logging;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;

/// <summary>
/// Result returned by a sync operation.
/// </summary>
public record SyncResult(
    bool Success,
    int TransactionCountFetched,
    int TransactionCountDeduped,
    string? ErrorCode,
    string? ErrorMessage);

/// <summary>
/// Drives the full transaction sync lifecycle for a single account:
/// create job → decrypt token → fetch from Plaid → deduplicate → persist → update account state.
/// </summary>
public interface IScheduledSyncService
{
    /// <summary>
    /// Performs a full sync for the given account.
    /// </summary>
    Task<SyncResult> PerformFullSyncAsync(
        Guid accountId,
        bool webhookTriggered = false,
        CancellationToken ct = default);
}

/// <inheritdoc />
public class ScheduledSyncService(
    IBankAccountRepository accounts,
    ITransactionRepository transactions,
    ISyncJobRepository syncJobs,
    IEncryptedCredentialRepository credentials,
    ICredentialEncryptionService encryption,
    IPlaidAdapter plaid,
    ITransactionDeduplicationService dedup,
    IBankSyncLogger logger) : IScheduledSyncService
{
    private readonly IBankAccountRepository _accounts = accounts;
    private readonly ITransactionRepository _transactions = transactions;
    private readonly ISyncJobRepository _syncJobs = syncJobs;
    private readonly IEncryptedCredentialRepository _credentials = credentials;
    private readonly ICredentialEncryptionService _encryption = encryption;
    private readonly IPlaidAdapter _plaid = plaid;
    private readonly ITransactionDeduplicationService _dedup = dedup;
    private readonly IBankSyncLogger _logger = logger;

    /// <inheritdoc />
    public async Task<SyncResult> PerformFullSyncAsync(
          Guid accountId,
          bool webhookTriggered = false,
          CancellationToken ct = default)
    {
        var startedAt = DateTime.UtcNow;

        // 1. Load account
        var account = await _accounts.GetByIdAsync(accountId, ct);
        if (account == null)
            return new SyncResult(false, 0, 0, "ACCOUNT_NOT_FOUND", "Account not found.");

        // 2. Create and persist a running SyncJob
        var job = new SyncJob(accountId, account.UserId)
        {
            Status = "running",
            WebhookTriggered = webhookTriggered,
            StartedAt = startedAt
        };
        await _syncJobs.AddAsync(job, ct);

        _logger.SyncStarted(job.CorrelationId ?? job.Id.ToString(), accountId);

        try
        {
            // 3. Transition account to syncing state (BeginSync allows active/failed/pending/reauth_required)
            account.BeginSync();
            await _accounts.UpdateAsync(account, ct);

            // 4. Load and decrypt access token
            var cred = await _credentials.GetByAccountIdAsync(accountId, ct)
                ?? throw new InvalidOperationException($"No credential found for account {accountId}.");

            var accessToken = _encryption.Decrypt(cred.EncryptedData, cred.Iv, cred.AuthTag, cred.KeyVersion);
            _logger.CredentialAccessed(job.CorrelationId ?? job.Id.ToString(), accountId);

            // 5. Determine date range: since last sync or last 30 days
            var latestJob = await _syncJobs.GetLatestByAccountIdAsync(accountId, ct);
            var since = latestJob?.LastTransactionDate?.AddDays(-1)  // overlap by 1 day for safety
                        ?? DateTime.UtcNow.AddDays(-30);
            var until = DateTime.UtcNow;

            // 6. Fetch transactions from Plaid
            var candidates = await _plaid.GetTransactionsAsync(
                accessToken, accountId, account.UserId, since, until, ct);

            // 7. Load existing hashes for deduplication
            var existing = (await _transactions.GetByAccountIdAsync(accountId, ct))
                .Select(t => t.UniqueHash)
                .ToHashSet();

            var newCandidates = _dedup.FilterDuplicates(candidates, existing);
            var entities = newCandidates.Select(_dedup.ToEntity).ToList();

            // 8. Bulk insert new transactions
            if (entities.Count > 0)
                await _transactions.AddRangeAsync(entities, ct);

            // 9. Fetch updated balance and mark account active
            var accounts = await _plaid.GetAccountsWithBalanceAsync(accessToken, ct);
            var balance = accounts.FirstOrDefault()?.CurrentBalance ?? 0m;

            account.MarkActive(balance);
            await _accounts.UpdateAsync(account, ct);

            // 10. Determine last transaction date
            var lastTxDate = entities.Count > 0
                ? entities.Max(t => t.PostedDate ?? t.TransactionDate)
                : (DateTime?)null;

            // 11. Mark job success
            job.MarkSuccess(candidates.Count, entities.Count, lastTxDate);
            await _syncJobs.UpdateAsync(job, ct);

            var durationMs = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds;
            _logger.SyncCompleted(job.CorrelationId ?? job.Id.ToString(), accountId,
                candidates.Count, entities.Count, durationMs);

            return new SyncResult(true, candidates.Count, entities.Count, null, null);
        }
        catch (Exception ex)
        {
            // Derive a safe error code from the exception message when possible
            var errorCode = ExtractErrorCode(ex.Message);

            // Mark job failed
            job.MarkFailed(ex.Message, errorCode);
            await _syncJobs.UpdateAsync(job, ct);

            // Update account state
            try
            {
                var freshAccount = await _accounts.GetByIdAsync(accountId, ct);
                if (freshAccount != null)
                {
                    if (errorCode == "ITEM_LOGIN_REQUIRED")
                        freshAccount.MarkReauthRequired();
                    else if (freshAccount.SyncStatus == "syncing")
                        freshAccount.MarkFailed(errorCode);

                    await _accounts.UpdateAsync(freshAccount, ct);
                }
            }
            catch
            {
                // Best-effort — don't mask the original exception
            }

            _logger.SyncFailed(job.CorrelationId ?? job.Id.ToString(), accountId,
                errorCode ?? "UNKNOWN", ex.Message, job.RetryCount);

            return new SyncResult(false, 0, 0, errorCode, ex.Message);
        }
    }

    private static string? ExtractErrorCode(string message)
    {
        // Common Plaid error codes that may surface in exception messages
        string[] knownCodes =
        [
            "ITEM_LOGIN_REQUIRED",
            "RATE_LIMIT_EXCEEDED",
            "INVALID_REQUEST",
            "SERVER_ERROR",
            "INTERNAL_SERVER_ERROR",
            "INVALID_CREDENTIALS",
            "PRODUCT_NOT_READY"
        ];

        foreach (var code in knownCodes)
        {
            if (message.Contains(code, StringComparison.OrdinalIgnoreCase))
                return code;
        }

        return null;
    }
}
