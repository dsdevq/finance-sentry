namespace FinanceSentry.Modules.Subscriptions.Domain.Repositories;

public interface IDetectedSubscriptionRepository
{
    Task<IReadOnlyList<DetectedSubscription>> GetByUserIdAsync(
        string userId, bool includeDismissed, CancellationToken ct = default);

    Task<DetectedSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DetectedSubscription?> FindByUserAndMerchantAsync(
        string userId, string merchantNameNormalized, CancellationToken ct = default);

    Task UpsertAsync(DetectedSubscription subscription, CancellationToken ct = default);

    Task UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);

    Task<IReadOnlyList<DetectedSubscription>> GetActiveByUserIdAsync(
        string userId, CancellationToken ct = default);

    Task<IReadOnlyList<DetectedSubscription>> GetStaleActiveAsync(
        string userId, CancellationToken ct = default);
}
