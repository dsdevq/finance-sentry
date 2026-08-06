namespace FinanceSentry.Modules.Research.Domain.Repositories;

using FinanceSentry.Modules.Research.Domain;

/// <summary>Persistence for monthly consensus trends (feature 037).</summary>
public interface IRecommendationTrendRepository
{
    /// <summary>
    /// Inserts new <c>(Ticker, Period)</c> rows and updates counts (+<c>IngestedAt</c>) on existing
    /// ones — providers restate recent months. Returns the number of newly inserted rows.
    /// </summary>
    Task<int> UpsertAsync(IReadOnlyList<RecommendationTrend> trends, CancellationToken ct = default);

    /// <summary>Latest <paramref name="months"/> periods for one ticker, newest first.</summary>
    Task<IReadOnlyList<RecommendationTrend>> GetLatestAsync(
        string ticker, int months, CancellationToken ct = default);
}
