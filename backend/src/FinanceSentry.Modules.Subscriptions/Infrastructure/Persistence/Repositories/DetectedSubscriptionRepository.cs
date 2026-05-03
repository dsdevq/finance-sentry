namespace FinanceSentry.Modules.Subscriptions.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Subscriptions.Domain;
using FinanceSentry.Modules.Subscriptions.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class DetectedSubscriptionRepository(SubscriptionsDbContext db) : IDetectedSubscriptionRepository
{
    private readonly SubscriptionsDbContext _db = db;

    public async Task<IReadOnlyList<DetectedSubscription>> GetByUserIdAsync(
        string userId, bool includeDismissed, CancellationToken ct = default)
    {
        var query = _db.DetectedSubscriptions.AsNoTracking()
            .Where(s => s.UserId == userId);

        if (!includeDismissed)
            query = query.Where(s => s.Status != SubscriptionStatus.Dismissed);

        return await query.OrderBy(s => s.MerchantNameDisplay).ToListAsync(ct);
    }

    public Task<DetectedSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.DetectedSubscriptions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<DetectedSubscription?> FindByUserAndMerchantAsync(
        string userId, string merchantNameNormalized, CancellationToken ct = default)
        => _db.DetectedSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.MerchantNameNormalized == merchantNameNormalized, ct);

    public async Task UpsertAsync(DetectedSubscription subscription, CancellationToken ct = default)
    {
        var existing = await _db.DetectedSubscriptions
            .FirstOrDefaultAsync(s => s.Id == subscription.Id, ct);

        if (existing is null)
            _db.DetectedSubscriptions.Add(subscription);

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        var subscription = await _db.DetectedSubscriptions.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (subscription is null) return;

        if (status == SubscriptionStatus.Dismissed)
            subscription.MarkDismissed();
        else if (status == SubscriptionStatus.Active)
            subscription.Restore();
        else if (status == SubscriptionStatus.PotentiallyCancelled)
            subscription.MarkPotentiallyCancelled();

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DetectedSubscription>> GetActiveByUserIdAsync(
        string userId, CancellationToken ct = default)
    {
        return await _db.DetectedSubscriptions.AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DetectedSubscription>> GetStaleActiveAsync(
        string userId, CancellationToken ct = default)
    {
        return await _db.DetectedSubscriptions
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(ct);
    }
}
