namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Modules.BankSync.Domain.Repositories;

/// <summary>
/// Aggregated dashboard payload for a user.
/// </summary>
public record DashboardData(
    Dictionary<string, decimal> AggregatedBalance,
    int AccountCount,
    Dictionary<string, int> AccountsByType,
    IReadOnlyList<MonthlyFlow> MonthlyFlow,
    IReadOnlyList<CategoryStat> TopCategories,
    DateTime? LastSyncTimestamp);

/// <summary>
/// Composes all dashboard data in a single call.
/// </summary>
public interface IDashboardQueryService
{
    /// <summary>
    /// Returns the full dashboard payload for the given user.
    /// </summary>
    Task<DashboardData> GetDashboardDataAsync(Guid userId, CancellationToken ct = default);
}

/// <inheritdoc />
public class DashboardQueryService : IDashboardQueryService
{
    private readonly IAggregationService _aggregation;
    private readonly IMoneyFlowStatisticsService _moneyFlow;
    private readonly IMerchantCategoryStatisticsService _categories;
    private readonly ISyncJobRepository _syncJobs;

    public DashboardQueryService(
        IAggregationService aggregation,
        IMoneyFlowStatisticsService moneyFlow,
        IMerchantCategoryStatisticsService categories,
        ISyncJobRepository syncJobs)
    {
        _aggregation = aggregation ?? throw new ArgumentNullException(nameof(aggregation));
        _moneyFlow   = moneyFlow   ?? throw new ArgumentNullException(nameof(moneyFlow));
        _categories  = categories  ?? throw new ArgumentNullException(nameof(categories));
        _syncJobs    = syncJobs    ?? throw new ArgumentNullException(nameof(syncJobs));
    }

    /// <inheritdoc />
    public async Task<DashboardData> GetDashboardDataAsync(Guid userId, CancellationToken ct = default)
    {
        // Fan-out: run independent queries concurrently for performance
        var balanceTask     = _aggregation.GetAggregatedBalanceAsync(userId, ct);
        var byTypeTask      = _aggregation.GetAccountCountByTypeAsync(userId, ct);
        var flowTask        = _moneyFlow.GetMonthlyFlowAsync(userId, 6, ct);
        var categoriesTask  = _categories.GetTopCategoriesAsync(userId, 10, ct);
        var lastSyncTask    = _syncJobs.GetLatestSuccessfulByUserIdAsync(userId, ct);

        await Task.WhenAll(balanceTask, byTypeTask, flowTask, categoriesTask, lastSyncTask);

        var balance    = await balanceTask;
        var byType     = await byTypeTask;
        var flow       = await flowTask;
        var topCats    = await categoriesTask;
        var lastSync   = await lastSyncTask;

        var accountCount = byType.Values.Sum();

        return new DashboardData(
            balance,
            accountCount,
            byType,
            flow,
            topCats,
            lastSync?.CompletedAt);
    }
}
