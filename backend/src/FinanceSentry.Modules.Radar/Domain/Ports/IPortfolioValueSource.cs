namespace FinanceSentry.Modules.Radar.Domain.Ports;

/// <summary>
/// Read-port: daily brokerage-sleeve value for a user, sourced from net-worth snapshots.
/// Lives in Radar's domain so the BookPerformanceService can consume portfolio history without
/// a compile-time dependency on the Wealth module.
/// </summary>
public interface IPortfolioValueSource
{
    /// <summary>
    /// Daily brokerage totals for the user from <paramref name="from"/> through <paramref name="to"/>,
    /// ordered oldest→newest. Returns an empty list when no snapshots exist for the range.
    /// </summary>
    Task<IReadOnlyList<DailyPortfolioValue>> GetAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
}

public sealed record DailyPortfolioValue(DateOnly Date, decimal BrokerageValueUsd);
