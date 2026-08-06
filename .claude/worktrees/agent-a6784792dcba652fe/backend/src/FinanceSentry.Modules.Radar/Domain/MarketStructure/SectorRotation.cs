namespace FinanceSentry.Modules.Radar.Domain.MarketStructure;

/// <summary>Pure sector-rotation ranking: rank sectors by RS, and rank deltas vs a prior ranking.</summary>
public static class SectorRotation
{
    /// <summary>
    /// Ranks sectors by relative strength (rank 1 = strongest). Sectors with a null RS are ranked
    /// last (weakest), ordered by name for determinism. Returns rank per sector.
    /// </summary>
    public static IReadOnlyDictionary<string, int> Rank(IReadOnlyDictionary<string, decimal?> rsBySector)
    {
        var ordered = rsBySector
            .OrderByDescending(kv => kv.Value.HasValue)
            .ThenByDescending(kv => kv.Value ?? decimal.MinValue)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Key)
            .ToList();

        var ranks = new Dictionary<string, int>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            ranks[ordered[i]] = i + 1;
        }

        return ranks;
    }

    /// <summary>
    /// Builds rotation rows for a single window: each sector's current rank plus rankDelta =
    /// currentRank − priorRank (positive = fell in the ranking). Delta is null if no prior rank.
    /// </summary>
    public static IReadOnlyList<SectorRotationRow> BuildRows(
        int window,
        IReadOnlyDictionary<string, decimal?> currentRs,
        IReadOnlyDictionary<string, int>? priorRanks)
    {
        var currentRanks = Rank(currentRs);
        return currentRanks
            .OrderBy(kv => kv.Value)
            .Select(kv =>
            {
                int? delta = priorRanks is not null && priorRanks.TryGetValue(kv.Key, out var prior)
                    ? kv.Value - prior
                    : null;
                return new SectorRotationRow(kv.Key, window, kv.Value, delta);
            })
            .ToList();
    }
}
