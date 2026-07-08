namespace FinanceSentry.Modules.Radar.Domain.Repositories;

using FinanceSentry.Modules.Radar.Domain;

public interface IDailyBarRepository
{
    /// <summary>
    /// True upsert on (Ticker, Date): new days insert, existing days update in place (Yahoo
    /// retroactively rescales adjusted closes after splits/dividends). Returns bars added.
    /// </summary>
    Task<int> UpsertRangeAsync(IReadOnlyCollection<DailyBar> bars, CancellationToken ct = default);

    /// <summary>Bars for a ticker on/after <paramref name="since"/>, ordered oldest→newest.</summary>
    Task<IReadOnlyList<DailyBar>> GetSinceAsync(string ticker, DateOnly since, CancellationToken ct = default);

    /// <summary>Latest stored bar date for a ticker, or null if none.</summary>
    Task<DateOnly?> GetLatestDateAsync(string ticker, CancellationToken ct = default);

    /// <summary>Latest stored bar date per ticker (only tickers with at least one bar).</summary>
    Task<IReadOnlyDictionary<string, DateOnly>> GetLatestDatesAsync(
        IReadOnlyCollection<string> tickers, CancellationToken ct = default);
}
