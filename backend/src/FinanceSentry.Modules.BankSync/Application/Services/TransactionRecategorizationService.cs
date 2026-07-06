namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Interfaces;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;
using FinanceSentry.Modules.BankSync.Infrastructure.TrueLayer;
using Microsoft.Extensions.Logging;

/// <summary>Outcome of a re-categorization pass for one user.</summary>
public record RecategorizationResult(int Examined, int ReResolved, int ReFetchedUpdated, int StillUncategorized);

/// <summary>
/// Recategorizes a user's already-stored transactions after the category-taxonomy overhaul.
/// Rows ingested before the overhaul carry no raw MCC/PFC signal, so they cannot be
/// re-resolved in place — they need a provider re-fetch. Rows ingested afterwards keep the
/// raw signal and are re-resolved for free (also picks up later MCC-map edits).
/// </summary>
public interface ITransactionRecategorizationService
{
    Task<RecategorizationResult> RecategorizeUserAsync(Guid userId, CancellationToken ct = default);
}

/// <inheritdoc />
public class TransactionRecategorizationService(
    IBankAccountRepository accounts,
    ITransactionRepository transactions,
    IEncryptedCredentialRepository credentials,
    ICredentialEncryptionService encryption,
    IPlaidAdapter plaid,
    ITransactionDeduplicationService dedup,
    IBankProviderFactory providerFactory,
    IMonobankCredentialRepository monobankCredentials,
    ITrueLayerConnectionRepository truelayerConnections,
    ITrueLayerClient truelayerClient,
    ICategoryResolver categoryResolver,
    ILogger<TransactionRecategorizationService> logger) : ITransactionRecategorizationService
{
    // Plaid /transactions/sync is paged; cap iterations so a bad cursor can't loop forever.
    private const int MaxPlaidPages = 50;

    private readonly IBankAccountRepository _accounts = accounts;
    private readonly ITransactionRepository _transactions = transactions;
    private readonly IEncryptedCredentialRepository _credentials = credentials;
    private readonly ICredentialEncryptionService _encryption = encryption;
    private readonly IPlaidAdapter _plaid = plaid;
    private readonly ITransactionDeduplicationService _dedup = dedup;
    private readonly IBankProviderFactory _providerFactory = providerFactory;
    private readonly IMonobankCredentialRepository _monobankCredentials = monobankCredentials;
    private readonly ITrueLayerConnectionRepository _truelayerConnections = truelayerConnections;
    private readonly ITrueLayerClient _truelayerClient = truelayerClient;
    private readonly ICategoryResolver _categoryResolver = categoryResolver;
    private readonly ILogger<TransactionRecategorizationService> _logger = logger;

    public async Task<RecategorizationResult> RecategorizeUserAsync(Guid userId, CancellationToken ct = default)
    {
        var userAccounts = (await _accounts.GetByUserIdAsync(userId, ct)).ToList();
        var userTransactions = (await _transactions.GetByUserIdAsync(userId, ct)).ToList();

        var reResolved = ReResolveFromStoredSignal(userTransactions);
        var reFetched = await ReFetchMissingSignalAsync(userAccounts, userTransactions, ct);

        await _transactions.SaveChangesAsync(ct);

        var stillUncategorized = userTransactions.Count(t =>
            t.MerchantCategory is null || t.MerchantCategory == CategoryKeys.Uncategorized);

        _logger.LogInformation(
            "Recategorization for user {UserId}: examined {Examined}, re-resolved {ReResolved}, re-fetched {ReFetched}, still uncategorized {Still}.",
            userId, userTransactions.Count, reResolved, reFetched, stillUncategorized);

        return new RecategorizationResult(userTransactions.Count, reResolved, reFetched, stillUncategorized);
    }

    /// <summary>Pass 1 — cheap in-process re-resolve for rows that already carry a raw signal.</summary>
    private int ReResolveFromStoredSignal(IReadOnlyList<Transaction> txns)
    {
        var updated = 0;
        foreach (var t in txns)
        {
            var resolved = ResolveFromRaw(t);
            if (resolved is not null && resolved != t.MerchantCategory)
            {
                t.MerchantCategory = resolved;
                updated++;
            }
        }
        return updated;
    }

    /// <summary>Pass 2 — provider re-fetch for rows with no stored raw signal (pre-overhaul rows).</summary>
    private async Task<int> ReFetchMissingSignalAsync(
        IReadOnlyList<BankAccount> userAccounts, IReadOnlyList<Transaction> txns, CancellationToken ct)
    {
        var missingByAccount = txns
            .Where(t => t.Mcc is null && string.IsNullOrWhiteSpace(t.SourceCategory))
            .GroupBy(t => t.AccountId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var updated = 0;
        foreach (var account in userAccounts)
        {
            if (!missingByAccount.TryGetValue(account.Id, out var rows) || rows.Count == 0)
                continue;

            IReadOnlyList<TransactionCandidate> candidates;
            try
            {
                candidates = await FetchCandidatesAsync(account, ct);
            }
            catch (Exception ex)
            {
                // Best-effort per account — one provider failing must not abort the rest.
                _logger.LogWarning(ex, "Recategorization re-fetch failed for account {AccountId} ({Provider}).",
                    account.Id, account.Provider);
                continue;
            }

            var byHash = new Dictionary<string, TransactionCandidate>();
            foreach (var c in candidates)
                byHash[_dedup.ComputeHash(c.AccountId, c.Amount, c.HashDate, c.Description)] = c;

            foreach (var t in rows)
            {
                if (!byHash.TryGetValue(t.UniqueHash, out var c))
                    continue;

                t.MerchantCategory = c.MerchantCategory;
                t.SourceCategory = c.SourceCategory;
                t.Mcc = c.Mcc;
                updated++;
            }
        }
        return updated;
    }

    private string? ResolveFromRaw(Transaction t)
    {
        if (t.Mcc.HasValue)
            return _categoryResolver.ResolveMcc(t.Mcc);
        if (!string.IsNullOrWhiteSpace(t.SourceCategory))
            return _categoryResolver.ResolvePlaidPrimary(t.SourceCategory);
        return null;
    }

    private async Task<IReadOnlyList<TransactionCandidate>> FetchCandidatesAsync(
        BankAccount account, CancellationToken ct)
    {
        return account.Provider switch
        {
            "monobank" => await FetchMonobankAsync(account, ct),
            "truelayer" => await FetchTrueLayerAsync(account, ct),
            _ => await FetchPlaidAsync(account, ct),
        };
    }

    private async Task<IReadOnlyList<TransactionCandidate>> FetchPlaidAsync(BankAccount account, CancellationToken ct)
    {
        var cred = await _credentials.GetByAccountIdAsync(account.Id, ct)
            ?? throw new InvalidOperationException($"No Plaid credential for account {account.Id}.");
        var accessToken = _encryption.Decrypt(cred.EncryptedData, cred.Iv, cred.AuthTag, cred.KeyVersion);

        // Page through the full history from a null cursor; leave the stored incremental cursor untouched.
        var all = new List<TransactionCandidate>();
        string? cursor = null;
        for (var page = 0; page < MaxPlaidPages; page++)
        {
            var (candidates, nextCursor) = await _plaid.SyncTransactionsAsync(
                accessToken, account.Id, account.UserId, cursor, ct);
            all.AddRange(candidates);
            if (candidates.Count == 0 || nextCursor == cursor)
                break;
            cursor = nextCursor;
        }
        return all;
    }

    private async Task<IReadOnlyList<TransactionCandidate>> FetchMonobankAsync(BankAccount account, CancellationToken ct)
    {
        if (account.MonobankCredentialId is null)
            throw new InvalidOperationException($"Monobank account {account.Id} has no credential id.");

        var cred = await _monobankCredentials.GetByIdAsync(account.MonobankCredentialId.Value, ct)
            ?? throw new InvalidOperationException($"Monobank credential {account.MonobankCredentialId} not found.");
        var token = _encryption.Decrypt(cred.EncryptedToken, cred.Iv, cred.AuthTag, cred.KeyVersion);

        // since:null → the adapter's 90-day windowed import (Monobank caps windows at 31 days).
        var (candidates, _) = await _providerFactory.Resolve("monobank")
            .SyncTransactionsAsync(token, account.ExternalAccountId, account.Id, account.UserId, null, ct);
        return candidates;
    }

    private async Task<IReadOnlyList<TransactionCandidate>> FetchTrueLayerAsync(BankAccount account, CancellationToken ct)
    {
        if (account.TrueLayerConnectionId is null)
            throw new InvalidOperationException($"TrueLayer account {account.Id} has no connection id.");

        var connection = await _truelayerConnections.GetByIdAsync(account.TrueLayerConnectionId.Value, ct)
            ?? throw new InvalidOperationException($"TrueLayer connection {account.TrueLayerConnectionId} not found.");
        var refreshToken = _encryption.Decrypt(
            connection.EncryptedRefreshToken, connection.Iv, connection.AuthTag, connection.KeyVersion);

        var tokenSet = await _truelayerClient.RefreshAccessTokenAsync(refreshToken, ct);

        // TrueLayer rotates refresh tokens — persist the new one so later syncs keep working.
        if (!string.IsNullOrEmpty(tokenSet.RefreshToken) && tokenSet.RefreshToken != refreshToken)
        {
            var encrypted = _encryption.Encrypt(tokenSet.RefreshToken);
            connection.SetRefreshToken(encrypted.Ciphertext, encrypted.Iv, encrypted.AuthTag);
            await _truelayerConnections.UpdateAsync(connection, ct);
        }

        var (candidates, _) = await _providerFactory.Resolve("truelayer")
            .SyncTransactionsAsync(tokenSet.AccessToken, account.ExternalAccountId, account.Id, account.UserId, null, ct);
        return candidates;
    }
}
