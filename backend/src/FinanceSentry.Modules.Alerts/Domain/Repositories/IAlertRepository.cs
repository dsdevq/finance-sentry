namespace FinanceSentry.Modules.Alerts.Domain.Repositories;

public interface IAlertRepository
{
    Task<(IReadOnlyList<Alert> Items, int TotalCount, int UnreadCount)> GetPagedAsync(
        Guid userId, string filter, int page, int pageSize, CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

    Task<Alert?> FindActiveAsync(
        Guid userId, string type, Guid? referenceId, CancellationToken ct = default);

    Task<bool> MarkReadAsync(Guid userId, Guid alertId, CancellationToken ct = default);

    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);

    Task<bool> DismissAsync(Guid userId, Guid alertId, CancellationToken ct = default);

    Task ResolveAsync(Guid alertId, CancellationToken ct = default);

    Task<int> PurgeOldAsync(DateTimeOffset olderThan, CancellationToken ct = default);

    Task DeleteByReferenceIdAsync(Guid referenceId, CancellationToken ct = default);

    Task AddAsync(Alert alert, CancellationToken ct = default);
}
