namespace FinanceSentry.Modules.Wealth.Infrastructure.Jobs;

using FinanceSentry.Core.Interfaces;
using Hangfire;

public class HistoricalBackfillJob(
    IEnumerable<IProviderMonthlyHistorySource> historySources,
    INetWorthSnapshotService snapshotService)
{
    private readonly IEnumerable<IProviderMonthlyHistorySource> _historySources = historySources;
    private readonly INetWorthSnapshotService _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var allBalances = new List<ProviderMonthlyBalance>();

        foreach (var source in _historySources)
        {
            var balances = await source.GetMonthlyBalancesAsync(userId, ct);
            allBalances.AddRange(balances);
        }

        var snapshots = allBalances
            .GroupBy(b => b.MonthEnd)
            .Select(g => new NetWorthSnapshotData(
                SnapshotDate: g.Key,
                BankingTotal: g.Where(b => b.AssetCategory == "banking").Sum(b => b.TotalUsd),
                BrokerageTotal: g.Where(b => b.AssetCategory == "brokerage").Sum(b => b.TotalUsd),
                CryptoTotal: g.Where(b => b.AssetCategory == "crypto").Sum(b => b.TotalUsd)))
            .OrderBy(s => s.SnapshotDate)
            .ToList();

        await _snapshotService.ReplaceAllSnapshotsAsync(userId, snapshots, ct);
    }
}
