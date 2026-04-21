namespace FinanceSentry.Modules.BankSync.Infrastructure.Plaid;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain.Interfaces;

public class PlaidAdapter(IPlaidClient client) : IPlaidAdapter, IBankProvider
{
    private readonly IPlaidClient _client = client;

    public string ProviderName => "plaid";

    public async Task<LinkTokenResult> CreateLinkTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await _client.CreateLinkTokenAsync(userId.ToString(), ct);
        var expiresIn = response.Expiration - DateTime.UtcNow;
        return new LinkTokenResult(response.LinkToken, expiresIn, response.RequestId);
    }

    public async Task<ExchangeResult> ExchangePublicTokenAsync(string publicToken, CancellationToken ct = default)
    {
        var response = await _client.ExchangePublicTokenAsync(publicToken, ct);
        return new ExchangeResult(response.AccessToken, response.ItemId);
    }

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

    async Task<IReadOnlyList<BankAccountInfo>> IBankProvider.GetAccountsAsync(string credential, CancellationToken ct)
    {
        var response = await _client.GetAccountsAsync(credential, ct);
        return response.Accounts
            .Select(a => new BankAccountInfo(
                ExternalAccountId: a.AccountId,
                Name: a.Name,
                AccountType: a.Subtype ?? a.Type,
                AccountNumberLast4: a.Mask ?? "0000",
                CurrentBalance: a.CurrentBalance,
                Currency: a.CurrencyCode ?? "USD",
                OwnerName: string.Empty))
            .ToList();
    }

    async Task<(IReadOnlyList<TransactionCandidate> Candidates, DateTime? NextSyncFrom)> IBankProvider.SyncTransactionsAsync(
        string credential, string externalAccountId, Guid accountId, Guid userId,
        DateTime? since, CancellationToken ct)
    {
        // Plaid sync uses cursor-based pagination, not time-based; since is ignored here
        var response = await _client.SyncTransactionsAsync(credential, cursor: null, ct: ct);
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
        return (candidates, null);
    }

    Task IBankProvider.DisconnectAsync(string credential, CancellationToken ct)
        => _client.RevokeAccessAsync(credential, ct);

    public async Task<(IReadOnlyList<TransactionCandidate> Candidates, string NextCursor)> SyncTransactionsAsync(
        string accessToken, Guid accountId, Guid userId,
        string? cursor, CancellationToken ct = default)
    {
        var response = await _client.SyncTransactionsAsync(accessToken, cursor, ct: ct);
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

    public Task RevokeAccessAsync(string accessToken, CancellationToken ct = default)
        => _client.RevokeAccessAsync(accessToken, ct);
}
