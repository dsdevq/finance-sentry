namespace FinanceSentry.Modules.Research;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain.Repositories;
using FinanceSentry.Modules.Research.Infrastructure.Jobs;
using FinanceSentry.Modules.Research.Infrastructure.Persistence;
using FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.Configure<OpportunityOptions>(config.GetSection(OpportunityOptions.SectionName));

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

        services.AddScoped<NewsIngestionJob>();
        services.AddScoped<MacroCalendarSeedJob>();
        services.AddScoped<ThesisMonitorJob>();
        services.AddScoped<ThesisTrackRecordSnapshotJob>();
        services.AddScoped<CandidateExpiryJob>();

        services.AddSingleton<IJobRegistrar, JobRegistrar>();

        return services;
    }
}
