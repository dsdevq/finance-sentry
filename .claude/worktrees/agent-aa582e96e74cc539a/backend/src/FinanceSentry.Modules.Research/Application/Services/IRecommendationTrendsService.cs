namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

/// <summary>
/// Structured provider of monthly analyst consensus (feature 037). Unlike
/// <see cref="IAnalystActionsSource"/> this yields aggregate counts, not per-event actions.
/// Implementations degrade silently when unconfigured (FR-002).
/// </summary>
public interface IRecommendationTrendsService
{
    /// <summary>False when no API key is configured — callers skip with a single Debug line.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Fetch current consensus months for the given tickers. Per-ticker failures are swallowed
    /// (Debug) — the whole call throws only when the provider is broken for every ticker
    /// (auth failure / paywall), so the health path fires (FR-009 convention).
    /// </summary>
    Task<IReadOnlyList<RecommendationTrend>> FetchAsync(
        IReadOnlyCollection<string> tickers, CancellationToken ct = default);
}
