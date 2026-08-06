namespace FinanceSentry.Modules.Wealth.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.Wealth.Application.Queries;
using FinanceSentry.Modules.Wealth.Domain.Services;

public class WealthAggregationService(
    IBankingAccountsReader bankingAccounts,
    IBankingTransactionReader bankingTransactions,
    ICryptoHoldingsReader? cryptoReader = null,
    IBrokerageHoldingsReader? brokerageReader = null) : IWealthAggregationService
{
    private static readonly HashSet<string> AllowedCategories =
        new(StringComparer.OrdinalIgnoreCase) { "banking", "crypto", "brokerage", "other" };

    private static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(1);

    private static readonly IReadOnlyDictionary<string, int> SyncStatusPriority =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["reauth_required"] = 0,
            ["failed"] = 1,
            ["stale"] = 2,
            ["pending"] = 3,
            ["syncing"] = 4,
            ["synced"] = 5,
            ["active"] = 5,
        };

    private readonly IBankingAccountsReader _bankingAccounts = bankingAccounts ?? throw new ArgumentNullException(nameof(bankingAccounts));
    private readonly IBankingTransactionReader _bankingTransactions = bankingTransactions ?? throw new ArgumentNullException(nameof(bankingTransactions));
    private readonly ICryptoHoldingsReader? _cryptoReader = cryptoReader;
    private readonly IBrokerageHoldingsReader? _brokerageReader = brokerageReader;

    public async Task<WealthSummaryResponse> GetWealthSummaryAsync(
        Guid userId, string? category, string? provider, CancellationToken ct = default)
    {
        if (category is not null && !AllowedCategories.Contains(category))
            throw new ArgumentException($"Invalid category '{category}'.", nameof(category));

        var bankAccounts = await _bankingAccounts.GetAccountSummariesAsync(userId, ct);

        IEnumerable<BankingAccountSummary> filtered = bankAccounts;

        if (provider is not null)
            filtered = filtered.Where(a => string.Equals(a.Provider, provider, StringComparison.OrdinalIgnoreCase));
        else if (category is not null)
            filtered = filtered.Where(a => ProviderCategoryMapper.GetCategory(a.Provider) == category);

        var grouped = filtered
            .GroupBy(a => ProviderCategoryMapper.GetCategory(a.Provider))
            .Select(g =>
            {
                var summaries = g.ToList();
                var institutions = BuildBankingInstitutions(summaries);
                return new CategorySummaryDto(
                    g.Key,
                    institutions.Sum(i => i.TotalInBaseCurrency),
                    institutions.Count,
                    institutions);
            })
            .ToList();

        if (_cryptoReader is not null && (category is null || category == "crypto") && (provider is null || provider == "binance"))
        {
            var holdings = await _cryptoReader.GetHoldingsAsync(userId, ct);
            if (holdings.Count > 0)
            {
                var accounts = holdings.Select(h => new AccountBalanceDto(
                    Guid.Empty, "Binance", "crypto", h.Asset, "binance", "crypto",
                    h.Asset, h.FreeQuantity + h.LockedQuantity, h.UsdValue, "synced", h.SyncedAt))
                    .ToList<AccountBalanceDto>();

                var institutionsByProvider = holdings
                    .GroupBy(h => h.Provider, StringComparer.OrdinalIgnoreCase)
                    .Select(pg => new InstitutionDto(
                        InstitutionId: userId,
                        Provider: pg.Key.ToLowerInvariant(),
                        Name: DisplayNameFor(pg.Key),
                        Category: "crypto",
                        TotalInBaseCurrency: pg.Sum(h => h.UsdValue),
                        SyncStatus: "synced",
                        LastSyncTimestamp: pg.Max(h => (DateTime?)h.SyncedAt),
                        LastSuccessfulSyncTimestamp: pg.Max(h => (DateTime?)h.SyncedAt),
                        Accounts: accounts.Where(a => string.Equals(a.Provider, pg.Key, StringComparison.OrdinalIgnoreCase)).ToList()))
                    .ToList();

                grouped.Add(new CategorySummaryDto("crypto", institutionsByProvider.Sum(i => i.TotalInBaseCurrency), institutionsByProvider.Count, institutionsByProvider));
            }
        }

        if (_brokerageReader is not null && (category is null || category == "brokerage") && (provider is null || provider == "ibkr"))
        {
            var holdings = await _brokerageReader.GetHoldingsAsync(userId, ct);
            if (holdings.Count > 0)
            {
                var accounts = holdings.Select(h => new AccountBalanceDto(
                    Guid.Empty, "IBKR", "brokerage",
                    h.Symbol.Length >= 4 ? h.Symbol[..4] : h.Symbol,
                    "ibkr", "brokerage", h.Symbol, h.Quantity, h.UsdValue,
                    DateTime.UtcNow - h.SyncedAt > StaleThreshold ? "stale" : "synced", h.SyncedAt))
                    .ToList<AccountBalanceDto>();

                var institutionsByProvider = holdings
                    .GroupBy(h => h.Provider, StringComparer.OrdinalIgnoreCase)
                    .Select(pg => new InstitutionDto(
                        InstitutionId: userId,
                        Provider: pg.Key.ToLowerInvariant(),
                        Name: DisplayNameFor(pg.Key),
                        Category: "brokerage",
                        TotalInBaseCurrency: pg.Sum(h => h.UsdValue),
                        SyncStatus: DateTime.UtcNow - pg.Max(h => h.SyncedAt) > StaleThreshold ? "stale" : "synced",
                        LastSyncTimestamp: pg.Max(h => (DateTime?)h.SyncedAt),
                        LastSuccessfulSyncTimestamp: pg.Max(h => (DateTime?)h.SyncedAt),
                        Accounts: accounts.Where(a => string.Equals(a.Provider, pg.Key, StringComparison.OrdinalIgnoreCase)).ToList()))
                    .ToList();

                grouped.Add(new CategorySummaryDto("brokerage", institutionsByProvider.Sum(i => i.TotalInBaseCurrency), institutionsByProvider.Count, institutionsByProvider));
            }
        }

        return new WealthSummaryResponse(
            grouped.Sum(c => c.TotalInBaseCurrency), "USD", grouped,
            new AppliedFiltersDto(category, provider));
    }

    private static IReadOnlyList<InstitutionDto> BuildBankingInstitutions(IReadOnlyList<BankingAccountSummary> summaries)
    {
        var buckets = new Dictionary<string, List<BankingAccountSummary>>();
        foreach (var s in summaries)
        {
            var key = InstitutionKeyFor(s);
            if (!buckets.TryGetValue(key, out var list))
            {
                list = [];
                buckets[key] = list;
            }
            list.Add(s);
        }

        return [.. buckets.Values.Select(list =>
        {
            var first = list[0];
            var accountDtos = list.Select(a => new AccountBalanceDto(
                a.AccountId, a.BankName, a.AccountType, a.AccountNumberLast4,
                a.Provider, ProviderCategoryMapper.GetCategory(a.Provider),
                a.Currency, a.CurrentBalance, a.BalanceUsd,
                a.SyncStatus, a.LastSyncTimestamp)).ToList();

            return new InstitutionDto(
                InstitutionId: InstitutionIdFor(first),
                Provider: first.Provider.ToLowerInvariant(),
                Name: first.BankName,
                Category: ProviderCategoryMapper.GetCategory(first.Provider),
                TotalInBaseCurrency: accountDtos.Sum(a => a.BalanceInBaseCurrency ?? 0m),
                SyncStatus: WorstSyncStatus(list.Select(a => a.SyncStatus)),
                LastSyncTimestamp: list.Max(a => a.LastSyncTimestamp),
                LastSuccessfulSyncTimestamp: list.Max(a => a.LastSuccessfulSyncTimestamp),
                Accounts: accountDtos);
        }).OrderByDescending(i => i.TotalInBaseCurrency)];
    }

    private static string InstitutionKeyFor(BankingAccountSummary s)
    {
        var scoped = s.MonobankCredentialId ?? s.TrueLayerConnectionId ?? s.AccountId;
        return $"{s.Provider.ToLowerInvariant()}::{scoped:N}";
    }

    private static Guid InstitutionIdFor(BankingAccountSummary s)
        => s.MonobankCredentialId ?? s.TrueLayerConnectionId ?? s.AccountId;

    private static string WorstSyncStatus(IEnumerable<string> statuses)
    {
        var min = statuses
            .Select(s => (Status: s, Priority: SyncStatusPriority.TryGetValue(s, out var p) ? p : int.MaxValue))
            .OrderBy(t => t.Priority)
            .First();
        return min.Status;
    }

    private static string DisplayNameFor(string provider) => provider.ToLowerInvariant() switch
    {
        "binance" => "Binance",
        "ibkr" => "Interactive Brokers",
        _ => char.ToUpperInvariant(provider[0]) + provider[1..],
    };

    public async Task<TransactionSummaryResponse> GetTransactionSummaryAsync(
        Guid userId, DateOnly from, DateOnly to,
        string? category, string? provider, CancellationToken ct = default)
    {
        if (category is not null && !AllowedCategories.Contains(category))
            throw new ArgumentException($"Invalid category '{category}'.", nameof(category));

        var txList = await _bankingTransactions.GetTransactionsAsync(userId, from, to, ct);

        IEnumerable<BankingTransactionSummary> filtered = txList.Where(t => !t.IsPending);

        if (provider is not null)
            filtered = filtered.Where(t => string.Equals(t.Provider, provider, StringComparison.OrdinalIgnoreCase));
        else if (category is not null)
            filtered = filtered.Where(t => ProviderCategoryMapper.GetCategory(t.Provider) == category);

        var byCategory = filtered
            .GroupBy(t => ProviderCategoryMapper.GetCategory(t.Provider))
            .Select(g =>
            {
                // Sum AmountUsd — accounts span currencies (UAH/EUR/…); summing native Amount
                // would add hryvnia to euros as if both were dollars.
                var debits = g.Where(t => string.Equals(t.TransactionType, "debit", StringComparison.OrdinalIgnoreCase)).Sum(t => t.AmountUsd);
                var credits = g.Where(t => string.Equals(t.TransactionType, "credit", StringComparison.OrdinalIgnoreCase)).Sum(t => t.AmountUsd);
                return new TransactionCategoryDto(g.Key, debits, credits, credits - debits, g.Count());
            })
            .ToList();

        var totalDebits = byCategory.Sum(c => c.TotalDebits);
        var totalCredits = byCategory.Sum(c => c.TotalCredits);

        return new TransactionSummaryResponse(
            from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"),
            totalDebits, totalCredits, totalCredits - totalDebits,
            byCategory, new AppliedFiltersDto(category, provider));
    }
}
