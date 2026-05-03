namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Modules.BankSync.Domain.Repositories;

/// <summary>
/// Monthly cash-flow statistics for a user (inflow / outflow / net per currency).
/// </summary>
public record MonthlyFlow(
    string Month,       // "2026-03"
    string Currency,
    decimal Inflow,
    decimal Outflow,
    decimal Net);

/// <summary>
/// Computes monthly money-flow statistics using an in-memory join of transactions and accounts.
/// </summary>
public interface IMoneyFlowStatisticsService
{
    /// <summary>
    /// Returns monthly inflow/outflow/net per currency for the last <paramref name="months"/> calendar months.
    /// Only posted (non-pending), active transactions are included.
    /// </summary>
    Task<IReadOnlyList<MonthlyFlow>> GetMonthlyFlowAsync(
        Guid userId, int months = 6, CancellationToken ct = default);
}

/// <inheritdoc />
public class MoneyFlowStatisticsService(
    ITransactionRepository transactions,
    IBankAccountRepository accounts) : IMoneyFlowStatisticsService
{
    private readonly ITransactionRepository _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
    private readonly IBankAccountRepository _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));

    /// <inheritdoc />
    public async Task<IReadOnlyList<MonthlyFlow>> GetMonthlyFlowAsync(
          Guid userId, int months = 6, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddMonths(-months);

        // 1. Build currency map from active accounts
        var accountList = await _accounts.GetByUserIdAsync(userId, ct);
        var accountCurrencies = accountList
            .Where(a => a.IsActive)
            .ToDictionary(a => a.Id, a => a.Currency);

        // 2. Fetch transactions in window
        var txList = await _transactions.GetByUserIdSinceAsync(userId, since, ct);

        // 3. Group by (currency, year-month) and sum inflow/outflow
        var result = txList
            .Where(t => !t.IsPending && t.IsActive)
            .Select(t => new
            {
                Transaction = t,
                Currency = accountCurrencies.TryGetValue(t.AccountId, out var cur) ? cur : "UNKNOWN",
                EffectiveDate = t.PostedDate ?? t.TransactionDate
            })
            .GroupBy(x => new { x.Currency, Month = x.EffectiveDate.ToString("yyyy-MM") })
            .Select(g =>
            {
                var inflow = g.Where(x => x.Transaction.TransactionType == "credit").Sum(x => x.Transaction.Amount);
                var outflow = g.Where(x => x.Transaction.TransactionType == "debit").Sum(x => x.Transaction.Amount);
                return new MonthlyFlow(g.Key.Month, g.Key.Currency, inflow, outflow, inflow - outflow);
            })
            .OrderBy(mf => mf.Month)
            .ToList();

        return result;
    }
}
