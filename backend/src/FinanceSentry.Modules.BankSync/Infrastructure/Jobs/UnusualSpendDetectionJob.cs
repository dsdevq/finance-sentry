namespace FinanceSentry.Modules.BankSync.Infrastructure.Jobs;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class UnusualSpendDetectionJob(
    BankSyncDbContext db,
    IAlertGeneratorService alerts,
    ILogger<UnusualSpendDetectionJob> logger)
{
    private const int MinHistoryMonths = 3;
    private const decimal SpendMultiplierThreshold = 2m;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var historyStart = currentMonthStart.AddMonths(-MinHistoryMonths);

        var rows = await db.Transactions
            .AsNoTracking()
            .Where(t => t.MerchantCategory != null
                     && t.Amount < 0
                     && t.TransactionDate >= historyStart)
            .Select(t => new
            {
                t.UserId,
                t.AccountId,
                t.MerchantCategory,
                t.TransactionDate,
                t.Amount,
            })
            .ToListAsync(ct);

        // Convert to USD by account currency before the threshold math — a category funded from
        // multiple-currency accounts would otherwise compare mixed native magnitudes.
        var currencyByAccount = await db.BankAccounts
            .AsNoTracking()
            .Select(a => new { a.Id, a.Currency })
            .ToDictionaryAsync(a => a.Id, a => a.Currency, ct);
        decimal ToUsd(Guid accountId, decimal amount) =>
            CurrencyConverter.ToUsd(Math.Abs(amount), currencyByAccount.TryGetValue(accountId, out var c) ? c : "USD");

        var grouped = rows.GroupBy(r => new {r.UserId, Category = r.MerchantCategory!});

        foreach (var group in grouped)
        {
            var byMonth = group
                .GroupBy(r => new {r.TransactionDate.Year, r.TransactionDate.Month})
                .ToDictionary(g => g.Key, g => g.Sum(x => ToUsd(x.AccountId, x.Amount)));

            var historicMonths = byMonth
                .Where(kv => new DateTime(kv.Key.Year, kv.Key.Month, 1) < currentMonthStart)
                .ToList();

            if (historicMonths.Count < MinHistoryMonths) continue;

            var currentKey = new {currentMonthStart.Year, currentMonthStart.Month};
            if (!byMonth.TryGetValue(currentKey, out var currentMonth)) continue;

            var average = historicMonths.Sum(kv => kv.Value) / historicMonths.Count;
            if (average <= 0) continue;

            if (currentMonth > average * SpendMultiplierThreshold)
            {
                try
                {
                    await alerts.GenerateUnusualSpendAlertAsync(
                        group.Key.UserId, group.Key.Category, currentMonth, average, ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "UnusualSpend alert failed for user {UserId} category {Category}",
                        group.Key.UserId, group.Key.Category);
                }
            }
        }
    }
}
