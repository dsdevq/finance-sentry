namespace FinanceSentry.Modules.BankSync.Application.Services;

using System.Collections.Concurrent;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Infrastructure.Logging;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Interfaces;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Monobank;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;
using FinanceSentry.Modules.BankSync.Infrastructure.TrueLayer;

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
    ITrueLayerConnectionRepository truelayerConnections,
    ITrueLayerClient truelayerClient,
    MonobankBalanceCache monobankBalanceCache,
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
    private readonly ITrueLayerConnectionRepository _truelayerConnections = truelayerConnections;
    private readonly ITrueLayerClient _truelayerClient = truelayerClient;
    private readonly MonobankBalanceCache _monobankBalanceCache = monobankBalanceCache;
    private readonly IAlertGeneratorService _alerts = alerts;
    private readonly IUserAlertPreferencesReader _userPreferences = userPreferences;

    /// <summary>
    /// Error codes that represent a transient, self-healing condition (provider rate-limit /
    /// 429). These must not mark the account failed or fire a SyncFailure alert — the next
    /// scheduled cycle retries and clears the state on its own.
    /// </summary>
    private static readonly HashSet<string> TransientErrorCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "MONOBANK_RATE_LIMITED",
        "RATE_LIMIT_EXCEEDED",
    };

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
            else if (account.Provider == "truelayer")
                result = await SyncTrueLayerAsync(account, job, startedAt, ct);
            else
                result = await SyncPlaidAsync(account, job, webhookTriggered, startedAt, ct);

            await EvaluateAlertsAfterSuccessAsync(account, ct);

            return result;
        }
        catch (Exception ex)
        {
            var errorCode = ExtractErrorCode(ex.Message, account.Provider);
            var isTransient = errorCode is not null && TransientErrorCodes.Contains(errorCode);

            job.MarkFailed(ex.Message, errorCode);
            await _syncJobs.UpdateAsync(job, ct);

            try
            {
                var freshAccount = await _accounts.GetByIdAsync(accountId, ct);
                if (freshAccount != null)
                {
                    if (errorCode is "ITEM_LOGIN_REQUIRED" or "MONOBANK_TOKEN_INVALID")
                        freshAccount.MarkReauthRequired();
                    else if (isTransient && freshAccount.SyncStatus == "syncing")
                        freshAccount.MarkTransientRetry();
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

            // Transient throttling (provider 429) is self-healing — the next scheduled
            // cycle retries. Surfacing a "reconnect your credentials" alert here would be
            // a false alarm, so we skip it. A genuine failure still alerts the user.
            if (!isTransient)
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

        var entities = await PersistAndReconcileAsync(account.Id, candidates, ct);

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

        var entities = await PersistAndReconcileAsync(account.Id, candidates, ct);

        // T031: update last sync timestamp on credential
        cred.LastSyncAt = DateTime.UtcNow;
        await _monobankCredentials.UpdateAsync(cred, ct);

        // Refresh live balance from Monobank /personal/client-info. The endpoint
        // is rate-limited to one call per 60s per token, so we check the shared
        // MonobankBalanceCache first (primed by BulkSyncMonobank) and only call the
        // API when the cache is cold. On rate-limit / transient failure we keep
        // the prior balance rather than zeroing it.
        decimal? latestBalance = _monobankBalanceCache.TryGet(plainToken, account.ExternalAccountId);
        if (latestBalance is null)
        {
            try
            {
                var freshAccounts = await provider.GetAccountsAsync(plainToken, ct);
                foreach (var fa in freshAccounts)
                {
                    if (fa.CurrentBalance.HasValue)
                        _monobankBalanceCache.Set(plainToken, fa.ExternalAccountId, fa.CurrentBalance.Value);
                }
                latestBalance = freshAccounts.FirstOrDefault(a => a.ExternalAccountId == account.ExternalAccountId)?.CurrentBalance;
            }
            catch (Infrastructure.Monobank.MonobankException)
            {
                // Rate limit or transient — leave the prior balance in place.
            }
        }

        account.MarkActive(latestBalance ?? account.CurrentBalance ?? 0m);
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

    // Serializes the refresh-token exchange per TrueLayer connection. A connection can back several
    // accounts, each with its own scheduled sync job firing on the same cron; without this gate two
    // jobs could refresh the shared, rotating refresh_token concurrently — one wins, the other gets
    // invalid_grant and the rotated token is lost, bricking the connection.
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> TrueLayerRefreshLocks = new();

    private async Task<SyncResult> SyncTrueLayerAsync(
        Domain.BankAccount account, SyncJob job, DateTime startedAt, CancellationToken ct)
    {
        if (account.TrueLayerConnectionId is null)
            throw new InvalidOperationException($"TrueLayer account {account.Id} has no connection id.");

        var connectionId = account.TrueLayerConnectionId.Value;
        var accessToken = await AcquireTrueLayerAccessTokenAsync(connectionId, job, account.Id, ct);

        var connection = await _truelayerConnections.GetByIdAsync(connectionId, ct)
            ?? throw new InvalidOperationException($"TrueLayer connection {connectionId} not found.");

        var provider = _providerFactory.Resolve("truelayer");
        var since = connection.LastSyncAt;

        var (candidates, _) = await provider.SyncTransactionsAsync(
            accessToken, account.ExternalAccountId, account.Id, account.UserId, since, ct);

        var entities = await PersistAndReconcileAsync(account.Id, candidates, ct);

        connection.LastSyncAt = DateTime.UtcNow;
        await _truelayerConnections.UpdateAsync(connection, ct);

        // Refresh the live balance — TrueLayer balances can move independently of
        // dedup'd transactions (e.g. an overdraft on AIB), so re-query rather than
        // hardcoding zero.
        decimal latestBalance = 0m;
        try
        {
            var bal = await _truelayerClient.GetBalanceAsync(accessToken, account.ExternalAccountId, ct);
            if (bal is not null)
                latestBalance = bal.Current;
        }
        catch (Infrastructure.TrueLayer.TrueLayerException)
        {
            // Best-effort — fall back to 0 if the balance endpoint trips.
        }

        account.MarkActive(latestBalance);
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

    /// <summary>
    /// Exchanges a connection's rotating refresh_token for a fresh access_token, serialized per
    /// connection. The rotated refresh_token is persisted <em>immediately</em> — before any transaction
    /// fetch — so a later sync failure cannot strand a consumed token and permanently brick the
    /// connection (the invalid_grant root cause). The connection is re-read inside the lock so parallel
    /// per-account jobs always refresh from the latest persisted token.
    /// </summary>
    private async Task<string> AcquireTrueLayerAccessTokenAsync(
        Guid connectionId, SyncJob job, Guid accountId, CancellationToken ct)
    {
        var gate = TrueLayerRefreshLocks.GetOrAdd(connectionId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            var connection = await _truelayerConnections.GetByIdAsync(connectionId, ct)
                ?? throw new InvalidOperationException($"TrueLayer connection {connectionId} not found.");

            var refreshToken = _encryption.Decrypt(
                connection.EncryptedRefreshToken, connection.Iv, connection.AuthTag, connection.KeyVersion);
            _logger.CredentialAccessed(job.CorrelationId ?? job.Id.ToString(), accountId);

            var tokenSet = await _truelayerClient.RefreshAccessTokenAsync(refreshToken, ct);

            // Persist the rotated refresh_token now — not after the sync — so nothing downstream can lose it.
            if (!string.IsNullOrEmpty(tokenSet.RefreshToken) && tokenSet.RefreshToken != refreshToken)
            {
                var encrypted = _encryption.Encrypt(tokenSet.RefreshToken);
                connection.SetRefreshToken(encrypted.Ciphertext, encrypted.Iv, encrypted.AuthTag);
                await _truelayerConnections.UpdateAsync(connection, ct);
            }

            return tokenSet.AccessToken;
        }
        finally
        {
            gate.Release();
        }
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

        if (provider == "truelayer")
        {
            // Expired/revoked consent is a re-consent condition, not a transient failure. Map it to the
            // canonical reauth code so the account is flagged for reconnection and the scheduler stops
            // retrying it every cycle (instead of logging errorCode=UNKNOWN forever).
            if (message.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase)
                || message.Contains("consent has expired", StringComparison.OrdinalIgnoreCase)
                || message.Contains("has been revoked", StringComparison.OrdinalIgnoreCase))
                return "ITEM_LOGIN_REQUIRED";
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

    /// <summary>
    /// Persists newly-synced candidates (deduplicated) and then retires any existing pending
    /// row that now has a settled/posted twin, so pending transactions don't linger as stale
    /// duplicates once they clear. See <see cref="PendingReconciler"/>.
    /// </summary>
    private async Task<IReadOnlyList<Domain.Transaction>> PersistAndReconcileAsync(
        Guid accountId, IEnumerable<TransactionCandidate> candidates, CancellationToken ct)
    {
        var existingRows = (await _transactions.GetByAccountIdAsync(accountId, ct)).ToList();
        var existingHashes = existingRows.Select(t => t.UniqueHash).ToHashSet();

        var entities = _dedup.FilterDuplicates(candidates, existingHashes)
            .Select(_dedup.ToEntity)
            .ToList();
        if (entities.Count > 0)
            await _transactions.AddRangeAsync(entities, ct);

        var stale = PendingReconciler.SelectStalePending(existingRows, entities);
        if (stale.Count > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var t in stale)
            {
                t.IsActive = false;
                t.DeletedAt = now;
            }
            await _transactions.SaveChangesAsync(ct);
        }

        return entities;
    }
}
