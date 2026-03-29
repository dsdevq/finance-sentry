namespace FinanceSentry.Modules.BankSync.Application.Services;

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
public class MerchantCategoryStatisticsService : IMerchantCategoryStatisticsService
{
    private readonly ITransactionRepository _transactions;

    public MerchantCategoryStatisticsService(ITransactionRepository transactions)
    {
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryStat>> GetTopCategoriesAsync(
        Guid userId, int limit = 10, CancellationToken ct = default)
    {
        var txList = await _transactions.GetByUserIdAsync(userId, ct);

        var debits = txList
            .Where(t => !t.IsPending && t.IsActive && t.TransactionType == "debit")
            .ToList();

        if (debits.Count == 0)
            return [];

        var totalSpend = debits.Sum(t => t.Amount);

        var result = debits
            .GroupBy(t => t.MerchantCategory ?? "Uncategorized")
            .Select(g =>
            {
                var spend = g.Sum(t => t.Amount);
                var pct = totalSpend > 0 ? Math.Round(spend / totalSpend * 100, 2) : 0m;
                return new CategoryStat(g.Key, spend, pct);
            })
            .OrderByDescending(cs => cs.TotalSpend)
            .Take(limit)
            .ToList();

        return result;
    }
}
