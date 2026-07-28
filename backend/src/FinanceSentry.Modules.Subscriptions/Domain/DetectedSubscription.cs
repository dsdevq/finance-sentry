namespace FinanceSentry.Modules.Subscriptions.Domain;

using FinanceSentry.Core.Interfaces;

public class DetectedSubscription
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UserId { get; private set; } = string.Empty;
    public string MerchantNameNormalized { get; private set; } = string.Empty;
    public string MerchantNameDisplay { get; private set; } = string.Empty;
    public string Cadence { get; private set; } = string.Empty;
    public decimal AverageAmount { get; private set; }
    public decimal LastKnownAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    /// <summary>"subscription" (open-ended service) or "installment" (fixed-term розстрочка).</summary>
    public string Kind { get; private set; } = SubscriptionKinds.Subscription;
    public DateOnly LastChargeDate { get; private set; }
    public DateOnly NextExpectedDate { get; private set; }
    public string Status { get; private set; } = SubscriptionStatus.Active;
    public int OccurrenceCount { get; private set; }
    public int ConfidenceScore { get; private set; }
    public string? Category { get; private set; }
    /// <summary>Installment plan length in payments, if known (user-set). Enables remaining/auto-complete.</summary>
    public int? TermCount { get; private set; }
    /// <summary>True when the user added this by hand — detection never overwrites or auto-stales it.</summary>
    public bool IsManual { get; private set; }
    public DateTimeOffset DetectedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DismissedAt { get; private set; }

    /// <summary>Payments remaining, or null when the term is unknown.</summary>
    public int? RemainingPayments => TermCount is int term ? Math.Max(0, term - OccurrenceCount) : null;

    private DetectedSubscription() { }

    public static DetectedSubscription Create(
        string userId,
        string merchantNameNormalized,
        string merchantNameDisplay,
        string cadence,
        decimal averageAmount,
        decimal lastKnownAmount,
        string currency,
        DateOnly lastChargeDate,
        DateOnly nextExpectedDate,
        int occurrenceCount,
        int confidenceScore,
        string? category,
        string kind = SubscriptionKinds.Subscription,
        bool isCompleted = false)
    {
        var entity = new DetectedSubscription
        {
            UserId = userId,
            MerchantNameNormalized = merchantNameNormalized,
            MerchantNameDisplay = merchantNameDisplay,
            Cadence = cadence,
            AverageAmount = averageAmount,
            LastKnownAmount = lastKnownAmount,
            Currency = currency,
            LastChargeDate = lastChargeDate,
            NextExpectedDate = nextExpectedDate,
            OccurrenceCount = occurrenceCount,
            ConfidenceScore = confidenceScore,
            Category = category,
            Kind = kind,
        };
        entity.EvaluateCompletion(isCompleted);
        return entity;
    }

    /// <summary>Creates a user-entered installment (manual, never overwritten by detection).</summary>
    public static DetectedSubscription CreateManual(
        string userId,
        string merchantNameDisplay,
        decimal monthlyAmount,
        string currency,
        DateOnly startDate,
        int? termCount)
    {
        var entity = new DetectedSubscription
        {
            UserId = userId,
            MerchantNameNormalized = MerchantNameKey(merchantNameDisplay),
            MerchantNameDisplay = merchantNameDisplay,
            Cadence = "monthly",
            AverageAmount = monthlyAmount,
            LastKnownAmount = monthlyAmount,
            Currency = currency,
            LastChargeDate = startDate,
            NextExpectedDate = startDate.AddMonths(1),
            OccurrenceCount = 1,
            ConfidenceScore = 100,
            Category = null,
            Kind = SubscriptionKinds.Installment,
            TermCount = termCount,
            IsManual = true,
        };
        entity.EvaluateCompletion(false);
        return entity;
    }

    private static string MerchantNameKey(string display) =>
        $"manual:{display.Trim().ToLowerInvariant()}";

    public void UpdateFromDetection(
        string merchantNameDisplay,
        decimal averageAmount,
        decimal lastKnownAmount,
        DateOnly lastChargeDate,
        DateOnly nextExpectedDate,
        int occurrenceCount,
        int confidenceScore,
        string? category,
        string kind = SubscriptionKinds.Subscription,
        bool isCompleted = false)
    {
        MerchantNameDisplay = merchantNameDisplay;
        AverageAmount = averageAmount;
        LastKnownAmount = lastKnownAmount;
        LastChargeDate = lastChargeDate;
        NextExpectedDate = nextExpectedDate;
        OccurrenceCount = occurrenceCount;
        ConfidenceScore = confidenceScore;
        Category = category;
        Kind = kind;
        Status = SubscriptionStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
        EvaluateCompletion(isCompleted);
    }

    /// <summary>Sets the installment term and completes it if payments have already reached it.</summary>
    public void SetTerm(int? termCount)
    {
        TermCount = termCount is > 0 ? termCount : null;
        UpdatedAt = DateTimeOffset.UtcNow;
        EvaluateCompletion(false);
    }

    public void MarkCompleted()
    {
        Status = SubscriptionStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Completes the installment when a payoff was seen or the term has been reached.</summary>
    private void EvaluateCompletion(bool payoffSeen)
    {
        if (payoffSeen || (TermCount is int term && OccurrenceCount >= term))
            Status = SubscriptionStatus.Completed;
    }

    public void MarkDismissed()
    {
        Status = SubscriptionStatus.Dismissed;
        DismissedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Restore()
    {
        Status = SubscriptionStatus.Active;
        DismissedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPotentiallyCancelled()
    {
        Status = SubscriptionStatus.PotentiallyCancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
