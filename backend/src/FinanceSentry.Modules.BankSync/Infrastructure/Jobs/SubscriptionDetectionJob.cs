namespace FinanceSentry.Modules.BankSync.Infrastructure.Jobs;

using System.Text.RegularExpressions;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
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

    // Installment (розстрочка) repayments look exactly like a monthly subscription
    // — fixed amount, monthly, repeated — but they are a fixed-term repayment, not a
    // recurring service. Monobank labels them distinctively (verified against live
    // data): "Погашення наступного платежу RozetkaPay", "Щомісячний платіж telemart
    // - monomarket", "Платіж Pandora", "Повне погашення RozetkaPay", etc.
    private const int InstallmentMcc = 4829;

    private static readonly string[] InstallmentDescriptionMarkers =
    [
        "погашення наступного платежу",   // "repayment of the next payment"
        "повне погашення",                // "full early payoff" — a finished-signal
        "щомісячний платіж",              // "monthly payment"
        "monomarket",                     // Monobank installment marketplace tag
        "розстроч",                       // розстрочка / у розстрочку
        "оплата частинами",
        "покупка частинами",
        "частинами",
        "installment",
    ];

    // Prefixes stripped to recover the merchant from an installment description.
    private static readonly string[] InstallmentMerchantPrefixes =
    [
        "погашення наступного платежу",
        "повне погашення",
        "щомісячний платіж",
        "платіж",
    ];

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

    public sealed record TxRow(
        Guid UserId, string? MerchantName, string? Description, decimal Amount,
        DateTime TransactionDate, string? MerchantCategory, int? Mcc, string? Currency);

    private async Task ProcessNonPlaidAccountsAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-LookbackMonths);

        var transactions = await db.Transactions
            .AsNoTracking()
            .Where(t => t.IsActive
                     && !t.IsPending
                     && t.Amount != 0m   // skip €0.00 auth holds / reversals that skew amount stability
                     && t.TransactionDate >= cutoff
                     && (t.TransactionType == null || t.TransactionType == "debit"))
            .Join(db.BankAccounts.Where(a => a.IsActive && a.Provider != "plaid"),
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
                var installmentTxs = txs.Where(t => IsInstallmentTransaction(t.Description, t.Mcc)).ToList();
                var regularTxs = txs.Where(t => !IsInstallmentTransaction(t.Description, t.Mcc)).ToList();

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

    /// <summary>
    /// Merchant key for recurring-service grouping. Mobile top-ups carry the phone number
    /// in the description ("*MOBI TOP-UP 0857860057"), which both fragments the merchant
    /// key and trips the generic top-up blocklist — collapse them to a stable per-number
    /// key instead so a monthly top-up is tracked like any other recurring cost.
    /// </summary>
    public static string NormalizeForDetection(TxRow transaction)
    {
        var raw = transaction.MerchantName ?? transaction.Description;
        if (raw is not null)
        {
            var mobi = MobiTopUpPattern.Match(raw.Trim());
            if (mobi.Success)
            {
                var number = mobi.Groups[1].Value;
                return $"mobile top-up {number[^4..]}";
            }
        }

        return MerchantNameNormalizer.Normalize(raw);
    }

    private static readonly Regex MobiTopUpPattern =
        new(@"^\*?\s*mobi\s+top-?up\s+(\d{4,})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
            .Where(t => IsInstallmentPayoff(t.Description))
            .GroupBy(t => ExtractInstallmentMerchant(t.Description ?? string.Empty))
            .ToDictionary(g => g.Key, g => g.Max(t => t.TransactionDate));

        var byPlan = transactions
            .Where(t => !IsInstallmentPayoff(t.Description))
            .GroupBy(t => (
                Merchant: ExtractInstallmentMerchant(t.Description ?? string.Empty),
                Amount: RoundPlanAmount(t.Amount)));

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
                MerchantNameNormalized: $"installment:{merchant.ToLowerInvariant()}:{roundedAmount}",
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

    /// <summary>
    /// Plan-identity amount: rounded to the whole unit, half away from zero, so cent-level
    /// jitter (telemart bills ₴6,499.84 and ₴6,499.85) stays one plan. Must round the same
    /// way as the M004 data migration's SQL <c>round()</c>.
    /// </summary>
    public static int RoundPlanAmount(decimal amount) =>
        (int)Math.Round(amount, MidpointRounding.AwayFromZero);

    public static bool IsInstallmentDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return false;

        var lowered = description.ToLowerInvariant();
        foreach (var marker in InstallmentDescriptionMarkers)
        {
            if (lowered.Contains(marker, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    /// <summary>
    /// True for a Monobank installment repayment: a strong description marker, or a
    /// "Платіж &lt;merchant&gt;" on the internal-transfer MCC (catches merchant payoffs
    /// like "Платіж Pandora" that carry no розстрочка keyword).
    /// </summary>
    public static bool IsInstallmentTransaction(string? description, int? mcc)
    {
        if (IsInstallmentDescription(description)) return true;
        if (mcc == InstallmentMcc && description is not null &&
            description.Trim().StartsWith("Платіж ", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return false;
    }

    public static bool IsInstallmentPayoff(string? description) =>
        description is not null &&
        description.Contains("повне погашення", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Recovers the merchant from an installment description by stripping Monobank's
    /// leading phrase ("Погашення наступного платежу", "Щомісячний платіж", "Платіж", …)
    /// and the trailing "- monomarket" marketplace tag.
    /// </summary>
    public static string ExtractInstallmentMerchant(string description)
    {
        var text = description.Trim();

        foreach (var prefix in InstallmentMerchantPrefixes)
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                text = text[prefix.Length..].Trim();
                break;
            }
        }

        var tagIdx = text.IndexOf("monomarket", StringComparison.OrdinalIgnoreCase);
        if (tagIdx >= 0)
            text = text[..tagIdx];

        return text.Trim(' ', '-', '\t');
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
