namespace FinanceSentry.Modules.BankSync.Infrastructure.Jobs;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Daily sentinel (044/US2): fires a DuplicateCharge alert when the same merchant charges the same
/// amount more than once within the configured window on the same account. Reads transactions directly
/// from BankSyncDbContext — no external feed needed.
/// </summary>
public sealed class DuplicateChargeDetectionJob(
    BankSyncDbContext db,
    IAlertGeneratorService alerts,
    IConfiguration config,
    ILogger<DuplicateChargeDetectionJob> logger)
{
    private const int DefaultDuplicateWindowDays = 5;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var windowDays = config.GetValue("HygieneSentinels:DuplicateWindowDays", DefaultDuplicateWindowDays);
        var since = DateTime.UtcNow.AddDays(-windowDays);

        IReadOnlyList<TransactionRow> rows;
        Dictionary<Guid, string> currencyByAccount;
        try
        {
            var rawRows = await db.Transactions
                .AsNoTracking()
                .Where(t => t.MerchantName != null
                         && t.TransactionDate >= since
                         && t.IsActive
                         && !t.IsPending)
                .Select(t => new TransactionRow(t.UserId, t.AccountId, t.MerchantName!, t.Amount))
                .ToListAsync(ct);

            rows = rawRows;

            currencyByAccount = await db.BankAccounts
                .AsNoTracking()
                .Select(a => new { a.Id, a.Currency })
                .ToDictionaryAsync(a => a.Id, a => a.Currency, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DuplicateChargeDetectionJob: failed to read transactions");
            return;
        }

        var groups = rows
            .GroupBy(r => (r.AccountId, r.MerchantName, Amount: Math.Round(Math.Abs(r.Amount), 2)));

        foreach (var group in groups)
        {
            if (group.Count() < 2) continue;

            var (accountId, merchantName, amount) = group.Key;
            var userId = group.First().UserId;
            var currency = currencyByAccount.TryGetValue(accountId, out var c) ? c : "USD";

            try
            {
                await alerts.GenerateDuplicateChargeAlertAsync(
                    userId, accountId, merchantName, amount, currency, group.Count(), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "DuplicateChargeDetectionJob: alert failed for account {AccountId} merchant {Merchant}",
                    accountId, merchantName);
            }
        }
    }

    private sealed record TransactionRow(Guid UserId, Guid AccountId, string MerchantName, decimal Amount);
}
