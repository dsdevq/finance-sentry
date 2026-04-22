namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.BankSync.Application.Queries;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Domain.Services;

public class WealthAggregationService(
    IBankAccountRepository accounts,
    ITransactionRepository transactions,
    ICryptoHoldingsReader? cryptoHoldingsReader = null) : IWealthAggregationService
{
    private static readonly HashSet<string> AllowedCategories =
        new(StringComparer.OrdinalIgnoreCase) { "banking", "crypto", "brokerage", "other" };

    private readonly IBankAccountRepository _accounts = accounts;
    private readonly ITransactionRepository _transactions = transactions;
    private readonly ICryptoHoldingsReader? _cryptoHoldingsReader = cryptoHoldingsReader;

    public async Task<WealthSummaryResponse> GetWealthSummaryAsync(
        Guid userId, string? category, string? provider, CancellationToken ct = default)
    {
        if (category is not null && !AllowedCategories.Contains(category))
            throw new ArgumentException($"Invalid category '{category}'.", nameof(category));

        var all = await _accounts.GetByUserIdAsync(userId, ct);

        IEnumerable<BankAccount> filtered = all;

        if (provider is not null)
            filtered = filtered.Where(a => string.Equals(a.Provider, provider, StringComparison.OrdinalIgnoreCase));
        else if (category is not null)
            filtered = filtered.Where(a => ProviderCategoryMapper.Map(a.Provider) == category);

        var grouped = filtered
            .GroupBy(a => ProviderCategoryMapper.Map(a.Provider))
            .Select(g =>
            {
                var accountDtos = g.Select(a => BuildAccountDto(a)).ToList();
                var total = accountDtos.Sum(d => d.BalanceInBaseCurrency ?? 0m);
                return new CategorySummaryDto(g.Key, total, accountDtos);
            })
            .ToList();

        if (_cryptoHoldingsReader is not null &&
            (category is null || category == "crypto") &&
            (provider is null || provider == "binance"))
        {
            var cryptoHoldings = await _cryptoHoldingsReader.GetHoldingsAsync(userId, ct);
            if (cryptoHoldings.Count > 0)
            {
                var cryptoAccountDtos = cryptoHoldings
                    .Select(h => new AccountBalanceDto(
                        Guid.Empty,
                        "Binance",
                        "crypto",
                        h.Asset,
                        h.Provider,
                        "crypto",
                        "USD",
                        h.FreeQuantity + h.LockedQuantity,
                        h.UsdValue,
                        "synced"))
                    .ToList<AccountBalanceDto>();

                var cryptoTotal = cryptoHoldings.Sum(h => h.UsdValue);
                grouped.Add(new CategorySummaryDto("crypto", cryptoTotal, cryptoAccountDtos));
            }
        }

        var totalNetWorth = grouped.Sum(c => c.TotalInBaseCurrency);

        return new WealthSummaryResponse(
            totalNetWorth,
            "USD",
            grouped,
            new AppliedFiltersDto(category, provider));
    }

    public async Task<TransactionSummaryResponse> GetTransactionSummaryAsync(
        Guid userId, DateOnly from, DateOnly to,
        string? category, string? provider, CancellationToken ct = default)
    {
        if (category is not null && !AllowedCategories.Contains(category))
            throw new ArgumentException($"Invalid category '{category}'.", nameof(category));

        var accountList = await _accounts.GetByUserIdAsync(userId, ct);
        var accountMap = accountList.ToDictionary(a => a.Id);

        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var txList = await _transactions.GetByUserIdAsync(userId, ct);

        IEnumerable<Transaction> filtered = txList
            .Where(t => t.IsActive && !t.IsPending)
            .Where(t =>
            {
                var date = t.PostedDate ?? t.TransactionDate;
                return date >= fromUtc && date <= toUtc;
            });

        if (provider is not null)
            filtered = filtered.Where(t =>
                accountMap.TryGetValue(t.AccountId, out var acc)
                && string.Equals(acc.Provider, provider, StringComparison.OrdinalIgnoreCase));
        else if (category is not null)
            filtered = filtered.Where(t =>
                accountMap.TryGetValue(t.AccountId, out var acc)
                && ProviderCategoryMapper.Map(acc.Provider) == category);

        var txByCategory = filtered
            .GroupBy(t =>
            {
                accountMap.TryGetValue(t.AccountId, out var acc);
                return ProviderCategoryMapper.Map(acc?.Provider);
            })
            .Select(g =>
            {
                var debits = g.Where(t => string.Equals(t.TransactionType, "debit", StringComparison.OrdinalIgnoreCase))
                              .Sum(t => t.Amount);
                var credits = g.Where(t => string.Equals(t.TransactionType, "credit", StringComparison.OrdinalIgnoreCase))
                               .Sum(t => t.Amount);
                return new TransactionCategoryDto(g.Key, debits, credits, credits - debits, g.Count());
            })
            .ToList();

        var totalDebits = txByCategory.Sum(c => c.TotalDebits);
        var totalCredits = txByCategory.Sum(c => c.TotalCredits);

        return new TransactionSummaryResponse(
            from.ToString("yyyy-MM-dd"),
            to.ToString("yyyy-MM-dd"),
            totalDebits,
            totalCredits,
            totalCredits - totalDebits,
            txByCategory,
            new AppliedFiltersDto(category, provider));
    }

    private static AccountBalanceDto BuildAccountDto(BankAccount a)
    {
        var cat = ProviderCategoryMapper.Map(a.Provider);
        decimal? usd = a.CurrentBalance.HasValue
            ? CurrencyConverter.ToUsd(a.CurrentBalance.Value, a.Currency)
            : null;

        return new AccountBalanceDto(
            a.Id,
            a.BankName,
            a.AccountType,
            a.AccountNumberLast4,
            a.Provider,
            cat,
            a.Currency,
            a.CurrentBalance,
            usd,
            a.SyncStatus);
    }
}
