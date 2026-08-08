namespace FinanceSentry.Modules.Radar.Domain.Repositories;

using FinanceSentry.Modules.Radar.Domain.Regime;

/// <summary>Persistence for the daily market-regime readings (feature 021).</summary>
public interface IRegimeReadingRepository
{
    Task AppendAsync(RegimeReading reading, CancellationToken ct = default);

    /// <summary>The newest reading, or null when none has ever been computed.</summary>
    Task<RegimeReading?> LatestAsync(CancellationToken ct = default);

    /// <summary>The newest reading strictly before <paramref name="before"/> (for band-change detection).</summary>
    Task<RegimeReading?> PriorAsync(DateTimeOffset before, CancellationToken ct = default);

    /// <summary>
    /// The most-recent readings (newest first, capped by <paramref name="limit"/>), used to locate
    /// the last band change per axis.
    /// </summary>
    Task<IReadOnlyList<RegimeReading>> RecentAsync(int limit, CancellationToken ct = default);
}
