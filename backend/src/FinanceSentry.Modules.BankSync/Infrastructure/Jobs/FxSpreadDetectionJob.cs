namespace FinanceSentry.Modules.BankSync.Infrastructure.Jobs;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Daily sentinel (044/US4): fires an FxSpread alert when a user's cross-currency routing pattern
/// (e.g. EUR→UAH) appears to lose more than the configured threshold to the FX spread.
///
/// Detection strategy: for each user with accounts in 2+ currencies, match same-day outflows from
/// currency-A accounts against inflows to currency-B accounts. Pairs whose implied A→B rate falls
/// within 3× of the market rate are considered plausible conversions; their volume-weighted average
/// implied rate is compared to the CurrencyConverter market rate. No external feed is required —
/// rates come from the process-level rate table (refreshed by the FX job).
/// </summary>
public sealed class FxSpreadDetectionJob(
    BankSyncDbContext db,
    IAlertGeneratorService alerts,
    IConfiguration config,
    ILogger<FxSpreadDetectionJob> logger)
{
    private const int DefaultLookbackDays = 3;
    private const decimal DefaultSpreadThreshold = 0.03m;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var lookbackDays = config.GetValue("HygieneSentinels:FxSpreadLookbackDays", DefaultLookbackDays);
        var threshold = config.GetValue("HygieneSentinels:FxSpreadThreshold", DefaultSpreadThreshold);
        var since = DateTime.UtcNow.AddDays(-lookbackDays);

        IReadOnlyList<AccountInfo> allAccounts;
        try
        {
            allAccounts = await db.BankAccounts
                .AsNoTracking()
                .Where(a => a.IsActive)
                .Select(a => new AccountInfo(a.Id, a.UserId, a.Currency))
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FxSpreadDetectionJob: failed to read accounts");
            return;
        }

        var multiCurrencyUsers = allAccounts
            .GroupBy(a => a.UserId)
            .Where(g => g.Select(a => a.Currency).Distinct().Count() >= 2)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (multiCurrencyUsers.Count == 0) return;

        var relevantAccountIds = allAccounts
            .Where(a => multiCurrencyUsers.ContainsKey(a.UserId))
            .Select(a => a.AccountId)
            .ToList();

        IReadOnlyList<TxRow> transactions;
        try
        {
            transactions = await db.Transactions
                .AsNoTracking()
                .Where(t => relevantAccountIds.Contains(t.AccountId)
                         && t.TransactionDate >= since
                         && t.IsActive)
                .Select(t => new TxRow(t.AccountId, t.UserId, t.Amount, t.TransactionDate.Date))
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FxSpreadDetectionJob: failed to read transactions");
            return;
        }

        var txByAccount = transactions.GroupBy(t => t.AccountId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (userId, userAccounts) in multiCurrencyUsers)
        {
            var currencies = userAccounts.Select(a => a.Currency).Distinct().ToList();

            foreach (var fromCurrency in currencies)
            {
                foreach (var toCurrency in currencies.Where(c => c != fromCurrency))
                {
                    var marketRate = ComputeMarketRate(fromCurrency, toCurrency);
                    if (marketRate <= 0) continue;

                    var fromAccountIds = userAccounts
                        .Where(a => a.Currency == fromCurrency)
                        .Select(a => a.AccountId)
                        .ToHashSet();
                    var toAccountIds = userAccounts
                        .Where(a => a.Currency == toCurrency)
                        .Select(a => a.AccountId)
                        .ToHashSet();

                    // Outflows from fromCurrency accounts (negative amounts), by day
                    var outflowsByDay = fromAccountIds
                        .SelectMany(id => txByAccount.TryGetValue(id, out var txs) ? txs : [])
                        .Where(t => t.Amount < 0)
                        .GroupBy(t => t.Date)
                        .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount)));

                    // Inflows to toCurrency accounts (positive amounts), by day
                    var inflowsByDay = toAccountIds
                        .SelectMany(id => txByAccount.TryGetValue(id, out var txs) ? txs : [])
                        .Where(t => t.Amount > 0)
                        .GroupBy(t => t.Date)
                        .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

                    if (outflowsByDay.Count == 0 || inflowsByDay.Count == 0) continue;

                    // Match same-day EUR outflows to UAH inflows; filter to plausible conversions.
                    // Same-day matching avoids double-counting (a ±1-day window would cause each
                    // outflow day to grab adjacent inflow days already claimed by the neighbour).
                    var matchedPairs = new List<(decimal FromOut, decimal ToIn)>();
                    foreach (var (day, fromOut) in outflowsByDay)
                    {
                        if (!inflowsByDay.TryGetValue(day, out var toIn) || toIn <= 0) continue;

                        var implied = toIn / fromOut;
                        // Only accept pairs whose implied rate is within 3× of market — filters random co-incident flows
                        if (implied < marketRate / 3m || implied > marketRate * 3m) continue;

                        matchedPairs.Add((fromOut, toIn));
                    }

                    if (matchedPairs.Count == 0) continue;

                    var totalFrom = matchedPairs.Sum(p => p.FromOut);
                    var totalTo = matchedPairs.Sum(p => p.ToIn);
                    var avgImplied = totalTo / totalFrom;
                    var spread = (marketRate - avgImplied) / marketRate;

                    if (spread <= threshold) continue;

                    try
                    {
                        // Dedup key is (UserId, fromCurrency, toCurrency) rather than per debit-transaction-id.
                        // This is deliberate: the aggregation-based matching combines daily outflows across
                        // multiple transactions into a single implied rate — there is no single "debit transaction id"
                        // to attach. Currency-pair-level dedup prevents re-alerting while the routing pattern persists.
                        await alerts.GenerateFxSpreadAlertAsync(
                            userId, fromCurrency, toCurrency, avgImplied, marketRate, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "FxSpreadDetectionJob: alert failed for user {UserId} pair {From}→{To}",
                            userId, fromCurrency, toCurrency);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns the number of <paramref name="toCurrency"/> units per 1 <paramref name="fromCurrency"/>
    /// unit at the current market rate, or 0 when either currency is unknown.
    /// </summary>
    private static decimal ComputeMarketRate(string fromCurrency, string toCurrency)
    {
        if (!CurrencyConverter.IsKnown(fromCurrency) || !CurrencyConverter.IsKnown(toCurrency))
            return 0m;

        var fromUsd = CurrencyConverter.ToUsd(1m, fromCurrency);
        var toUsd = CurrencyConverter.ToUsd(1m, toCurrency);
        return toUsd > 0m ? fromUsd / toUsd : 0m;
    }

    private sealed record AccountInfo(Guid AccountId, Guid UserId, string Currency);
    private sealed record TxRow(Guid AccountId, Guid UserId, decimal Amount, DateTime Date);
}
