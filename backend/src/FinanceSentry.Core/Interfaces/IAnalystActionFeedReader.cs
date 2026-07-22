namespace FinanceSentry.Core.Interfaces;

/// <summary>
/// Cross-module read contract (feature 031): newly-ingested analyst actions, for the Companion module
/// to surface street actions on held names. Implemented by the Research module.
/// </summary>
public interface IAnalystActionFeedReader
{
    /// <summary>Analyst actions ingested after <paramref name="watermark"/>, oldest first.</summary>
    Task<IReadOnlyList<AnalystActionFeedRecord>> GetNewSinceAsync(
        DateTimeOffset watermark, int limit, CancellationToken ct = default);
}

/// <summary>A lightweight projection of an analyst action for companion capture.</summary>
public sealed record AnalystActionFeedRecord(
    Guid ActionId,
    string Ticker,
    string Firm,
    string ActionType,
    decimal? NewTarget,
    DateTimeOffset IngestedAt);
