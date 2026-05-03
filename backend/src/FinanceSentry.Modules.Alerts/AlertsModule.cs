namespace FinanceSentry.Modules.Alerts;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Alerts.Application.Services;
using FinanceSentry.Modules.Alerts.Domain.Repositories;
using FinanceSentry.Modules.Alerts.Infrastructure.Jobs;
using FinanceSentry.Modules.Alerts.Infrastructure.Persistence;
using FinanceSentry.Modules.Alerts.Infrastructure.Persistence.Repositories;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class AlertsModule
{
    internal sealed class ModuleRegistrar : IModuleRegistrar
    {
        public void Register(IServiceCollection services, IConfiguration config)
            => services.AddAlertsModule(config);
    }

    private sealed class JobRegistrar : IJobRegistrar
    {
        public void RegisterJobs(IServiceProvider sp)
        {
            sp.GetRequiredService<IRecurringJobManager>()
                .AddOrUpdate<AlertPurgeJob>("alert-purge", job => job.ExecuteAsync(CancellationToken.None), Cron.Monthly());
        }
    }

    public static IServiceCollection AddAlertsModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AlertsDbContext>(
            o => o.UseNpgsql(config.GetConnectionString("Default")!, b => b.MigrationsHistoryTable("__EFMigrationsHistory", "public")));

        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IAlertGeneratorService, AlertGeneratorService>();
        services.AddScoped<AlertPurgeJob>();

        services.AddSingleton<IJobRegistrar, JobRegistrar>();

        return services;
    }
}
