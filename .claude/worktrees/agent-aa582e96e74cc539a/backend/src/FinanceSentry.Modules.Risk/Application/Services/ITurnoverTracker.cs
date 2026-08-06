namespace FinanceSentry.Modules.Risk.Application.Services;

using FinanceSentry.Modules.Risk.Domain;

/// <summary>Pure function over HoldingSnapshot deltas -&gt; discretionary trade count/quarter (FR-001b).</summary>
public interface ITurnoverTracker
{
    /// <summary>
    /// Counts distinct (Symbol, Sleeve) quantity-increase events in the trailing rolling quarter
    /// (90 days) ending at <paramref name="asOf"/>. A quantity decrease is never counted — the
    /// turnover budget targets discretionary adds/new-buys (Barber-Odean framing), not trims.
    /// </summary>
    int CountDiscretionaryTradesInRollingQuarter(
        IReadOnlyList<HoldingSnapshot> history, DateTimeOffset asOf);
}
