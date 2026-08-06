namespace FinanceSentry.Core.Interfaces;

/// <summary>
/// Core read of the Research watchlist for the Radar universe — lets Radar read watchlist
/// tickers without depending on the Research module's internal repository.
/// </summary>
public interface IWatchlistReader
{
    Task<IReadOnlyList<string>> ListTickersAsync(Guid userId, CancellationToken ct = default);
}
