namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

/// <summary>Calculator output — a DTO, never persisted (data-model.md).</summary>
public record ThesisPerformanceResult(
    Guid SubjectId,
    ThesisEventType FromEvent,
    ThesisEventType ToEvent,
    decimal? AbsoluteReturnPct,
    decimal? BenchmarkReturnPct,
    decimal? ExcessReturnPct,
    decimal? NetAbsoluteReturnPct,
    decimal? NetExcessReturnPct,
    bool IsEvaluable,
    string? ExclusionReason,
    string PriceSourceUsed);

/// <summary>
/// Pure inputs for <see cref="IThesisPerformanceCalculator"/> — every price/timestamp the caller
/// resolved (from persisted <see cref="ThesisEvent"/> rows or a live quote), so the calculator
/// itself performs no I/O (SC-001).
/// </summary>
public record ThesisPerformanceInput(
    Guid SubjectId,
    ThesisEventType FromEvent,
    DateTimeOffset FromTimestamp,
    decimal? FromSubjectPrice,
    decimal? FromBenchmarkPrice,
    ThesisEventType ToEvent,
    DateTimeOffset ToTimestamp,
    decimal? ToSubjectPrice,
    decimal? ToBenchmarkPrice,
    string PriceSourceUsed,
    FrictionConfig Friction);
