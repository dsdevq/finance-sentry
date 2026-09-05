namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Core.Domain;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

/// <summary>
/// Monthly cash-flow statistics for a user (inflow / outflow / net per currency).
/// Amounts are provided both in the source currency and normalized to USD so
/// consumers can render a single cross-currency total per month.
/// <para>
/// <see cref="OutflowUsd"/> is partitioned into <see cref="CommittedOutflowUsd"/> and
/// <see cref="DiscretionaryOutflowUsd"/>, which always sum back to it. Only the USD figures
/// are split: the split exists to be aggregated across currencies, and a native
/// committed total has no meaning once rows from a UAH and a EUR account sit side by side.
/// </para>
/// </summary>
public record MonthlyFlow(
    string Month,       // "2026-03"
    string Currency,
    decimal Inflow,
    decimal Outflow,
    decimal Net,
    decimal InflowUsd,
    decimal OutflowUsd,
    decimal NetUsd,
    decimal CommittedOutflowUsd,
    decimal DiscretionaryOutflowUsd);

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
    /// Pending transactions count — a card hold is real spending, and excluding it made the
    /// current month's outflow a fraction of reality. Settlement retires or flips the pending
    /// row in place, so it is never double-counted for long.
    /// <para>
    /// <b>Committed vs discretionary match rule.</b> An outflow is COMMITTED when the key
    /// derived from it by <see cref="CommitmentKeyResolver.Resolve"/> is the key of one of the
    /// user's detected commitments whose status is <c>active</c> — the same key the detector
    /// grouped the charge under, so the two sides cannot drift apart. That covers both kinds of
    /// commitment the detector stores: recurring services, keyed by normalized merchant name,
    /// and installment (розстрочка) plans, keyed as
    /// <c>installment:{merchant}:{roundedAmount}</c>. Every other non-transfer outflow is
    /// DISCRETIONARY. Transfers are excluded from both, exactly as they are from
    /// <c>Outflow</c> — so a mortgage repayment made as a transfer to a masked card is in no
    /// bucket and in no denominator.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<MonthlyFlow>> GetMonthlyFlowAsync(
        Guid userId, int months = 6, CancellationToken ct = default);
}

/// <inheritdoc />
public class MoneyFlowStatisticsService(
    ITransactionRepository transactions,
    IBankAccountRepository accounts,
    ITransferDetectionService transferDetection,
    IActiveSubscriptionsReader activeSubscriptions) : IMoneyFlowStatisticsService
{
    private readonly ITransactionRepository _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
    private readonly IBankAccountRepository _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    private readonly ITransferDetectionService _transferDetection = transferDetection ?? throw new ArgumentNullException(nameof(transferDetection));
    private readonly IActiveSubscriptionsReader _activeSubscriptions = activeSubscriptions ?? throw new ArgumentNullException(nameof(activeSubscriptions));

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

        // 4. Merchant keys of the user's active commitments — the committed/discretionary
        // classifier. Read once for the whole window; the detector's status is point-in-time,
        // so a subscription cancelled today reclassifies its past charges too.
        var committedMerchantKeys = await _activeSubscriptions.GetActiveCommitmentMerchantKeysAsync(userId, ct);

        // 5. Group by (currency, year-month) and sum inflow/outflow (pending included —
        // a hold is committed money; see interface doc)
        var result = txList
            .Where(t => t.IsActive && !transferIds.Contains(t.Id)
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
                var debits = g.Where(x => x.Transaction.TransactionType == "debit").ToList();
                var inflow = g.Where(x => x.Transaction.TransactionType == "credit").Sum(x => x.Transaction.Amount);
                var outflow = debits.Sum(x => x.Transaction.Amount);
                var committed = debits
                    .Where(x => committedMerchantKeys.Contains(
                        CommitmentKeyResolver.Resolve(
                            x.Transaction.MerchantName, x.Transaction.Description,
                            x.Transaction.Amount, x.Transaction.Mcc)))
                    .Sum(x => x.Transaction.Amount);

                var inflowUsd = CurrencyConverter.ToUsd(inflow, g.Key.Currency);
                var outflowUsd = CurrencyConverter.ToUsd(outflow, g.Key.Currency);
                // Convert committed only, then subtract: converting both subsets independently
                // lets rounding split them apart from OutflowUsd.
                var committedUsd = CurrencyConverter.ToUsd(committed, g.Key.Currency);

                return new MonthlyFlow(
                    g.Key.Month,
                    g.Key.Currency,
                    inflow,
                    outflow,
                    inflow - outflow,
                    inflowUsd,
                    outflowUsd,
                    inflowUsd - outflowUsd,
                    committedUsd,
                    outflowUsd - committedUsd);
            })
            .OrderByDescending(mf => mf.Month)
            .ToList();

        return result;
    }
}
