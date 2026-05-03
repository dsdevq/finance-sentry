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
                var accountDtos = g.Select(a => new AccountBalanceDto(
                    a.AccountId, a.BankName, a.AccountType, a.AccountNumberLast4,
                    a.Provider, ProviderCategoryMapper.GetCategory(a.Provider),
                    a.Currency, a.CurrentBalance, a.BalanceUsd,
                    a.SyncStatus, a.LastSyncTimestamp)).ToList();

                return new CategorySummaryDto(g.Key, accountDtos.Sum(d => d.BalanceInBaseCurrency ?? 0m), accountDtos);
            })
            .ToList();

        if (_cryptoReader is not null && (category is null || category == "crypto") && (provider is null || provider == "binance"))
        {
            var holdings = await _cryptoReader.GetHoldingsAsync(userId, ct);
            if (holdings.Count > 0)
            {
                var dtos = holdings.Select(h => new AccountBalanceDto(
                    Guid.Empty, "Binance", "crypto", h.Asset, "binance", "crypto",
                    "USD", h.FreeQuantity + h.LockedQuantity, h.UsdValue, "synced", h.SyncedAt))
                    .ToList<AccountBalanceDto>();

                grouped.Add(new CategorySummaryDto("crypto", holdings.Sum(h => h.UsdValue), dtos));
            }
        }

        if (_brokerageReader is not null && (category is null || category == "brokerage") && (provider is null || provider == "ibkr"))
        {
            var holdings = await _brokerageReader.GetHoldingsAsync(userId, ct);
            if (holdings.Count > 0)
            {
                var dtos = holdings.Select(h => new AccountBalanceDto(
                    Guid.Empty, "IBKR", "brokerage",
                    h.Symbol.Length >= 4 ? h.Symbol[..4] : h.Symbol,
                    "ibkr", "brokerage", "USD", h.Quantity, h.UsdValue,
                    DateTime.UtcNow - h.SyncedAt > StaleThreshold ? "stale" : "synced", h.SyncedAt))
                    .ToList<AccountBalanceDto>();

                grouped.Add(new CategorySummaryDto("brokerage", holdings.Sum(h => h.UsdValue), dtos));
            }
        }

        return new WealthSummaryResponse(
            grouped.Sum(c => c.TotalInBaseCurrency), "USD", grouped,
            new AppliedFiltersDto(category, provider));
    }

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
                var debits = g.Where(t => string.Equals(t.TransactionType, "debit", StringComparison.OrdinalIgnoreCase)).Sum(t => t.Amount);
                var credits = g.Where(t => string.Equals(t.TransactionType, "credit", StringComparison.OrdinalIgnoreCase)).Sum(t => t.Amount);
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
