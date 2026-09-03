namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Core.Domain;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Domain;
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
    decimal NetUsd,
    decimal FamilySupportOutflowUsd = 0m,
    decimal InvestedOutflowUsd = 0m);

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
    /// Counterparty transactions (e.g. family rent / support) are netted separately and
    /// folded into each month's inflow/outflow so the savings rate is honest. The
    /// <paramref name="classification"/> is computed once per request by
    /// <see cref="ICounterpartyClassificationService.ClassifyForWindowAsync"/> and shared with
    /// the top-categories reader, so both tell the same story about the same movements.
    /// </summary>
    Task<IReadOnlyList<MonthlyFlow>> GetMonthlyFlowAsync(
        Guid userId,
        CounterpartyClassificationResult classification,
        int months = 6,
        CancellationToken ct = default);
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
          Guid userId,
          CounterpartyClassificationResult classification,
          int months = 6,
          CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(classification);

        // Floor to the first day of the month so the oldest bucket is a WHOLE month.
        var since = MonthWindow.StartOfMonthsAgo(months);

        // 1. Build currency map from active accounts
        var accountList = await _accounts.GetByUserIdAsync(userId, ct);
        var accountCurrencies = accountList
            .Where(a => a.IsActive)
            .ToDictionary(a => a.Id, a => a.Currency);

        // 2. Fetch transactions in window
        var txList = (await _transactions.GetByUserIdSinceAsync(userId, since, ct)).ToList();

        // 3. Counterparty classification (computed once upstream): identifies transactions that
        //    belong to known counterparties (e.g. family rent / support) and carries their net
        //    monthly flows. These are excluded from the normal transfer-detection pass to avoid
        //    double-exclusion.
        var matchedIds = classification.MatchedTransactionIds;

        // 4. Detect internal transfer pairs among NON-counterparty transactions.
        var nonCounterpartyTx = txList.Where(t => !matchedIds.Contains(t.Id)).ToList();
        var transferIds = _transferDetection.DetectTransferTransactionIds(nonCounterpartyTx, accountCurrencies);

        // 5. Normal flow: exclude counterparty-matched, transfer pairs, and TRANSFER category.
        var normalGroups = txList
            .Where(t => t.IsActive
                        && !matchedIds.Contains(t.Id)
                        && !transferIds.Contains(t.Id)
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
                return (Month: g.Key.Month, Currency: g.Key.Currency, InflowUsd: inflowUsd, OutflowUsd: outflowUsd, Inflow: inflow, Outflow: outflow);
            })
            .ToDictionary(x => (x.Month, x.Currency));

        // 6. Fold the counterparty flows into the USD-denominated totals, one combined entry
        //    per month (the netting is already currency-normalised).
        //
        //    The flow ROLE decides where the movement lands:
        //      family_support — real spending, so net expense joins Outflow;
        //      investment     — the money is still the user's, it only changed sleeve, so it
        //                       stays OUT of Outflow and is reported separately. Folding it
        //                       into spend would understate the savings rate by exactly the
        //                       amount that was saved.
        var cpByMonth = classification.MonthlyFlows
            .GroupBy(f => f.Month)
            .ToDictionary(
                g => g.Key,
                g => (
                    IncomeUsd: g.Where(f => f.FlowRole != FlowRoles.Investment).Sum(f => f.NetIncomeUsd),
                    ExpenseUsd: g.Where(f => f.FlowRole != FlowRoles.Investment).Sum(f => f.NetExpenseUsd),
                    FamilySupportUsd: g.Where(f => f.FlowRole == FlowRoles.FamilySupport).Sum(f => f.NetExpenseUsd),
                    InvestedUsd: g.Where(f => f.FlowRole == FlowRoles.Investment).Sum(f => f.NetExpenseUsd)));

        var result = new List<MonthlyFlow>();

        // Emit one row per (currency, month) from the normal flow.
        foreach (var kv in normalGroups)
        {
            var (month, currency) = kv.Key;
            var n = kv.Value;
            var inflowUsd = n.InflowUsd;
            var outflowUsd = n.OutflowUsd;
            var familySupportUsd = 0m;
            var investedUsd = 0m;

            // Attach counterparty adjustment to the first (often only) currency bucket for
            // this month. For most users there is one dominant currency (UAH or EUR).
            if (cpByMonth.TryGetValue(month, out var cp))
            {
                inflowUsd += cp.IncomeUsd;
                outflowUsd += cp.ExpenseUsd;
                familySupportUsd = cp.FamilySupportUsd;
                investedUsd = cp.InvestedUsd;
                cpByMonth.Remove(month); // consumed — do not double-add across currencies
            }

            result.Add(new MonthlyFlow(
                month, currency,
                n.Inflow, n.Outflow, n.Inflow - n.Outflow,
                inflowUsd, outflowUsd, inflowUsd - outflowUsd,
                familySupportUsd, investedUsd));
        }

        // Emit synthetic USD rows for months that are ONLY in the counterparty flows
        // (no normal transactions that month but counterparty flows exist).
        foreach (var kv in cpByMonth)
        {
            var month = kv.Key;
            result.Add(new MonthlyFlow(
                month, "USD",
                0m, 0m, 0m,
                kv.Value.IncomeUsd, kv.Value.ExpenseUsd, kv.Value.IncomeUsd - kv.Value.ExpenseUsd,
                kv.Value.FamilySupportUsd, kv.Value.InvestedUsd));
        }

        return result
            .OrderByDescending(mf => mf.Month)
            .ToList();
    }
}
