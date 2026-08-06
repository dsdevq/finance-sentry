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

/// <summary>One month of aggregate analyst consensus for the queried ticker (feature 037).</summary>
public record RecommendationTrendDto(
    DateOnly Period,
    int StrongBuy,
    int Buy,
    int Hold,
    int Sell,
    int StrongSell,
    string Source,
    DateTimeOffset IngestedAt);

/// <summary>
/// Envelope for an analyst-actions query. <see cref="Coverage"/> distinguishes "no coverage in the
/// universe" from "no recent actions" (spec edge case): <c>inUniverse</c> | <c>notInUniverse</c>
/// (a specific ticker was queried) | <c>marketWide</c> (no ticker filter).
/// <see cref="RecommendationTrends"/> (feature 037) is present only on ticker-filtered queries:
/// latest consensus months, newest first; empty = tracked but no structured consensus yet.
/// </summary>
public record AnalystActionsResult(
    IReadOnlyList<AnalystActionDto> Actions,
    string Coverage,
    DateTimeOffset RetrievedAt,
    IReadOnlyList<RecommendationTrendDto>? RecommendationTrends = null);
