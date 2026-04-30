namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

/// <summary>
/// Aggregated dashboard payload for a user.
/// </summary>
public record DashboardData(
    Dictionary<string, decimal> AggregatedBalance,
    decimal TotalNetWorthUsd,
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
public class DashboardQueryService(
    IAggregationService aggregation,
    IMoneyFlowStatisticsService moneyFlow,
    IMerchantCategoryStatisticsService categories,
    ISyncJobRepository syncJobs,
    ICryptoHoldingsReader? cryptoHoldingsReader = null,
    IBrokerageHoldingsReader? brokerageHoldingsReader = null) : IDashboardQueryService
{
    private readonly IAggregationService _aggregation = aggregation ?? throw new ArgumentNullException(nameof(aggregation));
    private readonly IMoneyFlowStatisticsService _moneyFlow = moneyFlow ?? throw new ArgumentNullException(nameof(moneyFlow));
    private readonly IMerchantCategoryStatisticsService _categories = categories ?? throw new ArgumentNullException(nameof(categories));
    private readonly ISyncJobRepository _syncJobs = syncJobs ?? throw new ArgumentNullException(nameof(syncJobs));
    private readonly ICryptoHoldingsReader? _cryptoHoldingsReader = cryptoHoldingsReader;
    private readonly IBrokerageHoldingsReader? _brokerageHoldingsReader = brokerageHoldingsReader;

    /// <inheritdoc />
    public async Task<DashboardData> GetDashboardDataAsync(Guid userId, CancellationToken ct = default)
    {
        // Sequential — DbContext is scoped per request and not thread-safe.
        // Fan-out would require IDbContextFactory.
        var balance = await _aggregation.GetAggregatedBalanceAsync(userId, ct);
        var bankTotalUsd = await _aggregation.GetTotalNetWorthUsdAsync(userId, ct);
        var byType = await _aggregation.GetAccountCountByTypeAsync(userId, ct);
        var flow = await _moneyFlow.GetMonthlyFlowAsync(userId, 6, ct);
        var topCats = await _categories.GetTopCategoriesAsync(userId, 10, ct);
        var lastSync = await _syncJobs.GetLatestSuccessfulByUserIdAsync(userId, ct);

        var cryptoTotalUsd = _cryptoHoldingsReader is not null
            ? (await _cryptoHoldingsReader.GetHoldingsAsync(userId, ct)).Sum(h => h.UsdValue)
            : 0m;

        var brokerageTotalUsd = _brokerageHoldingsReader is not null
            ? (await _brokerageHoldingsReader.GetHoldingsAsync(userId, ct)).Sum(h => h.UsdValue)
            : 0m;

        var accountCount = byType.Values.Sum();

        return new DashboardData(
            balance,
            bankTotalUsd + cryptoTotalUsd + brokerageTotalUsd,
            accountCount,
            byType,
            flow,
            topCats,
            lastSync?.CompletedAt);
    }
}
