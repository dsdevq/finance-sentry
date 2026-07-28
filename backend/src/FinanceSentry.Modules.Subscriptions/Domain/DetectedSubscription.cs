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
    public DateTimeOffset DetectedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DismissedAt { get; private set; }

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
        string kind = SubscriptionKinds.Subscription)
    {
        return new DetectedSubscription
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
    }

    public void UpdateFromDetection(
        string merchantNameDisplay,
        decimal averageAmount,
        decimal lastKnownAmount,
        DateOnly lastChargeDate,
        DateOnly nextExpectedDate,
        int occurrenceCount,
        int confidenceScore,
        string? category,
        string kind = SubscriptionKinds.Subscription)
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
