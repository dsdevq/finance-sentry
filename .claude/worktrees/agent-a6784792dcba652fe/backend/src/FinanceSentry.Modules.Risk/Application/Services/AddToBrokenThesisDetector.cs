namespace FinanceSentry.Modules.Risk.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Risk.Domain;

public sealed class AddToBrokenThesisDetector : IAddToBrokenThesisDetector
{
    public IReadOnlyList<BrokenThesisFlag> Detect(
        IReadOnlyList<HoldingSnapshot> history, IReadOnlyList<BrokenThesisSummary> brokenTheses)
    {
        var flags = new List<BrokenThesisFlag>();
        var brokenByTicker = brokenTheses
            .GroupBy(t => t.Ticker, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Min(t => t.BrokenAt), StringComparer.OrdinalIgnoreCase);

        foreach (var group in history.GroupBy(h => h.Symbol, StringComparer.OrdinalIgnoreCase))
        {
            if (!brokenByTicker.TryGetValue(group.Key, out var brokenAt))
            {
                continue;
            }

            var ordered = group.OrderBy(h => h.CapturedAt).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var curr = ordered[i];
                if (curr.Quantity > prev.Quantity && curr.CapturedAt > brokenAt)
                {
                    flags.Add(new BrokenThesisFlag(group.Key, curr.CapturedAt, prev.Quantity, curr.Quantity));
                }
            }
        }

        return flags;
    }
}
