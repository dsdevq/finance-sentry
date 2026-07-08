namespace FinanceSentry.Core.Interfaces;

/// <summary>
/// Core-facing read of 018 market structure — lets other modules (Research's opportunity scorer)
/// read a ticker's structure snapshot without depending on the Radar module directly.
/// </summary>
public interface IMarketStructureReader
{
    Task<MarketStructureSnapshot?> GetStructureAsync(string ticker, CancellationToken ct = default);
}

/// <summary>Projection of Radar's <c>TickerStructure</c> across the module boundary.</summary>
public sealed record MarketStructureSnapshot(
    string Ticker,
    IReadOnlyDictionary<int, decimal?> RsByWindow,
    IReadOnlyDictionary<int, decimal?> ReturnByWindow,
    decimal? ExtensionFromMa50,
    decimal? TodayZScore,
    decimal? VolumeRatio,
    decimal? Ma50,
    decimal? Ma200,
    bool Stale);
