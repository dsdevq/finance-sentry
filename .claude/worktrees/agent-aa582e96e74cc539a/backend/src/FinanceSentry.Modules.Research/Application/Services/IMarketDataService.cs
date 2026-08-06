namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

public interface IMarketDataService
{
    Task<IReadOnlyDictionary<string, QuoteCacheEntry>> GetQuotesAsync(
        IReadOnlyCollection<string> tickers, CancellationToken ct = default);

    Task<IReadOnlyList<DailyClose>> GetDailyClosesAsync(
        string ticker, DateOnly since, CancellationToken ct = default);
}

public record DailyClose(DateOnly Date, decimal Close);
