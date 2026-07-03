namespace FinanceSentry.Modules.Research.Infrastructure.Jobs;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class NewsIngestionJob(
    ResearchDbContext research,
    IBrokerageHoldingsReader brokerage,
    ICryptoHoldingsReader crypto,
    IMarketNewsService news,
    ILogger<NewsIngestionJob> logger)
{
    private const int MaxTickersPerRun = 60;

    public async Task IngestTickersAsync(CancellationToken ct = default)
    {
        var tickers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var watchlistTickers = await research.WatchlistItems.AsNoTracking()
            .Select(w => w.Ticker)
            .ToListAsync(ct);
        foreach (var t in watchlistTickers)
        {
            tickers.Add(t);
        }

        var thesisTickers = await research.Theses.AsNoTracking()
            .Select(t => t.Ticker)
            .ToListAsync(ct);
        foreach (var t in thesisTickers)
        {
            tickers.Add(t);
        }

        var userIds = await research.WatchlistItems.AsNoTracking()
            .Select(w => w.UserId)
            .Union(research.Theses.AsNoTracking().Select(t => t.UserId))
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in userIds)
        {
            foreach (var h in await brokerage.GetHoldingsAsync(userId, ct))
            {
                tickers.Add(h.Symbol);
            }

            foreach (var h in await crypto.GetHoldingsAsync(userId, ct))
            {
                tickers.Add(h.Asset + "-USD");
            }
        }

        var toFetch = tickers.Take(MaxTickersPerRun).ToArray();
        var inserted = await news.IngestForTickersAsync(toFetch, ct);
        logger.LogInformation("NewsIngestionJob (tickers) ingested {Count} new articles across {Tickers} tickers", inserted, toFetch.Length);
    }

    public async Task IngestFedAsync(CancellationToken ct = default)
    {
        var inserted = await news.IngestFedPressAsync(ct);
        logger.LogInformation("NewsIngestionJob (fed) ingested {Count} new articles", inserted);
    }
}
