namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Infrastructure.Logging;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Interfaces;
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
/// create job → decrypt token → fetch from provider → deduplicate → persist → update account state.
/// Supports both Plaid (cursor-based) and Monobank (timestamp-based) providers.
/// </summary>
public interface IScheduledSyncService
{
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
    IBankSyncLogger logger,
    IBankProviderFactory providerFactory,
    IMonobankCredentialRepository monobankCredentials,
    IAlertGeneratorService alerts,
    IUserAlertPreferencesReader userPreferences) : IScheduledSyncService
{
    private readonly IBankAccountRepository _accounts = accounts;
    private readonly ITransactionRepository _transactions = transactions;
    private readonly ISyncJobRepository _syncJobs = syncJobs;
    private readonly IEncryptedCredentialRepository _credentials = credentials;
    private readonly ICredentialEncryptionService _encryption = encryption;
    private readonly IPlaidAdapter _plaid = plaid;
    private readonly ITransactionDeduplicationService _dedup = dedup;
    private readonly IBankSyncLogger _logger = logger;
    private readonly IBankProviderFactory _providerFactory = providerFactory;
    private readonly IMonobankCredentialRepository _monobankCredentials = monobankCredentials;
    private readonly IAlertGeneratorService _alerts = alerts;
    private readonly IUserAlertPreferencesReader _userPreferences = userPreferences;

    /// <inheritdoc />
    public async Task<SyncResult> PerformFullSyncAsync(
          Guid accountId,
          bool webhookTriggered = false,
          CancellationToken ct = default)
    {
        var startedAt = DateTime.UtcNow;

        var account = await _accounts.GetByIdAsync(accountId, ct);
        if (account == null)
            return new SyncResult(false, 0, 0, "ACCOUNT_NOT_FOUND", "Account not found.");

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
            account.BeginSync();
            await _accounts.UpdateAsync(account, ct);

            SyncResult result;

            if (account.Provider == "monobank")
                result = await SyncMonobankAsync(account, job, startedAt, ct);
            else
                result = await SyncPlaidAsync(account, job, webhookTriggered, startedAt, ct);

            await EvaluateAlertsAfterSuccessAsync(account, ct);

            return result;
        }
        catch (Exception ex)
        {
            var errorCode = ExtractErrorCode(ex.Message, account.Provider);

            job.MarkFailed(ex.Message, errorCode);
            await _syncJobs.UpdateAsync(job, ct);

            try
            {
                var freshAccount = await _accounts.GetByIdAsync(accountId, ct);
                if (freshAccount != null)
                {
                    if (errorCode is "ITEM_LOGIN_REQUIRED" or "MONOBANK_TOKEN_INVALID")
                        freshAccount.MarkReauthRequired();
                    else if (freshAccount.SyncStatus == "syncing")
                        freshAccount.MarkFailed(errorCode);

                    await _accounts.UpdateAsync(freshAccount, ct);
                }
            }
            catch
            {
                // best-effort
            }

            _logger.SyncFailed(job.CorrelationId ?? job.Id.ToString(), accountId,
                errorCode ?? "UNKNOWN", ex.Message, job.RetryCount);

            await EvaluateSyncFailureAlertAsync(account, errorCode, ct);

            return new SyncResult(false, 0, 0, errorCode, ex.Message);
        }
    }

    private async Task EvaluateAlertsAfterSuccessAsync(Domain.BankAccount account, CancellationToken ct)
    {
        try
        {
            var prefs = await _userPreferences.GetAsync(account.UserId, ct);
            if (prefs is null) return;

            if (prefs.SyncFailureAlerts)
                await _alerts.ResolveSyncFailureAlertAsync(account.UserId, account.Provider, account.Id, ct);

            if (prefs.LowBalanceAlerts && account.CurrentBalance.HasValue)
            {
                var balance = account.CurrentBalance.Value;
                if (balance < prefs.LowBalanceThreshold)
                {
                    await _alerts.GenerateLowBalanceAlertAsync(
                        account.UserId, account.Id, account.BankName,
                        balance, prefs.LowBalanceThreshold, ct);
                }
                else
                {
                    await _alerts.ResolveLowBalanceAlertAsync(account.UserId, account.Id, ct);
                }
            }
        }
        catch
        {
            // best-effort: alert generation failure must not break sync
        }
    }

    private async Task EvaluateSyncFailureAlertAsync(Domain.BankAccount account, string? errorCode, CancellationToken ct)
    {
        try
        {
            var prefs = await _userPreferences.GetAsync(account.UserId, ct);
            if (prefs is null || !prefs.SyncFailureAlerts) return;

            await _alerts.GenerateSyncFailureAlertAsync(
                account.UserId, account.Provider, account.Id, account.BankName, errorCode, ct);
        }
        catch
        {
            // best-effort
        }
    }

    private async Task<SyncResult> SyncPlaidAsync(
        Domain.BankAccount account, SyncJob job, bool webhookTriggered, DateTime startedAt, CancellationToken ct)
    {
        var cred = await _credentials.GetByAccountIdAsync(account.Id, ct)
            ?? throw new InvalidOperationException($"No Plaid credential found for account {account.Id}.");

        var accessToken = _encryption.Decrypt(cred.EncryptedData, cred.Iv, cred.AuthTag, cred.KeyVersion);
        _logger.CredentialAccessed(job.CorrelationId ?? job.Id.ToString(), account.Id);

        var (candidates, nextCursor) = await _plaid.SyncTransactionsAsync(
            accessToken, account.Id, account.UserId, cred.PlaidSyncCursor, ct);

        var existing = (await _transactions.GetByAccountIdAsync(account.Id, ct))
            .Select(t => t.UniqueHash)
            .ToHashSet();

        var newCandidates = _dedup.FilterDuplicates(candidates, existing);
        var entities = newCandidates.Select(_dedup.ToEntity).ToList();

        if (entities.Count > 0)
            await _transactions.AddRangeAsync(entities, ct);

        cred.PlaidSyncCursor = nextCursor;
        cred.UpdateLastUsedAt();
        await _credentials.UpdateAsync(cred, ct);

        var plaidAccounts = await _plaid.GetAccountsWithBalanceAsync(accessToken, ct);
        var balance = plaidAccounts.FirstOrDefault()?.CurrentBalance ?? 0m;

        account.MarkActive(balance);
        await _accounts.UpdateAsync(account, ct);

        var lastTxDate = entities.Count > 0
            ? entities.Max(t => t.PostedDate ?? t.TransactionDate)
            : (DateTime?)null;

        job.MarkSuccess(candidates.Count, entities.Count, lastTxDate);
        await _syncJobs.UpdateAsync(job, ct);

        var durationMs = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds;
        _logger.SyncCompleted(job.CorrelationId ?? job.Id.ToString(), account.Id,
            candidates.Count, entities.Count, durationMs);

        return new SyncResult(true, candidates.Count, entities.Count, null, null);
    }

    private async Task<SyncResult> SyncMonobankAsync(
        Domain.BankAccount account, SyncJob job, DateTime startedAt, CancellationToken ct)
    {
        if (account.MonobankCredentialId is null)
            throw new InvalidOperationException($"Monobank account {account.Id} has no credential id.");

        var cred = await _monobankCredentials.GetByIdAsync(account.MonobankCredentialId.Value, ct)
            ?? throw new InvalidOperationException($"Monobank credential {account.MonobankCredentialId} not found.");

        var plainToken = _encryption.Decrypt(cred.EncryptedToken, cred.Iv, cred.AuthTag, cred.KeyVersion);
        _logger.CredentialAccessed(job.CorrelationId ?? job.Id.ToString(), account.Id);

        var provider = _providerFactory.Resolve("monobank");

        var since = cred.LastSyncAt;
        var (candidates, _) = await provider.SyncTransactionsAsync(
            plainToken, account.ExternalAccountId, account.Id, account.UserId, since, ct);

        var existing = (await _transactions.GetByAccountIdAsync(account.Id, ct))
            .Select(t => t.UniqueHash)
            .ToHashSet();

        var newCandidates = _dedup.FilterDuplicates(candidates, existing);
        var entities = newCandidates.Select(_dedup.ToEntity).ToList();

        if (entities.Count > 0)
            await _transactions.AddRangeAsync(entities, ct);

        // T031: update last sync timestamp on credential
        cred.LastSyncAt = DateTime.UtcNow;
        await _monobankCredentials.UpdateAsync(cred, ct);

        account.MarkActive(0m);
        await _accounts.UpdateAsync(account, ct);

        var lastTxDate = entities.Count > 0
            ? entities.Max(t => t.PostedDate ?? t.TransactionDate)
            : (DateTime?)null;

        job.MarkSuccess(candidates.Count, entities.Count, lastTxDate);
        await _syncJobs.UpdateAsync(job, ct);

        var durationMs = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds;
        _logger.SyncCompleted(job.CorrelationId ?? job.Id.ToString(), account.Id,
            candidates.Count, entities.Count, durationMs);

        return new SyncResult(true, candidates.Count, entities.Count, null, null);
    }

    private static string? ExtractErrorCode(string message, string provider)
    {
        if (provider == "monobank")
        {
            if (message.Contains("MONOBANK_TOKEN_INVALID", StringComparison.OrdinalIgnoreCase))
                return "MONOBANK_TOKEN_INVALID";
            if (message.Contains("MONOBANK_RATE_LIMITED", StringComparison.OrdinalIgnoreCase))
                return "MONOBANK_RATE_LIMITED";
            return null;
        }

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
