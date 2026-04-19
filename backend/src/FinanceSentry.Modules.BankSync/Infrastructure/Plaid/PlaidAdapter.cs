namespace FinanceSentry.Modules.BankSync.Infrastructure.Plaid;

using FinanceSentry.Modules.BankSync.Application.Services;

/// <summary>
/// Domain adapter for Plaid API (T204).
/// Translates raw Plaid HTTP responses (via IPlaidClient) into domain models.
/// All Plaid-specific types stay in this layer — domain entities never import Plaid DTOs.
/// </summary>
public class PlaidAdapter(IPlaidClient client) : IPlaidAdapter
{
    private readonly IPlaidClient _client = client;

    /// <summary>Creates a Plaid Link token for the frontend (step 1 of account connection).</summary>
    public async Task<LinkTokenResult> CreateLinkTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await _client.CreateLinkTokenAsync(userId.ToString(), ct);
        var expiresIn = response.Expiration - DateTime.UtcNow;
        return new LinkTokenResult(response.LinkToken, expiresIn, response.RequestId);
    }

    /// <summary>Exchanges short-lived public token for long-lived access token (step 2).</summary>
    public async Task<ExchangeResult> ExchangePublicTokenAsync(string publicToken, CancellationToken ct = default)
    {
        var response = await _client.ExchangePublicTokenAsync(publicToken, ct);
        return new ExchangeResult(response.AccessToken, response.ItemId);
    }

    /// <summary>Returns domain-mapped accounts with current balances.</summary>
    public async Task<IReadOnlyList<PlaidAccountInfo>> GetAccountsWithBalanceAsync(
        string accessToken, CancellationToken ct = default)
    {
        var response = await _client.GetAccountsAsync(accessToken, ct);
        return response.Accounts
            .Select(a => new PlaidAccountInfo(
                PlaidAccountId: a.AccountId,
                Name: a.Name,
                AccountType: a.Subtype ?? a.Type,
                AccountNumberLast4: a.Mask,
                CurrentBalance: a.CurrentBalance,
                AvailableBalance: a.AvailableBalance,
                Currency: a.CurrencyCode))
            .ToList();
    }

    /// <summary>
    /// Fetches incremental transaction changes via /transactions/sync.
    /// Plaid amount sign: positive = outflow (debit), negative = inflow (credit).
    /// Returns mapped candidates plus the next cursor to persist for future incremental syncs.
    /// </summary>
    public async Task<(IReadOnlyList<TransactionCandidate> Candidates, string NextCursor)> SyncTransactionsAsync(
        string accessToken, Guid accountId, Guid userId,
        string? cursor, CancellationToken ct = default)
    {
        var response = await _client.SyncTransactionsAsync(accessToken, cursor, ct: ct);

        // added + modified both map to upsert candidates; removed are handled by dedup (ignored if not persisted)
        var candidates = response.Added.Concat(response.Modified)
            .Select(t => new TransactionCandidate(
                AccountId: accountId,
                UserId: userId,
                Amount: Math.Abs(t.Amount),
                TransactionDate: t.AuthorizedDate ?? t.Date,
                PostedDate: t.Pending ? null : t.Date,
                Description: t.Name,
                IsPending: t.Pending,
                TransactionType: t.Amount > 0 ? "debit" : "credit",
                MerchantName: t.MerchantName,
                MerchantCategory: t.PersonalFinanceCategory,
                PlaidTransactionId: t.TransactionId))
            .ToList();

        return (candidates, response.NextCursor);
    }

    /// <summary>Revokes access — called when user disconnects their bank account.</summary>
    public Task RevokeAccessAsync(string accessToken, CancellationToken ct = default)
        => _client.RevokeAccessAsync(accessToken, ct);
}
