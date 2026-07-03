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
        services.AddScoped<IThesisRepository, ThesisRepository>();
        services.AddScoped<IQuoteCacheRepository, QuoteCacheRepository>();
        services.AddScoped<INewsRepository, NewsRepository>();
        services.AddScoped<IMacroCalendarRepository, MacroCalendarRepository>();

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

        services.AddScoped<IMarketDataService, YahooMarketDataService>();
        services.AddScoped<IMarketNewsService, RssMarketNewsService>();
        services.AddScoped<IMacroCalendarService, MacroCalendarService>();

        services.AddScoped<NewsIngestionJob>();
        services.AddScoped<MacroCalendarSeedJob>();

        services.AddSingleton<IJobRegistrar, JobRegistrar>();

        return services;
    }
}
