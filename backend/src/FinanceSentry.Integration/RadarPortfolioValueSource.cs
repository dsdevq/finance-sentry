namespace FinanceSentry.Integration;

using FinanceSentry.Modules.Radar.Domain.Ports;
using FinanceSentry.Modules.Wealth.Domain.Repositories;

/// <summary>
/// 412: implements the Radar module's <see cref="IPortfolioValueSource"/> by reading daily
/// brokerage-sleeve totals from the Wealth module's net-worth snapshot table. Lives in the
/// Integration layer so neither module references the other directly.
/// </summary>
public sealed class RadarPortfolioValueSource(INetWorthSnapshotRepository snapshots)
    : IPortfolioValueSource
{
    public async Task<IReadOnlyList<DailyPortfolioValue>> GetAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var raw = await snapshots.GetByUserIdAsync(userId, from, to, ct);
        return raw
            .Select(s => new DailyPortfolioValue(s.SnapshotDate, s.BrokerageTotal))
            .ToList();
    }
}
