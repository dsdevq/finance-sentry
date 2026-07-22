namespace FinanceSentry.Modules.Research.API.Responses;

public record AnalystActionDto(
    string Ticker,
    string Firm,
    string ActionType,
    string? PriorRating,
    string? NewRating,
    decimal? PriorTarget,
    decimal? NewTarget,
    DateOnly ActionDate,
    string Source,
    string? SourceUrl,
    DateTimeOffset IngestedAt);

/// <summary>
/// Envelope for an analyst-actions query. <see cref="Coverage"/> distinguishes "no coverage in the
/// universe" from "no recent actions" (spec edge case): <c>inUniverse</c> | <c>notInUniverse</c>
/// (a specific ticker was queried) | <c>marketWide</c> (no ticker filter).
/// </summary>
public record AnalystActionsResult(
    IReadOnlyList<AnalystActionDto> Actions,
    string Coverage,
    DateTimeOffset RetrievedAt);
