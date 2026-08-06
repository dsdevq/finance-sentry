namespace FinanceSentry.Modules.Research.API.Responses;

/// <summary>Aggregate track-record output (US3) — a DTO, never persisted (data-model.md).</summary>
public record TrackRecordSummaryDto(
    int TotalCount,
    int ClosedCount,
    int ExcludedCount,
    decimal? TerminalHitRate,
    decimal? ActiveHitRate,
    decimal? AverageExcessReturnPct,
    decimal? MedianExcessReturnPct,
    decimal? BestExcessReturnPct,
    decimal? WorstExcessReturnPct,
    IReadOnlyDictionary<string, TrackRecordSliceDto> BySource,
    IReadOnlyDictionary<string, TrackRecordSliceDto> ByStatus,
    bool LowSampleCaveat);

public record TrackRecordSliceDto(
    int Count,
    decimal? HitRate,
    decimal? AverageExcessReturnPct);
