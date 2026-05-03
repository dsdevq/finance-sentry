namespace FinanceSentry.Modules.Subscriptions.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Subscriptions.Domain;
using FinanceSentry.Modules.Subscriptions.Domain.Repositories;

public class SubscriptionDetectionResultService(IDetectedSubscriptionRepository repository)
    : ISubscriptionDetectionResultService
{
    private readonly IDetectedSubscriptionRepository _repository = repository;

    public async Task UpsertDetectedSubscriptionsAsync(
        string userId,
        IReadOnlyList<DetectedSubscriptionData> results,
        CancellationToken ct = default)
    {
        foreach (var result in results)
        {
            var existing = await _repository.FindByUserAndMerchantAsync(
                userId, result.MerchantNameNormalized, ct);

            if (existing is null)
            {
                var subscription = DetectedSubscription.Create(
                    userId,
                    result.MerchantNameNormalized,
                    result.MerchantNameDisplay,
                    result.Cadence,
                    result.AverageAmount,
                    result.LastKnownAmount,
                    result.Currency,
                    result.LastChargeDate,
                    result.NextExpectedDate,
                    result.OccurrenceCount,
                    result.ConfidenceScore,
                    result.Category);

                await _repository.UpsertAsync(subscription, ct);
            }
            else if (existing.Status != SubscriptionStatus.Dismissed)
            {
                existing.UpdateFromDetection(
                    result.MerchantNameDisplay,
                    result.AverageAmount,
                    result.LastKnownAmount,
                    result.LastChargeDate,
                    result.NextExpectedDate,
                    result.OccurrenceCount,
                    result.ConfidenceScore,
                    result.Category);

                await _repository.UpsertAsync(existing, ct);
            }
        }
    }

    public async Task MarkStaleAsPotentiallyCancelledAsync(
        string userId,
        CancellationToken ct = default)
    {
        var active = await _repository.GetStaleActiveAsync(userId, ct);
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var subscription in active)
        {
            var averageIntervalDays = subscription.Cadence == "annual" ? 365 : 30;
            var staleThreshold = subscription.LastChargeDate.AddDays((int)(averageIntervalDays * 1.5));

            if (now > staleThreshold)
            {
                subscription.MarkPotentiallyCancelled();
                await _repository.UpsertAsync(subscription, ct);
            }
        }
    }
}
