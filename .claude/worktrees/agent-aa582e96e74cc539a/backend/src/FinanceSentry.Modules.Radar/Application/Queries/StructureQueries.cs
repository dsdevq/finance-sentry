namespace FinanceSentry.Modules.Radar.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using FinanceSentry.Modules.Radar.Domain.Repositories;

// ── get_market_structure ────────────────────────────────────────────────────
public sealed record GetMarketStructureQuery(string Ticker) : IQuery<TickerStructure?>;

public sealed class GetMarketStructureQueryHandler(IStructureQueryService structure)
    : IQueryHandler<GetMarketStructureQuery, TickerStructure?>
{
    public Task<TickerStructure?> Handle(GetMarketStructureQuery query, CancellationToken cancellationToken)
        => structure.GetStructureAsync(query.Ticker, cancellationToken);
}

// ── get_relative_strength ───────────────────────────────────────────────────
public sealed record GetRelativeStrengthQuery(IReadOnlyList<string>? Tickers)
    : IQuery<IReadOnlyList<TickerStructure>>;

public sealed class GetRelativeStrengthQueryHandler(IStructureQueryService structure)
    : IQueryHandler<GetRelativeStrengthQuery, IReadOnlyList<TickerStructure>>
{
    public Task<IReadOnlyList<TickerStructure>> Handle(
        GetRelativeStrengthQuery query, CancellationToken cancellationToken)
        => structure.GetStructuresAsync(query.Tickers, cancellationToken);
}

// ── get_sector_rotation ─────────────────────────────────────────────────────
public sealed record GetSectorRotationQuery : IQuery<IReadOnlyList<SectorRotationRow>>;

public sealed class GetSectorRotationQueryHandler(IStructureQueryService structure)
    : IQueryHandler<GetSectorRotationQuery, IReadOnlyList<SectorRotationRow>>
{
    public Task<IReadOnlyList<SectorRotationRow>> Handle(
        GetSectorRotationQuery query, CancellationToken cancellationToken)
        => structure.GetSectorRotationAsync(cancellationToken);
}

// ── get_market_breadth ──────────────────────────────────────────────────────
public sealed record GetMarketBreadthQuery : IQuery<BreadthResult>;

public sealed class GetMarketBreadthQueryHandler(IStructureQueryService structure)
    : IQueryHandler<GetMarketBreadthQuery, BreadthResult>
{
    public Task<BreadthResult> Handle(GetMarketBreadthQuery query, CancellationToken cancellationToken)
        => structure.GetBreadthAsync(cancellationToken);
}

// ── list_signals ────────────────────────────────────────────────────────────
public sealed record ListSignalsQuery(
    DateOnly? Since,
    string? Scanner,
    string? Type,
    string? Subject,
    Guid? UserId) : IQuery<IReadOnlyList<RadarSignalDto>>;

public sealed class ListSignalsQueryHandler(IStructureQueryService structure)
    : IQueryHandler<ListSignalsQuery, IReadOnlyList<RadarSignalDto>>
{
    public Task<IReadOnlyList<RadarSignalDto>> Handle(ListSignalsQuery query, CancellationToken cancellationToken)
    {
        var since = query.Since is not null
            ? new DateTimeOffset(query.Since.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);

        return structure.ListSignalsAsync(
            new SignalFilter(since, query.Scanner, query.Type, query.Subject, query.UserId),
            cancellationToken);
    }
}

// ── get_radar_summary ───────────────────────────────────────────────────────
public sealed record GetRadarSummaryQuery : IQuery<RadarSummary>;

public sealed class GetRadarSummaryQueryHandler(IStructureQueryService structure)
    : IQueryHandler<GetRadarSummaryQuery, RadarSummary>
{
    public Task<RadarSummary> Handle(GetRadarSummaryQuery query, CancellationToken cancellationToken)
        => structure.GetSummaryAsync(cancellationToken);
}
