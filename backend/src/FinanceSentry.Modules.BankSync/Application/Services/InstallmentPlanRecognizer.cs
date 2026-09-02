namespace FinanceSentry.Modules.BankSync.Application.Services;

/// <summary>
/// Recognises Monobank installment (розстрочка) repayments and derives the key of the plan a
/// repayment belongs to.
/// <para>
/// Lives in the Application layer rather than in <c>SubscriptionDetectionJob</c> because two
/// callers need it: the detection job (to route installments to their own detector and to key
/// the plans it stores) and <see cref="CommitmentKeyResolver"/> (to decide whether an outflow is
/// committed). One definition means the key a plan is stored under and the key a transaction is
/// matched by cannot drift apart.
/// </para>
/// </summary>
public static class InstallmentPlanRecognizer
{
    // Installment (розстрочка) repayments look exactly like a monthly subscription
    // — fixed amount, monthly, repeated — but they are a fixed-term repayment, not a
    // recurring service. Monobank labels them distinctively (verified against live
    // data): "Погашення наступного платежу RozetkaPay", "Щомісячний платіж telemart
    // - monomarket", "Платіж Pandora", "Повне погашення RozetkaPay", etc.
    private const int InstallmentMcc = 4829;

    private static readonly string[] DescriptionMarkers =
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
    private static readonly string[] MerchantPrefixes =
    [
        "погашення наступного платежу",
        "повне погашення",
        "щомісячний платіж",
        "платіж",
    ];

    public static bool IsInstallmentDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return false;

        var lowered = description.ToLowerInvariant();
        foreach (var marker in DescriptionMarkers)
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
    public static string ExtractMerchant(string description)
    {
        var text = description.Trim();

        foreach (var prefix in MerchantPrefixes)
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

    /// <summary>
    /// Plan-identity amount: rounded to the whole unit, half away from zero, so cent-level
    /// jitter (telemart bills ₴6,499.84 and ₴6,499.85) stays one plan. Must round the same
    /// way as the M004 data migration's SQL <c>round()</c>.
    /// </summary>
    public static int RoundPlanAmount(decimal amount) =>
        (int)Math.Round(amount, MidpointRounding.AwayFromZero);

    /// <summary>
    /// The key an installment plan is stored under on
    /// <c>DetectedSubscription.MerchantNameNormalized</c>. One plan per (merchant, rounded
    /// monthly amount) — the same shop can carry several concurrent розстрочки.
    /// </summary>
    public static string PlanKey(string merchant, decimal amount) =>
        $"installment:{merchant.ToLowerInvariant()}:{RoundPlanAmount(amount)}";

    /// <summary>
    /// The plan key a single repayment belongs to, derived from the transaction alone, or
    /// <c>null</c> when the transaction is not a plan repayment the detector would have keyed.
    /// A full payoff returns <c>null</c>: the detector never stores a plan under a payoff's
    /// own amount — it uses payoffs only to mark a plan completed.
    /// </summary>
    public static string? PlanKeyForTransaction(string? description, decimal amount, int? mcc)
    {
        if (!IsInstallmentTransaction(description, mcc)) return null;
        if (IsInstallmentPayoff(description)) return null;

        var merchant = ExtractMerchant(description ?? string.Empty);
        return string.IsNullOrWhiteSpace(merchant) ? null : PlanKey(merchant, amount);
    }
}
