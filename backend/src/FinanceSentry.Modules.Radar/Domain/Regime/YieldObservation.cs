namespace FinanceSentry.Modules.Radar.Domain.Regime;

/// <summary>A single parsed FRED observation (a dated yield value). Placeholder rows ("." in FRED)
/// are never represented here — they are skipped at parse time.</summary>
public sealed record YieldObservation(DateOnly Date, decimal Value)
{
    /// <summary>The most-recent observation by date, or null when the series has none.</summary>
    public static YieldObservation? Latest(IReadOnlyList<YieldObservation> observations)
        => observations.Count == 0 ? null : observations.OrderByDescending(o => o.Date).First();
}
