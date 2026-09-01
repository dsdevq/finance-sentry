namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Core.Domain;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

/// <summary>
/// Monthly cash-flow statistics for a user (inflow / outflow / net per currency).
/// Amounts are provided both in the source currency and normalized to USD so
/// consumers can render a single cross-currency total per month.
/// </summary>
public record MonthlyFlow(
    string Month,       // "2026-03"
    string Currency,
    decimal Inflow,
    decimal Outflow,
    decimal Net,
    decimal InflowUsd,
    decimal OutflowUsd,
    decimal NetUsd);

/// <summary>
/// Computes monthly money-flow statistics using an in-memory join of transactions and accounts.
/// </summary>
public interface IMoneyFlowStatisticsService
{
    /// <summary>
    /// Returns monthly inflow/outflow/net per currency over the last <paramref name="months"/>
    /// COMPLETE calendar months plus the one in progress. The window starts on a month
    /// boundary (see <see cref="MonthWindow"/>) so no bucket is a partial fragment of a month;
    /// the trailing in-progress bucket is included so callers can render a month-to-date
    /// figure, and it is up to them to keep it out of month-over-month comparisons.
    /// Only posted (non-pending) transactions are counted — the dashboard's definition of
    /// spending is "posted, active, excluding internal transfers and transfer-category".
    /// </summary>
    Task<IReadOnlyList<MonthlyFlow>> GetMonthlyFlowAsync(
        Guid userId, int months = 6, CancellationToken ct = default);
}

/// <inheritdoc />
public class MoneyFlowStatisticsService(
    ITransactionRepository transactions,
    IBankAccountRepository accounts,
    ITransferDetectionService transferDetection) : IMoneyFlowStatisticsService
{
    private readonly ITransactionRepository _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
    private readonly IBankAccountRepository _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    private readonly ITransferDetectionService _transferDetection = transferDetection ?? throw new ArgumentNullException(nameof(transferDetection));

    /// <inheritdoc />
    public async Task<IReadOnlyList<MonthlyFlow>> GetMonthlyFlowAsync(
          Guid userId, int months = 6, CancellationToken ct = default)
    {
        // Floor to the first day of the month so the oldest bucket is a WHOLE month.
        // A raw UtcNow.AddMonths(-months) starts mid-month, leaving the leading bucket
        // holding only a few days of transactions — it rendered as a near-zero bar and
        // produced a savings rate computed from a single day.
        var since = MonthWindow.StartOfMonthsAgo(months);

        // 1. Build currency map from active accounts
        var accountList = await _accounts.GetByUserIdAsync(userId, ct);
        var accountCurrencies = accountList
            .Where(a => a.IsActive)
            .ToDictionary(a => a.Id, a => a.Currency);

        // 2. Fetch transactions in window
        var txList = await _transactions.GetByUserIdSinceAsync(userId, since, ct);

        // 3. Detect internal transfer pairs so they don't inflate inflow / outflow.
        // Currency map enables cross-currency pairing (e.g. Revolut EUR → Monobank UAH).
        var transferIds = _transferDetection.DetectTransferTransactionIds(txList.ToList(), accountCurrencies);

        // 4. Group by (currency, year-month) and sum inflow/outflow (posted only —
        // same definition as the dashboard: active, non-pending, non-transfer).
        var result = txList
            .Where(t => t.IsActive && !t.IsPending && !transferIds.Contains(t.Id)
                        && !CategoryKeys.IsTransfer(t.MerchantCategory))
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
                var inflowUsd = CurrencyConverter.ToUsd(inflow, g.Key.Currency);
                var outflowUsd = CurrencyConverter.ToUsd(outflow, g.Key.Currency);
                return new MonthlyFlow(
                    g.Key.Month,
                    g.Key.Currency,
                    inflow,
                    outflow,
                    inflow - outflow,
                    inflowUsd,
                    outflowUsd,
                    inflowUsd - outflowUsd);
            })
            .OrderByDescending(mf => mf.Month)
            .ToList();

        return result;
    }
}
