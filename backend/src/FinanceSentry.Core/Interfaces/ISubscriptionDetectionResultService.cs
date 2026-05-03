namespace FinanceSentry.Core.Interfaces;

public interface ISubscriptionDetectionResultService
{
    Task UpsertDetectedSubscriptionsAsync(
        string userId,
        IReadOnlyList<DetectedSubscriptionData> results,
        CancellationToken ct = default);

    Task MarkStaleAsPotentiallyCancelledAsync(
        string userId,
        CancellationToken ct = default);
}

public record DetectedSubscriptionData(
    string MerchantNameNormalized,
    string MerchantNameDisplay,
    string Cadence,
    decimal AverageAmount,
    decimal LastKnownAmount,
    string Currency,
    DateOnly LastChargeDate,
    DateOnly NextExpectedDate,
    int OccurrenceCount,
    int ConfidenceScore,
    string? Category);
