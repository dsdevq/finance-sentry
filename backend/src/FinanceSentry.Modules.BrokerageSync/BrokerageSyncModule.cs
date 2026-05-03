namespace FinanceSentry.Modules.BrokerageSync;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Application.Services;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Jobs;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Persistence;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Persistence.Repositories;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class BrokerageSyncModule
{
    internal sealed class ModuleRegistrar : IModuleRegistrar
    {
        public void Register(IServiceCollection services, IConfiguration config)
            => services.AddBrokerageSyncModule(config);
    }

    private sealed class JobRegistrar : IJobRegistrar
    {
        public void RegisterJobs(IServiceProvider sp)
        {
            sp.GetRequiredService<IRecurringJobManager>()
                .AddOrUpdate<IBKRSyncJob>("ibkr-sync", job => job.ExecuteAsync(), "*/15 * * * *");
        }
    }

    public static IServiceCollection AddBrokerageSyncModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<BrokerageSyncDbContext>(
            o => o.UseNpgsql(config.GetConnectionString("Default")!, b => b.MigrationsHistoryTable("__EFMigrationsHistory", "public")));

        services.AddHttpClient<IBKRGatewayClient>(client =>
                client.DefaultRequestHeaders.UserAgent.ParseAdd("FinanceSentry/1.0"))
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var env = sp.GetRequiredService<IHostEnvironment>();
                var allowSelfSigned = config.GetValue<bool>("IBKR:AllowSelfSignedCert") || env.IsDevelopment();
                return new HttpClientHandler
                {
                    UseCookies = false,
                    ServerCertificateCustomValidationCallback = allowSelfSigned
                        ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                        : null,
                };
            });

        services.AddScoped<IBrokerAdapter, IBKRAdapter>();
        services.AddScoped<IIBKRCredentialRepository, IBKRCredentialRepository>();
        services.AddScoped<IBrokerageHoldingRepository, BrokerageHoldingRepository>();
        services.AddScoped<IBrokerageHoldingsReader, BrokerageHoldingsReader>();
        services.AddScoped<IBKRSyncJob>();

        services.AddSingleton<IJobRegistrar, JobRegistrar>();

        return services;
    }
}
