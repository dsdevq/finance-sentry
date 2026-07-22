namespace FinanceSentry.Modules.Research.Infrastructure.Jobs;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using FinanceSentry.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Idempotently seeds the market-wide default news feeds and the TrendForce → DRAM thesis page source
/// (feature 030, T046/FR-007). Runs on a schedule (and startup) so the defaults exist without a data
/// migration; existing sources (matched by URL) are left untouched. The TrendForce source is only
/// seeded once a DRAM thesis exists — it is skipped gracefully otherwise.
/// </summary>
public sealed class NewsSourceSeedJob(
    INewsSourceRepository sources,
    ResearchDbContext research,
    ILogger<NewsSourceSeedJob> logger)
{
    private const string TrendForceUrl = "https://www.trendforce.com/presscenter/";

    private static readonly (string Name, string Url)[] MarketWideRssDefaults =
    [
        ("Yahoo Finance Top Stories", "https://finance.yahoo.com/news/rssindex"),
        ("MarketWatch Top Stories", "http://feeds.marketwatch.com/marketwatch/topstories/"),
    ];

    private static readonly string[] DramKeywords = ["DRAM", "HBM", "NAND", "memory"];

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var seeded = 0;

        foreach (var (name, url) in MarketWideRssDefaults)
        {
            if (await sources.GetByUrlAsync(url, ct) is not null)
            {
                continue;
            }

            await sources.AddAsync(new NewsSource { Name = name, Kind = NewsSourceKind.Rss, Url = url }, ct);
            seeded++;
        }

        seeded += await SeedTrendForceAsync(ct);
        logger.LogInformation("NewsSourceSeedJob seeded {Count} new sources", seeded);
    }

    private async Task<int> SeedTrendForceAsync(CancellationToken ct)
    {
        if (await sources.GetByUrlAsync(TrendForceUrl, ct) is not null)
        {
            return 0;
        }

        var dramThesisId = await FindDramThesisIdAsync(ct);
        if (dramThesisId is null)
        {
            logger.LogInformation("TrendForce source not seeded — no DRAM thesis found yet (skipped gracefully)");
            return 0;
        }

        await sources.AddAsync(new NewsSource
        {
            Name = "TrendForce Press Center",
            Kind = NewsSourceKind.Page,
            Url = TrendForceUrl,
            ThesisId = dramThesisId,
            Keywords = [.. DramKeywords],
        }, ct);
        return 1;
    }

    private async Task<Guid?> FindDramThesisIdAsync(CancellationToken ct)
    {
        var byTicker = await research.Theses.AsNoTracking()
            .Where(t => t.Ticker == "DRAM")
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);
        if (byTicker is not null)
        {
            return byTicker;
        }

        // Fall back to a text match (theses are few) so a DRAM thesis filed under a proxy ticker still binds.
        var candidates = await research.Theses.AsNoTracking()
            .Select(t => new { t.Id, t.ThesisText })
            .ToListAsync(ct);
        return candidates
            .FirstOrDefault(t => t.ThesisText.Contains("DRAM", StringComparison.OrdinalIgnoreCase))?.Id;
    }
}
