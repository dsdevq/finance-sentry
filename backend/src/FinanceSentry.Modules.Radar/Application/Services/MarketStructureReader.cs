namespace FinanceSentry.Modules.Radar.Application.Services;

using FinanceSentry.Core.Interfaces;

/// <summary>
/// <see cref="IMarketStructureReader"/> impl: projects Radar's internal <c>TickerStructure</c> into
/// the Core-facing <see cref="MarketStructureSnapshot"/> so other modules (019) can read structure
/// without a compile-time dependency on Radar.
/// </summary>
public sealed class MarketStructureReader(IStructureQueryService structureQueryService) : IMarketStructureReader
{
    public async Task<MarketStructureSnapshot?> GetStructureAsync(string ticker, CancellationToken ct = default)
    {
        var structure = await structureQueryService.GetStructureAsync(ticker, ct);
        return structure is null
            ? null
            : new MarketStructureSnapshot(
                structure.Ticker,
                structure.RsByWindow,
                structure.ReturnByWindow,
                structure.ExtensionFromMa50,
                structure.TodayZScore,
                structure.VolumeRatio,
                structure.Ma50,
                structure.Ma200,
                structure.Stale);
    }
}
