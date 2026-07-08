namespace FinanceSentry.Core.Interfaces;

/// <summary>
/// Core-facing read of 018 market structure — lets other modules (Research's opportunity scorer)
/// read a ticker's structure snapshot without depending on the Radar module directly.
/// </summary>
public interface IMarketStructureReader
{
    Task<MarketStructureSnapshot?> GetStructureAsync(string ticker, CancellationToken ct = default);

    /// <summary>
    /// Pairwise 63-day daily-return correlations among <paramref name="tickers"/>, computed from
    /// persisted bars. Pairs without enough overlapping history are omitted (022 FR-001d).
    /// </summary>
    Task<IReadOnlyList<PairwiseCorrelation>> GetPairwiseCorrelationsAsync(
        IReadOnlyCollection<string> tickers, CancellationToken ct = default);
}

public sealed record PairwiseCorrelation(string TickerA, string TickerB, decimal Correlation);

/// <summary>
/// Projection of Radar's <c>TickerStructure</c> across the module boundary, plus the 019 FR-003
/// scoring inputs: the ticker's (affinity-assigned) sector rotation rank + delta, and distance
/// from the 63-day high (breakout state; 0 = at/above the high). Null when not computable.
/// </summary>
public sealed record MarketStructureSnapshot(
    string Ticker,
    IReadOnlyDictionary<int, decimal?> RsByWindow,
    IReadOnlyDictionary<int, decimal?> ReturnByWindow,
    decimal? ExtensionFromMa50,
    decimal? TodayZScore,
    decimal? VolumeRatio,
    decimal? Ma50,
    decimal? Ma200,
    bool Stale,
    int? SectorRank = null,
    int? SectorRankDelta = null,
    decimal? DistanceFrom63dHigh = null);
