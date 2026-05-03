namespace FinanceSentry.Modules.BankSync.Infrastructure.Jobs;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class SubscriptionDetectionJob(
    BankSyncDbContext db,
    IPlaidClient plaid,
    ICredentialEncryptionService encryption,
    ISubscriptionDetectionResultService resultService,
    ILogger<SubscriptionDetectionJob> logger)
{
    private const int LookbackMonths = 13;
    private const int MinOccurrences = 3;
    private const double MaxAmountCv = 0.20;
    private const int MonthlyMinDays = 28;
    private const int MonthlyMaxDays = 35;
    private const int AnnualMinDays = 351;
    private const int AnnualMaxDays = 379;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        await ProcessPlaidAccountsAsync(ct);
        await ProcessNonPlaidAccountsAsync(ct);
    }

    private async Task ProcessPlaidAccountsAsync(CancellationToken ct)
    {
        var plaidAccounts = await db.BankAccounts
            .AsNoTracking()
            .Where(a => a.IsActive && a.Provider == "plaid")
            .Join(db.EncryptedCredentials, a => a.Id, c => c.AccountId,
                (a, c) => new { a.UserId, a.Currency, Cred = c })
            .ToListAsync(ct);

        var byUser = plaidAccounts.GroupBy(x => x.UserId);

        foreach (var userGroup in byUser)
        {
            var userId = userGroup.Key.ToString();

            try
            {
                var results = new List<DetectedSubscriptionData>();

                foreach (var item in userGroup)
                {
                    string accessToken;
                    try
                    {
                        accessToken = encryption.Decrypt(
                            item.Cred.EncryptedData, item.Cred.Iv, item.Cred.AuthTag, item.Cred.KeyVersion);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to decrypt Plaid token for user {UserId}", userId);
                        continue;
                    }

                    PlaidRecurringResponse recurring;
                    try
                    {
                        recurring = await plaid.GetRecurringTransactionsAsync(accessToken, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Plaid recurring API call failed for user {UserId}", userId);
                        continue;
                    }

                    foreach (var stream in recurring.OutflowStreams)
                    {
                        if (stream.AverageAmount <= 0) continue;

                        var cadence = MapFrequency(stream.Frequency);
                        if (cadence is null) continue;

                        var lastDate = stream.LastDate is not null
                            ? DateOnly.Parse(stream.LastDate)
                            : DateOnly.FromDateTime(DateTime.UtcNow);

                        var nextDate = cadence == "annual"
                            ? lastDate.AddYears(1)
                            : lastDate.AddMonths(1);

                        var confidence = stream.Status == "MATURE" ? 90 : 60;

                        results.Add(new DetectedSubscriptionData(
                            MerchantNameNormalized: MerchantNameNormalizer.Normalize(stream.MerchantName ?? stream.Description),
                            MerchantNameDisplay: stream.MerchantName ?? stream.Description,
                            Cadence: cadence,
                            AverageAmount: stream.AverageAmount,
                            LastKnownAmount: stream.LastAmount,
                            Currency: stream.IsoCurrencyCode ?? item.Currency ?? "USD",
                            LastChargeDate: lastDate,
                            NextExpectedDate: nextDate,
                            OccurrenceCount: stream.TransactionIds.Count,
                            ConfidenceScore: confidence,
                            Category: stream.PersonalFinanceCategory));
                    }
                }

                await resultService.UpsertDetectedSubscriptionsAsync(userId, results, ct);
                await resultService.MarkStaleAsPotentiallyCancelledAsync(userId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Plaid subscription detection failed for user {UserId}", userId);
            }
        }
    }

    private async Task ProcessNonPlaidAccountsAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-LookbackMonths);

        var transactions = await db.Transactions
            .AsNoTracking()
            .Where(t => t.IsActive
                     && !t.IsPending
                     && t.TransactionDate >= cutoff
                     && (t.TransactionType == null || t.TransactionType == "debit"))
            .Join(db.BankAccounts.Where(a => a.IsActive && a.Provider != "plaid"),
                t => t.AccountId, a => a.Id, (t, a) => new
                {
                    t.UserId,
                    t.MerchantName,
                    t.Description,
                    t.Amount,
                    t.TransactionDate,
                    t.MerchantCategory,
                    Currency = a.Currency,
                })
            .ToListAsync(ct);

        var byUser = transactions.GroupBy(t => t.UserId);

        foreach (var userGroup in byUser)
        {
            var userId = userGroup.Key.ToString();

            try
            {
                var byMerchant = userGroup.GroupBy(t =>
                    MerchantNameNormalizer.Normalize(t.MerchantName ?? t.Description));

                var results = new List<DetectedSubscriptionData>();

                foreach (var merchantGroup in byMerchant)
                {
                    var normalized = merchantGroup.Key;
                    var sorted = merchantGroup.OrderBy(t => t.TransactionDate).ToList();

                    if (sorted.Count < MinOccurrences) continue;

                    var dates = sorted.Select(t => t.TransactionDate).ToList();
                    var intervals = new List<int>();
                    for (var i = 1; i < dates.Count; i++)
                        intervals.Add((int)(dates[i] - dates[i - 1]).TotalDays);

                    if (intervals.Count == 0) continue;

                    var median = Median(intervals);

                    string cadence;
                    if (median >= MonthlyMinDays && median <= MonthlyMaxDays)
                        cadence = "monthly";
                    else if (median >= AnnualMinDays && median <= AnnualMaxDays)
                        cadence = "annual";
                    else
                        continue;

                    var amounts = sorted.Select(t => (double)t.Amount).ToList();
                    var mean = amounts.Average();
                    if (mean <= 0) continue;

                    var stddev = Math.Sqrt(amounts.Sum(a => Math.Pow(a - mean, 2)) / amounts.Count);
                    var cv = stddev / mean;
                    if (cv > MaxAmountCv) continue;

                    var lastTransaction = sorted.Last();
                    var lastChargeDate = DateOnly.FromDateTime(lastTransaction.TransactionDate);
                    var nextExpectedDate = lastChargeDate.AddDays((int)median);

                    var displayName = MerchantNameNormalizer.GetDisplayName(sorted.Select(t => t.MerchantName));
                    var topCategory = sorted
                        .Select(t => t.MerchantCategory)
                        .Where(c => c != null)
                        .GroupBy(c => c)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key;

                    results.Add(new DetectedSubscriptionData(
                        MerchantNameNormalized: normalized,
                        MerchantNameDisplay: displayName,
                        Cadence: cadence,
                        AverageAmount: (decimal)mean,
                        LastKnownAmount: lastTransaction.Amount,
                        Currency: lastTransaction.Currency ?? "USD",
                        LastChargeDate: lastChargeDate,
                        NextExpectedDate: nextExpectedDate,
                        OccurrenceCount: sorted.Count,
                        ConfidenceScore: sorted.Count,
                        Category: topCategory));
                }

                await resultService.UpsertDetectedSubscriptionsAsync(userId, results, ct);
                await resultService.MarkStaleAsPotentiallyCancelledAsync(userId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Heuristic subscription detection failed for user {UserId}", userId);
            }
        }
    }

    private static string? MapFrequency(string frequency) => frequency switch
    {
        "WEEKLY" => "monthly",
        "BIWEEKLY" => "monthly",
        "SEMI_MONTHLY" => "monthly",
        "MONTHLY" => "monthly",
        "ANNUALLY" => "annual",
        _ => null
    };

    private static double Median(List<int> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }
}
