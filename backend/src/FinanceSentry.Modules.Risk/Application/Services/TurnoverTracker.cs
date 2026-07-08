namespace FinanceSentry.Modules.Risk.Application.Services;

using FinanceSentry.Modules.Risk.Domain;

public sealed class TurnoverTracker : ITurnoverTracker
{
    private const int RollingQuarterDays = 90;

    public int CountDiscretionaryTradesInRollingQuarter(
        IReadOnlyList<HoldingSnapshot> history, DateTimeOffset asOf)
    {
        var since = asOf - TimeSpan.FromDays(RollingQuarterDays);
        var count = 0;

        foreach (var group in history.GroupBy(h => (h.Symbol, h.Sleeve)))
        {
            var ordered = group.OrderBy(h => h.CapturedAt).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var curr = ordered[i];
                if (curr.CapturedAt < since)
                {
                    continue;
                }

                if (curr.Quantity > prev.Quantity)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
