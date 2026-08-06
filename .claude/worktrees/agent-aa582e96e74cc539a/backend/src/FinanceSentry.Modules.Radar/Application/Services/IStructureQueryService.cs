namespace FinanceSentry.Modules.Radar.Application.Services;

using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using FinanceSentry.Modules.Radar.Domain.Repositories;

public interface IStructureQueryService
{
    Task<TickerStructure?> GetStructureAsync(string ticker, CancellationToken ct = default);

    Task<IReadOnlyList<TickerStructure>> GetStructuresAsync(
        IReadOnlyList<string>? tickers, CancellationToken ct = default);

    Task<IReadOnlyList<SectorRotationRow>> GetSectorRotationAsync(CancellationToken ct = default);

    Task<BreadthResult> GetBreadthAsync(CancellationToken ct = default);

    Task<RadarSummary> GetSummaryAsync(CancellationToken ct = default);

    Task<IReadOnlyList<RadarSignalDto>> ListSignalsAsync(SignalFilter filter, CancellationToken ct = default);
}
