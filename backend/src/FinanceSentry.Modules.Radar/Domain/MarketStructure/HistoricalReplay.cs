namespace FinanceSentry.Modules.Radar.Domain.MarketStructure;

/// <summary>
/// Pure historical replay of the signal definitions over a persisted bar series (FR-016). Walks the
/// series forward, computing the same z-score/extension the live scanner uses, so seeded episodes
/// (2020 crash, 2022 unwind, 2026-07 memory rotation) produce the expected detections (SC-002).
/// </summary>
public static class HistoricalReplay
{
    public sealed record Thresholds(decimal UnusualMoveZScore, decimal ExtensionThreshold, int VolWindow);

    public sealed record Detection(DateOnly Date, string SignalType, decimal Value);

    private const int Ma50 = 50;

    public static IReadOnlyList<Detection> Replay(IReadOnlyList<DailyBar> bars, Thresholds thresholds)
    {
        var detections = new List<Detection>();
        if (bars.Count == 0)
        {
            return detections;
        }

        var closes = bars.Select(b => b.AdjClose).ToList();

        // Need at least VolWindow+1 closes to have a z-score at index i.
        for (var i = thresholds.VolWindow + 1; i < bars.Count; i++)
        {
            var window = closes.Take(i + 1).ToArray();

            var z = Volatility.TodayZScore(window, thresholds.VolWindow);
            if (z is not null && Math.Abs(z.Value) >= thresholds.UnusualMoveZScore)
            {
                detections.Add(new Detection(bars[i].Date, RadarReplaySignalTypes.UnusualMove, z.Value));
            }

            var ma50 = MovingAverages.Sma(window, Ma50);
            var extension = MovingAverages.Extension(window[^1], ma50);
            if (extension is not null && extension.Value >= thresholds.ExtensionThreshold)
            {
                detections.Add(new Detection(bars[i].Date, RadarReplaySignalTypes.Extended, extension.Value));
            }
        }

        return detections;
    }
}

/// <summary>Signal-type keys used by the replay (kept independent of the Alerts/scanner constants).</summary>
public static class RadarReplaySignalTypes
{
    public const string UnusualMove = "unusual_move";
    public const string Extended = "extended";
}
