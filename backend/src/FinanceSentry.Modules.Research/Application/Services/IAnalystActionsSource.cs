namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

/// <summary>
/// A free public source of analyst/street actions (feature 030). Implementations live in
/// Infrastructure (MarketBeat market-wide sweep, Yahoo per-ticker). Every source is treated as
/// unreliable-by-default: a source that changes its markup or blocks access MUST throw so the
/// failure is visible (FR-009) rather than silently returning "no new actions".
/// </summary>
public interface IAnalystActionsSource
{
    /// <summary>Stable source key stored on each action (<c>marketbeat</c> | <c>yahoo</c>).</summary>
    string SourceName { get; }

    /// <summary>
    /// Fetch the source's current actions. <paramref name="universe"/> is the per-ticker ingestion
    /// set — market-wide sources ignore it; per-ticker sources iterate it (and return empty when it
    /// is empty). Throws on unreachable source or markup drift.
    /// </summary>
    Task<IReadOnlyList<AnalystActionRecord>> FetchAsync(
        IReadOnlyCollection<string> universe, CancellationToken ct = default);
}

/// <summary>A source-agnostic analyst action before it is stamped with source + ingestion time.</summary>
public sealed record AnalystActionRecord(
    string Ticker,
    string Firm,
    AnalystActionType ActionType,
    string? PriorRating,
    string? NewRating,
    decimal? PriorTarget,
    decimal? NewTarget,
    DateOnly ActionDate,
    string? SourceUrl);
