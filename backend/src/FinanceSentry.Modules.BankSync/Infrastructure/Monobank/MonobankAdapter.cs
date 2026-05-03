namespace FinanceSentry.Modules.BankSync.Infrastructure.Monobank;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;
using FinanceSentry.Modules.BankSync.Domain.Interfaces;

public class MonobankAdapter(MonobankHttpClient client, MonobankCategoryMapper categoryMapper) : IMonobankAdapter, IBankProvider
{
    private readonly MonobankHttpClient _client = client;
    private readonly MonobankCategoryMapper _categoryMapper = categoryMapper;

    public string ProviderName => "monobank";

    public async Task<IReadOnlyList<MonobankAccountInfo>> ConnectAsync(
        string token, CancellationToken ct = default)
    {
        var info = await _client.GetClientInfoAsync(token, ct);
        return info.Accounts;
    }

    public async Task<IReadOnlyList<MonobankAccountInfo>> GetAccountsAsync(
        string token, CancellationToken ct = default)
    {
        var info = await _client.GetClientInfoAsync(token, ct);
        return info.Accounts;
    }

    public Task<IReadOnlyList<MonobankTransaction>> GetStatementsAsync(
        string token, string accountId, DateTimeOffset from, DateTimeOffset to,
        CancellationToken ct = default)
        => _client.GetStatementsAsync(token, accountId, from, to, ct);

    public Task SetWebhookAsync(string token, string url, CancellationToken ct = default)
        => _client.SetWebhookAsync(token, url, ct);

    // ── IBankProvider ─────────────────────────────────────────────────────────

    async Task<IReadOnlyList<BankAccountInfo>> IBankProvider.GetAccountsAsync(
        string credential, CancellationToken ct)
    {
        var info = await _client.GetClientInfoAsync(credential, ct);
        return info.Accounts.Select(a => new BankAccountInfo(
            ExternalAccountId: a.Id,
            Name: a.Name,
            AccountType: a.Type,
            AccountNumberLast4: a.MaskedPan.Length >= 4
                ? a.MaskedPan[^4..] : a.MaskedPan.PadLeft(4, '0'),
            CurrentBalance: MonobankHttpClient.KopecksToDecimal(a.Balance),
            Currency: MonobankHttpClient.MapCurrency(a.CurrencyCode),
            OwnerName: info.Name)).ToList();
    }

    async Task<(IReadOnlyList<TransactionCandidate> Candidates, DateTime? NextSyncFrom)> IBankProvider.SyncTransactionsAsync(
        string credential, string externalAccountId, Guid accountId, Guid userId,
        DateTime? since, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var candidates = new List<TransactionCandidate>();

        if (since.HasValue)
        {
            var from = new DateTimeOffset(since.Value.AddSeconds(1), TimeSpan.Zero);
            var txns = await _client.GetStatementsAsync(credential, externalAccountId, from, now, ct);
            candidates.AddRange(MapTransactions(txns, accountId, userId));
        }
        else
        {
            // 90-day initial import: 3 × 31-day windows
            var windows = new[]
            {
                (now.AddDays(-90), now.AddDays(-59)),
                (now.AddDays(-59), now.AddDays(-28)),
                (now.AddDays(-28), now)
            };

            foreach (var (from, to) in windows)
            {
                var txns = await _client.GetStatementsAsync(credential, externalAccountId, from, to, ct);
                candidates.AddRange(MapTransactions(txns, accountId, userId));
            }
        }

        return (candidates, DateTime.UtcNow);
    }

    Task IBankProvider.DisconnectAsync(string credential, CancellationToken ct)
        => Task.CompletedTask;

    private IEnumerable<TransactionCandidate> MapTransactions(
        IReadOnlyList<MonobankTransaction> txns, Guid accountId, Guid userId)
    {
        return txns.Select(t =>
        {
            var amount = MonobankHttpClient.KopecksToDecimal(Math.Abs(t.Amount));
            var txType = t.Amount < 0 ? "debit" : "credit";
            var txDate = DateTimeOffset.FromUnixTimeSeconds(t.Time).UtcDateTime;
            return new TransactionCandidate(
                AccountId: accountId,
                UserId: userId,
                Amount: amount,
                TransactionDate: txDate,
                PostedDate: txDate,
                Description: t.Description,
                IsPending: t.Hold,
                TransactionType: txType,
                MerchantName: t.CounterName,
                MerchantCategory: _categoryMapper.Map(t.MCC.ToString()),
                PlaidTransactionId: null);
        });
    }
}
