namespace FinanceSentry.Modules.BankSync.Infrastructure.TrueLayer;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;
using FinanceSentry.Modules.BankSync.Domain.Interfaces;

/// <summary>
/// Implements IBankProvider for TrueLayer. The credential argument on every method
/// is an OAuth access_token — the caller (ScheduledSyncService) is responsible for
/// exchanging the per-connection refresh_token for a fresh access_token before
/// invoking this adapter.
/// </summary>
public class TrueLayerAdapter(ITrueLayerClient client, TrueLayerCategoryMapper categoryMapper) : IBankProvider
{
    private const int InitialSyncWindowDays = 90;

    private readonly TrueLayerCategoryMapper _categoryMapper = categoryMapper;

    public string ProviderName => "truelayer";

    public async Task<IReadOnlyList<BankAccountInfo>> GetAccountsAsync(string credential, CancellationToken ct = default)
    {
        var accounts = await client.ListAccountsAsync(credential, ct);
        var result = new List<BankAccountInfo>(accounts.Count);

        foreach (var a in accounts)
        {
            decimal? currentBalance = null;
            try
            {
                var balance = await client.GetBalanceAsync(credential, a.AccountId, ct);
                currentBalance = balance?.Current;
            }
            catch (TrueLayerException)
            {
                // Balance is best-effort; don't fail account creation on a single 4xx.
            }

            result.Add(new BankAccountInfo(
                ExternalAccountId: a.AccountId,
                Name: !string.IsNullOrWhiteSpace(a.DisplayName) ? a.DisplayName : a.ProviderName,
                AccountType: a.AccountType,
                AccountNumberLast4: a.AccountNumberLast4,
                CurrentBalance: currentBalance,
                Currency: a.Currency,
                OwnerName: string.Empty));
        }

        return result;
    }

    public async Task<(IReadOnlyList<TransactionCandidate> Candidates, DateTime? NextSyncFrom)> SyncTransactionsAsync(
        string credential, string externalAccountId, Guid accountId, Guid userId,
        DateTime? since, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = since.HasValue
            ? DateOnly.FromDateTime(since.Value.Date)
            : today.AddDays(-InitialSyncWindowDays);

        var booked = await client.GetTransactionsAsync(credential, externalAccountId, from, today, ct);
        var pending = await client.GetPendingTransactionsAsync(credential, externalAccountId, ct);

        var candidates = booked
            .Concat(pending)
            .Select(t => MapTransaction(t, accountId, userId, _categoryMapper))
            .ToList();

        return (candidates, DateTime.UtcNow);
    }

    public Task DisconnectAsync(string credential, CancellationToken ct = default)
        => Task.CompletedTask;

    private static TransactionCandidate MapTransaction(TrueLayerTransaction t, Guid accountId, Guid userId, TrueLayerCategoryMapper categoryMapper)
    {
        var amount = Math.Abs(t.Amount);
        var txType = t.Amount < 0 || t.TransactionType == "debit" ? "debit" : "credit";

        return new TransactionCandidate(
            AccountId: accountId,
            UserId: userId,
            Amount: amount,
            TransactionDate: t.Timestamp,
            PostedDate: t.IsPending ? null : t.Timestamp,
            Description: t.Description,
            IsPending: t.IsPending,
            TransactionType: txType,
            MerchantName: t.MerchantName,
            MerchantCategory: categoryMapper.Map(t.Classification),
            PlaidTransactionId: null);
    }
}
