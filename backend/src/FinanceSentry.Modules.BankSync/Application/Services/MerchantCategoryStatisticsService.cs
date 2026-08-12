namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

/// <summary>
/// Spending breakdown per merchant category for a user.
/// </summary>
public record CategoryStat(string Category, decimal TotalSpend, decimal PercentOfTotal);

/// <summary>
/// Computes the top spending categories from debit transactions.
/// </summary>
public interface IMerchantCategoryStatisticsService
{
    /// <summary>
    /// Returns the top <paramref name="limit"/> spending categories sorted by TotalSpend DESC.
    /// Only posted (non-pending), active debit transactions are included.
    /// </summary>
    Task<IReadOnlyList<CategoryStat>> GetTopCategoriesAsync(
        Guid userId, int limit = 10, CancellationToken ct = default);
}

/// <inheritdoc />
public class MerchantCategoryStatisticsService(
    ITransactionRepository transactions,
    IBankAccountRepository accounts,
    ITransferDetectionService transferDetection) : IMerchantCategoryStatisticsService
{
    private readonly ITransactionRepository _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
    private readonly IBankAccountRepository _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    private readonly ITransferDetectionService _transferDetection = transferDetection ?? throw new ArgumentNullException(nameof(transferDetection));

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryStat>> GetTopCategoriesAsync(
          Guid userId, int limit = 10, CancellationToken ct = default)
    {
        var txList = await _transactions.GetByUserIdAsync(userId, ct);

        // Convert each transaction to USD by its account currency — accounts span UAH/EUR/…,
        // so summing native Amount would mix currencies and inflate the spend total.
        var accountList = await _accounts.GetByUserIdAsync(userId, ct);
        var currencyByAccount = accountList.ToDictionary(a => a.Id, a => a.Currency);

        var transferIds = _transferDetection.DetectTransferTransactionIds(txList.ToList(), currencyByAccount);
        decimal ToUsd(Transaction t) =>
            CurrencyConverter.ToUsd(t.Amount, currencyByAccount.TryGetValue(t.AccountId, out var c) ? c : "USD");

        var debits = txList
            .Where(t => !t.IsPending && t.IsActive && t.TransactionType == "debit"
                        && !transferIds.Contains(t.Id)
                        && !CategoryKeys.IsTransfer(t.MerchantCategory))
            .ToList();

        if (debits.Count == 0)
            return [];

        var totalSpend = debits.Sum(ToUsd);

        var result = debits
            .GroupBy(t => t.MerchantCategory ?? CategoryKeys.Uncategorized)
            .Select(g =>
            {
                var spend = g.Sum(ToUsd);
                var pct = totalSpend > 0 ? Math.Round(spend / totalSpend * 100, 2) : 0m;
                return new CategoryStat(g.Key, spend, pct);
            })
            .OrderByDescending(cs => cs.TotalSpend)
            .Take(limit)
            .ToList();

        return result;
    }
}
