namespace FinanceSentry.Modules.Research.Application.Services;

public interface IMarketNewsService
{
    Task<int> IngestForTickersAsync(IReadOnlyCollection<string> tickers, CancellationToken ct = default);

    Task<int> IngestFedPressAsync(CancellationToken ct = default);
}
