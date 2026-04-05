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
    /// Returns transaction candidates for a given account + user, ready for deduplication.
    /// Handles pending/posted distinction per TransactionDeduplicationService contract.
    /// </summary>
    public async Task<IReadOnlyList<TransactionCandidate>> GetTransactionsAsync(
        string accessToken, Guid accountId, Guid userId,
        DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var response = await _client.GetTransactionsAsync(accessToken, startDate, endDate, 0, 500, ct);
        return response.Transactions
            .Select(t => new TransactionCandidate(
                AccountId: accountId,
                UserId: userId,
                Amount: Math.Abs(t.Amount), // Plaid uses negative for debits
                TransactionDate: t.AuthorizedDate ?? t.Date,
                PostedDate: t.Pending ? null : t.Date,
                Description: t.Name,
                IsPending: t.Pending,
                TransactionType: t.Amount < 0 ? "debit" : "credit",
                MerchantName: t.MerchantName,
                MerchantCategory: t.PersonalFinanceCategory,
                PlaidTransactionId: t.TransactionId))
            .ToList();
    }

    /// <summary>Revokes access — called when user disconnects their bank account.</summary>
    public Task RevokeAccessAsync(string accessToken, CancellationToken ct = default)
        => _client.RevokeAccessAsync(accessToken, ct);
}
