namespace FinanceSentry.Modules.BankSync.Infrastructure.Monobank;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;
using FinanceSentry.Modules.BankSync.Domain.Interfaces;
using FinanceSentry.Modules.BankSync.Infrastructure.Categorization;

public class MonobankAdapter(MonobankHttpClient client, ICategoryResolver categoryResolver) : IMonobankAdapter, IBankProvider
{
    private readonly MonobankHttpClient _client = client;
    private readonly ICategoryResolver _categoryResolver = categoryResolver;

    /// <summary>Monobank rejects statement ranges longer than 31 days (+1h) with a 400.</summary>
    private const int MaxStatementWindowDays = 31;

    /// <summary>How far back the first-ever import of an account reaches.</summary>
    private const int InitialImportDays = 90;

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
            OwnerName: info.Name,
            ProductType: a.ProductType)).ToList();
    }

    public async Task<(IReadOnlyList<TransactionCandidate> Candidates, DateTime? NextSyncFrom)> SyncTransactionsAsync(
        string credential, string externalAccountId, Guid accountId, Guid userId,
        DateTime? since, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var candidates = new List<TransactionCandidate>();

        var start = since.HasValue
            ? new DateTimeOffset(since.Value.AddSeconds(1), TimeSpan.Zero)
            : now.AddDays(-InitialImportDays);

        // The statement endpoint rejects ranges longer than 31 days with a 400, so any
        // span — the 90-day initial import or an incremental catch-up after a long gap —
        // must be fetched as consecutive ≤31-day windows.
        for (var from = start; from < now; from = from.AddDays(MaxStatementWindowDays))
        {
            var to = from.AddDays(MaxStatementWindowDays) < now ? from.AddDays(MaxStatementWindowDays) : now;
            var txns = await _client.GetStatementsAsync(credential, externalAccountId, from, to, ct);
            candidates.AddRange(MapTransactions(txns, accountId, userId));
        }

        return (candidates, DateTime.UtcNow);
    }

    Task IBankProvider.DisconnectAsync(string credential, CancellationToken ct)
        => Task.CompletedTask;

    public async Task<IReadOnlyList<TransactionCandidate>> GetCandidatesAsync(
        string token, string externalAccountId, Guid accountId, Guid userId,
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var txns = await _client.GetStatementsAsync(token, externalAccountId, from, to, ct);
        return MapTransactions(txns, accountId, userId).ToList();
    }

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
                MerchantCategory: ResolveCategory(t),
                PlaidTransactionId: null,
                Mcc: t.MCC);
        });
    }

    // A directional-transfer description (savings-jar top-up "Поповнення «…»" / withdrawal
    // "З банки «…»") is a stronger signal than the MCC: Monobank tags jar operations with the
    // charity MCC 8398, which would otherwise land them in GOVERNMENT_AND_NON_PROFIT spend.
    // Fall back to the MCC map for everything else.
    private string ResolveCategory(MonobankTransaction t) =>
        TransferDescriptionClassifier.Resolve(t.Description) ?? _categoryResolver.ResolveMcc(t.MCC);
}
