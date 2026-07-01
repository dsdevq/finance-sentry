namespace FinanceSentry.Modules.BrokerageSync;

using Docker.DotNet;
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

        services.Configure<IBeamOptions>(config.GetSection(IBeamOptions.SectionName));

        services.AddHttpClient<IBKRGatewayClient>(client =>
                client.DefaultRequestHeaders.UserAgent.ParseAdd("FinanceSentry/1.0"))
            .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
            {
                UseCookies = false,
                // IBeam serves a self-signed cert; trust is anchored on the
                // private Docker network the API and gateway share.
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            });

        services.AddSingleton<IDockerClient>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<IBeamOptions>>().Value;
            return new DockerClientConfiguration(new Uri(options.DockerEndpoint)).CreateClient();
        });

        services.AddSingleton<IIBeamGatewayResolver, IBeamGatewayResolver>();
        services.AddSingleton<IIBeamContainerManager, IBeamContainerManager>();

        services.AddScoped<IBrokerAdapter, IBKRAdapter>();
        services.AddScoped<IIBKRCredentialRepository, IBKRCredentialRepository>();
        services.AddScoped<IBrokerageHoldingRepository, BrokerageHoldingRepository>();
        services.AddScoped<IBrokerageHoldingsReader, BrokerageHoldingsReader>();
        services.AddScoped<IBKRSyncJob>();

        services.AddSingleton<IJobRegistrar, JobRegistrar>();

        return services;
    }
}
