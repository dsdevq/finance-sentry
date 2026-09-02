namespace FinanceSentry.Modules.BankSync.Infrastructure.Jobs;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class SubscriptionDetectionJob(
    BankSyncDbContext db,
    ISubscriptionDetectionResultService resultService,
    ILogger<SubscriptionDetectionJob> logger)
{
    private const int LookbackMonths = 13;
    // Three consistent monthly charges is enough signal — newer open-banking connections
    // often only have a few months of history, so requiring 4 hid real subscriptions
    // (Netcup, OpenAI, Claude) whose window only holds 3 charges.
    private const int MinOccurrences = 3;
    private const double MaxAmountCv = 0.10;
    private const int MonthlyMinDays = 28;
    private const int MonthlyMaxDays = 35;
    private const int AnnualMinDays = 351;
    private const int AnnualMaxDays = 379;
    // A price change (plan upgrade, VAT shift) moves the charge amount in one step;
    // adjacent amounts within this relative distance chain into the same cluster,
    // while a different plan (e.g. Claude Pro €22 vs Max €110) breaks the chain.
    private const double AmountClusterTolerance = 0.15;

    private static readonly string[] UnidentifiableNormalizedNames =
    [
        "unknown",
        "transfer",
        "top-up",
        "topup",
        "recharge",
        "withdrawal",
        "atm",
        "cash",
    ];

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        await ProcessAccountsAsync(ct);
    }

    public sealed record TxRow(
        Guid UserId, string? MerchantName, string? Description, decimal Amount,
        DateTime TransactionDate, string? MerchantCategory, int? Mcc, string? Currency);

    private async Task ProcessAccountsAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-LookbackMonths);

        var transactions = await db.Transactions
            .AsNoTracking()
            .Where(t => t.IsActive
                     && !t.IsPending
                     && t.Amount != 0m   // skip €0.00 auth holds / reversals that skew amount stability
                     && t.TransactionDate >= cutoff
                     && (t.TransactionType == null || t.TransactionType == "debit"))
            .Join(db.BankAccounts.Where(a => a.IsActive),
                t => t.AccountId, a => a.Id, (t, a) => new TxRow(
                    t.UserId, t.MerchantName, t.Description, t.Amount,
                    t.TransactionDate, t.MerchantCategory, t.Mcc, a.Currency))
            .ToListAsync(ct);

        foreach (var userGroup in transactions.GroupBy(t => t.UserId))
        {
            var userId = userGroup.Key.ToString();

            try
            {
                var txs = userGroup.ToList();
                var installmentTxs = txs.Where(t => InstallmentPlanRecognizer.IsInstallmentTransaction(t.Description, t.Mcc)).ToList();
                var regularTxs = txs.Where(t => !InstallmentPlanRecognizer.IsInstallmentTransaction(t.Description, t.Mcc)).ToList();

                var results = new List<DetectedSubscriptionData>();
                results.AddRange(DetectSubscriptions(regularTxs));
                results.AddRange(DetectInstallments(installmentTxs));

                await resultService.UpsertDetectedSubscriptionsAsync(userId, results, ct);
                await resultService.MarkStaleAsPotentiallyCancelledAsync(userId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Heuristic subscription detection failed for user {UserId}", userId);
            }
        }
    }

    // Recurring-service detection: group by merchant, keep only the amount cluster that
    // contains the most recent charge (so a discontinued plan's old price — e.g. Claude
    // Pro €22 next to Max €110 — can't poison the stability gates), then require a
    // consistent monthly/annual cadence, a stable amount, and at least MinOccurrences
    // charges. A recurring transfer to a masked card number is a loan/mortgage repayment,
    // not a service — it's emitted as an installment so it stays out of the spend summary.
    public static IEnumerable<DetectedSubscriptionData> DetectSubscriptions(IReadOnlyList<TxRow> transactions)
    {
        var results = new List<DetectedSubscriptionData>();
        var byMerchant = transactions.GroupBy(NormalizeForDetection);

        foreach (var merchantGroup in byMerchant)
        {
            var normalized = merchantGroup.Key;
            var sorted = LatestAmountCluster(merchantGroup).OrderBy(t => t.TransactionDate).ToList();

            if (sorted.Count < MinOccurrences) continue;
            if (IsUnidentifiableMerchant(normalized)) continue;

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

            var displayName = normalized.StartsWith("mobile top-up ", StringComparison.Ordinal)
                ? $"Mobile top-up {normalized[^4..]}"
                : MerchantNameNormalizer.GetDisplayName(sorted.Select(t => t.MerchantName ?? t.Description));
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
                Category: topCategory,
                Kind: MaskedPan.IsLikely(normalized)
                    ? SubscriptionKinds.Installment
                    : SubscriptionKinds.Subscription));
        }

        return results;
    }

    /// <summary>Merchant key for recurring-service grouping.</summary>
    public static string NormalizeForDetection(TxRow transaction) =>
        MerchantNameNormalizer.NormalizeDetectionKey(transaction.MerchantName, transaction.Description);

    /// <summary>
    /// Splits a merchant's charges into amount clusters (adjacent sorted amounts within
    /// <see cref="AmountClusterTolerance"/> chain together) and returns the cluster holding
    /// the most recent charge — the current price of whatever is still being billed.
    /// </summary>
    public static IReadOnlyList<TxRow> LatestAmountCluster(IEnumerable<TxRow> transactions)
    {
        var byAmount = transactions.OrderBy(t => t.Amount).ToList();
        if (byAmount.Count == 0) return byAmount;

        var clusters = new List<List<TxRow>> { new() { byAmount[0] } };
        for (var i = 1; i < byAmount.Count; i++)
        {
            var previous = (double)byAmount[i - 1].Amount;
            var current = (double)byAmount[i].Amount;
            if (previous > 0 && (current - previous) / previous <= AmountClusterTolerance)
                clusters[^1].Add(byAmount[i]);
            else
                clusters.Add(new List<TxRow> { byAmount[i] });
        }

        return clusters.MaxBy(c => c.Max(t => t.TransactionDate))!;
    }

    // Installment detection: one plan per (merchant, rounded monthly amount) — the same
    // shop can carry several concurrent розстрочки (e.g. two Алло plans at ₴2,339.95 and
    // ₴2,999.95) and merchant-only grouping merges them into one row with polluted stats.
    // No cadence/CV/min-count gates — a single "- monomarket" repayment is a real
    // installment. A full payoff ("Повне погашення") carries its own amount, so it's
    // matched by merchant: it completes every plan with no payments after it, while a
    // plan that keeps charging past the payoff date is a separate, still-active plan.
    public static IEnumerable<DetectedSubscriptionData> DetectInstallments(IReadOnlyList<TxRow> transactions)
    {
        var results = new List<DetectedSubscriptionData>();

        var payoffDatesByMerchant = transactions
            .Where(t => InstallmentPlanRecognizer.IsInstallmentPayoff(t.Description))
            .GroupBy(t => InstallmentPlanRecognizer.ExtractMerchant(t.Description ?? string.Empty))
            .ToDictionary(g => g.Key, g => g.Max(t => t.TransactionDate));

        var byPlan = transactions
            .Where(t => !InstallmentPlanRecognizer.IsInstallmentPayoff(t.Description))
            .GroupBy(t => (
                Merchant: InstallmentPlanRecognizer.ExtractMerchant(t.Description ?? string.Empty),
                Amount: InstallmentPlanRecognizer.RoundPlanAmount(t.Amount)));

        foreach (var group in byPlan)
        {
            var (merchant, roundedAmount) = group.Key;
            if (string.IsNullOrWhiteSpace(merchant)) continue;

            var payments = group.OrderBy(t => t.TransactionDate).ToList();
            var lastPayment = payments[^1];
            var lastPaymentDate = lastPayment.TransactionDate;

            var completed = payoffDatesByMerchant.TryGetValue(merchant, out var payoff)
                && payoff >= lastPaymentDate;

            var lastChargeDate = DateOnly.FromDateTime(lastPaymentDate);

            results.Add(new DetectedSubscriptionData(
                MerchantNameNormalized: InstallmentPlanRecognizer.PlanKey(merchant, roundedAmount),
                MerchantNameDisplay: merchant,
                Cadence: "monthly",
                AverageAmount: Math.Round(payments.Average(t => t.Amount), 2),
                LastKnownAmount: lastPayment.Amount,
                Currency: lastPayment.Currency ?? "USD",
                LastChargeDate: lastChargeDate,
                NextExpectedDate: lastChargeDate.AddMonths(1),
                OccurrenceCount: payments.Count,
                ConfidenceScore: 100,
                Category: null,
                Kind: SubscriptionKinds.Installment,
                IsCompleted: completed));
        }

        return results;
    }

    public static bool IsUnidentifiableMerchant(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized)) return true;

        // Keys produced by NormalizeForDetection's mobile top-up special case are
        // deliberately identifiable despite containing "top-up".
        if (normalized.StartsWith("mobile top-up ", StringComparison.Ordinal)) return false;

        foreach (var marker in UnidentifiableNormalizedNames)
        {
            if (normalized.Contains(marker, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    private static double Median(List<int> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }
}
