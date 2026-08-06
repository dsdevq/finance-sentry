namespace FinanceSentry.Modules.Radar.Domain.MarketStructure;

/// <summary>Per-ticker computed structure. Windows with insufficient bars are null (not evaluable), never 0.</summary>
public sealed record TickerStructure(
    string Ticker,
    IReadOnlyDictionary<int, decimal?> ReturnByWindow,
    IReadOnlyDictionary<int, decimal?> RsByWindow,
    decimal? Ma20,
    decimal? Ma50,
    decimal? Ma200,
    decimal? ExtensionFromMa50,
    decimal? Vol63,
    decimal? TodayZScore,
    decimal? VolumeRatio,
    bool Stale);

public sealed record SectorRotationRow(
    string Sector,
    int Window,
    int Rank,
    int? RankDelta);

public sealed record BreadthResult(
    decimal? PctAboveMa20,
    decimal? PctAboveMa50,
    decimal? PctAboveMa200,
    int Evaluated);

public sealed record RadarSignalDto(
    DateTimeOffset Timestamp,
    string Scanner,
    string SignalType,
    string Severity,
    string SubjectType,
    string Subject,
    string DedupKey,
    IReadOnlyDictionary<string, object> Payload,
    int PayloadVersion);

public sealed record RadarSummary(
    IReadOnlyList<SectorRotationRow> Leaders,
    IReadOnlyList<SectorRotationRow> Laggards,
    BreadthResult Breadth,
    IReadOnlyList<RadarSignalDto> TodayNotable,
    bool Stale);

public sealed record IngestRunSummary(
    int TickersIngested,
    int BarsAdded,
    int Errors,
    IReadOnlyList<string> FailedTickers);

public sealed record ComputeRunSummary(
    int TickersComputed,
    IReadOnlyDictionary<string, int> SignalsByType,
    int Errors);
