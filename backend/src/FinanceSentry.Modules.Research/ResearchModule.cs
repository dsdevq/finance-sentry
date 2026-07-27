namespace FinanceSentry.Modules.Research;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain.Repositories;
using FinanceSentry.Modules.Research.Infrastructure.Jobs;
using FinanceSentry.Modules.Research.Infrastructure.Persistence;
using FinanceSentry.Modules.Research.Infrastructure.Services;
using FinanceSentry.Modules.Research.Infrastructure.Sources;
using FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class ResearchModule
{
    internal sealed class ModuleRegistrar : IModuleRegistrar
    {
        public void Register(IServiceCollection services, IConfiguration config)
            => services.AddResearchModule(config);
    }

    private sealed class JobRegistrar : IJobRegistrar
    {
        public void RegisterJobs(IServiceProvider sp)
        {
            var mgr = sp.GetRequiredService<IRecurringJobManager>();

            mgr.AddOrUpdate<NewsIngestionJob>(
                "research-news-tickers",
                job => job.IngestTickersAsync(CancellationToken.None),
                "*/30 * * * *");

            mgr.AddOrUpdate<NewsIngestionJob>(
                "research-news-fed",
                job => job.IngestFedAsync(CancellationToken.None),
                "0 */6 * * *");

            mgr.AddOrUpdate<MacroCalendarSeedJob>(
                "research-macro-seed",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily(3));

            // Seed market-wide default feeds + TrendForce→DRAM page source (feature 030). Idempotent.
            mgr.AddOrUpdate<NewsSourceSeedJob>(
                "research-news-sources-seed",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily(2));

            mgr.AddOrUpdate<ThesisMonitorJob>(
                "thesis-monitor",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily());

            mgr.AddOrUpdate<ThesisTrackRecordSnapshotJob>(
                "thesis-track-record-snapshot",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Weekly());

            mgr.AddOrUpdate<CandidateExpiryJob>(
                "opportunity-candidate-expiry",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily());

            var opportunity = sp.GetRequiredService<IOptions<OpportunityOptions>>().Value;
            mgr.AddOrUpdate<OpportunityScanJob>(
                "opportunity-scan",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily(opportunity.ScanHourUtc));

            // Nightly analyst-actions ingestion (feature 030), 01:00 UTC — after opportunity-scan (00:00).
            mgr.AddOrUpdate<AnalystActionsIngestionJob>(
                "analyst-actions-ingestion",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily(1));

            // Research retrieval indexing (feature 036), offset 15 min from the */30 news ingestion
            // so freshly ingested articles are chunked/embedded shortly after they land.
            mgr.AddOrUpdate<ResearchIndexingJob>(
                "research-retrieval-indexing",
                job => job.ExecuteAsync(CancellationToken.None),
                "15,45 * * * *");
        }
    }

    public static IServiceCollection AddResearchModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ResearchDbContext>(
            o => o.UseNpgsql(
                config.GetConnectionString("Default")!,
                b => b.MigrationsHistoryTable("__ef_migrations_history_research", "public")));

        services.AddScoped<IWatchlistRepository, WatchlistRepository>();
        services.AddScoped<IWatchlistReader, WatchlistReader>();
        services.AddScoped<IThesisRepository, ThesisRepository>();
        services.AddScoped<IBrokenThesisReader, BrokenThesisReader>();
        services.AddScoped<IQuoteCacheRepository, QuoteCacheRepository>();
        services.AddScoped<INewsRepository, NewsRepository>();
        services.AddScoped<IMacroCalendarRepository, MacroCalendarRepository>();
        services.AddScoped<IIpsRepository, IpsRepository>();
        services.AddScoped<IThesisEventRepository, ThesisEventRepository>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<ICandidateScoreRepository, CandidateScoreRepository>();
        services.AddScoped<IAnalystActionRepository, AnalystActionRepository>();
        services.AddScoped<IAnalystUniverseRepository, AnalystUniverseRepository>();
        services.AddScoped<INewsSourceRepository, NewsSourceRepository>();
        services.AddScoped<IValuationSnapshotRepository, ValuationSnapshotRepository>();
        services.AddScoped<IResearchDocumentRepository, ResearchDocumentRepository>();
        services.AddScoped<IResearchRetrievalRepository, ResearchRetrievalRepository>();
        services.Configure<OpportunityOptions>(config.GetSection(OpportunityOptions.SectionName));
        services.Configure<ResearchRetrievalOptions>(config.GetSection(ResearchRetrievalOptions.SectionName));

        services.AddHttpClient(YahooMarketDataService.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://query1.finance.yahoo.com");
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (compatible; FinanceSentry/1.0; +https://finance-sentry.local)");
        });

        services.AddHttpClient(RssMarketNewsService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (compatible; FinanceSentry/1.0; +https://finance-sentry.local)");
        });

        // Yahoo's quoteSummary/calendarEvents endpoint requires a cookie + crumb pair. Share one
        // CookieContainer across handler rotations so the seeded consent cookies persist as long as
        // the crumb the service caches. A browser-like UA is required or Yahoo rejects the request.
        var yahooEarningsCookies = new System.Net.CookieContainer();
        services.AddHttpClient(YahooEarningsCalendarService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(12);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            CookieContainer = yahooEarningsCookies,
            UseCookies = true,
            AutomaticDecompression = System.Net.DecompressionMethods.All,
        })
        .SetHandlerLifetime(TimeSpan.FromHours(2));

        // SEC EDGAR (filings + XBRL fundamentals). SEC policy REQUIRES a descriptive User-Agent
        // with contact info and permits gzip; a generic UA gets throttled/blocked.
        services.AddHttpClient(SecEdgarService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FinanceSentry/1.0 (contact: payar3282@gmail.com)");
            client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
        });

        // MarketBeat daily ratings page (analyst actions market-wide sweep). A browser-like UA is
        // required or the page returns a challenge instead of the table.
        services.AddHttpClient(MarketBeatAnalystActionsSource.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
        });

        // Yahoo quoteSummary/upgradeDowngradeHistory (per-ticker analyst actions) — same crumb + cookie
        // dance as the earnings client, with its own CookieContainer so its crumb/cookies are isolated.
        var yahooAnalystCookies = new System.Net.CookieContainer();
        services.AddHttpClient(YahooAnalystActionsSource.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(12);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            CookieContainer = yahooAnalystCookies,
            UseCookies = true,
            AutomaticDecompression = System.Net.DecompressionMethods.All,
        })
        .SetHandlerLifetime(TimeSpan.FromHours(2));

        // Yahoo quoteSummary valuation modules (feature 030, US2) — same crumb + cookie dance with its
        // own isolated CookieContainer so its crumb/cookies don't collide with the analyst client.
        var yahooValuationCookies = new System.Net.CookieContainer();
        services.AddHttpClient(YahooValuationDataService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(12);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            CookieContainer = yahooValuationCookies,
            UseCookies = true,
            AutomaticDecompression = System.Net.DecompressionMethods.All,
        })
        .SetHandlerLifetime(TimeSpan.FromHours(2));

        services.AddScoped<IMarketDataService, YahooMarketDataService>();
        services.AddScoped<IMarketNewsService, RssMarketNewsService>();
        services.AddScoped<IMacroCalendarService, MacroCalendarService>();
        services.AddScoped<IThesisEventRecorder, ThesisEventRecorder>();
        services.AddScoped<IThesisPerformanceCalculator, ThesisPerformanceCalculator>();
        services.Configure<FrictionConfig>(config.GetSection(FrictionConfig.SectionName));

        // Singleton: holds the cached Yahoo crumb + per-ticker event cache across requests.
        services.AddSingleton<IEarningsCalendarService, YahooEarningsCalendarService>();

        // Singleton: caches the ticker->CIK map + per-ticker EDGAR results across requests.
        services.AddSingleton<ISecEdgarService, SecEdgarService>();

        // Analyst-actions sources (feature 030). Both singletons: MarketBeat is stateless; Yahoo caches
        // its crumb. Registered under IAnalystActionsSource so the job resolves them as IEnumerable.
        services.AddSingleton<MarketBeatAnalystActionsSource>();
        services.AddSingleton<YahooAnalystActionsSource>();
        services.AddSingleton<IAnalystActionsSource>(sp => sp.GetRequiredService<MarketBeatAnalystActionsSource>());
        services.AddSingleton<IAnalystActionsSource>(sp => sp.GetRequiredService<YahooAnalystActionsSource>());
        services.AddSingleton<IAnalystSourceHealth, AnalystSourceHealth>();
        services.AddScoped<IAnalystUniverseService, AnalystUniverseService>();
        services.AddScoped<Core.Interfaces.IAnalystActionFeedReader, Infrastructure.Persistence.AnalystActionFeedReader>();

        // Valuation snapshot services (feature 030, US2). Both scoped: the valuation service depends on
        // the scoped IMarketDataService for price/staleness, and its crumb still caches within each
        // request/job scope (where the ticker and all its peers are resolved on one instance).
        services.AddScoped<IValuationDataService, YahooValuationDataService>();
        services.AddScoped<IValuationHistoryService, ValuationHistoryService>();

        // TrendForce press-center page source (feature 030, US3). A browser-like UA is required or the
        // page returns a challenge instead of the article list. Registered under INewsPageSource so the
        // news job resolves page sources as an IEnumerable.
        services.AddHttpClient(TrendForcePageSource.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
        });

        services.AddScoped<INewsPageSource, TrendForcePageSource>();

        // Research retrieval (feature 036). Embeddings go through a configurable OpenAI-compatible
        // endpoint; when disabled (default), indexing still stores chunks for lexical-only search.
        var retrievalOptions = config.GetSection(ResearchRetrievalOptions.SectionName)
            .Get<ResearchRetrievalOptions>() ?? new ResearchRetrievalOptions();
        services.AddHttpClient(ConfiguredEmbeddingService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, retrievalOptions.Embedding.TimeoutSeconds));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (compatible; FinanceSentry/1.0; +https://finance-sentry.local)");
        });
        services.AddSingleton<IEmbeddingService, ConfiguredEmbeddingService>();
        services.AddSingleton<IResearchChunker, ResearchChunker>();
        services.AddScoped<IResearchCorpusSourceReader, ResearchCorpusSourceReader>();
        services.AddScoped<IResearchIndexer, ResearchIndexer>();
        services.AddScoped<IResearchRetriever, ResearchRetriever>();

        services.AddScoped<NewsIngestionJob>();
        services.AddScoped<NewsSourceSeedJob>();
        services.AddScoped<AnalystActionsIngestionJob>();
        services.AddScoped<MacroCalendarSeedJob>();
        services.AddScoped<ThesisMonitorJob>();
        services.AddScoped<ThesisTrackRecordSnapshotJob>();
        services.AddScoped<CandidateExpiryJob>();
        services.AddScoped<OpportunityScanJob>();
        services.AddScoped<ResearchIndexingJob>();

        services.AddSingleton<IJobRegistrar, JobRegistrar>();

        return services;
    }
}
