namespace FinanceSentry.Modules.Alerts;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Alerts.Application.Services;
using FinanceSentry.Modules.Alerts.Domain.Repositories;
using FinanceSentry.Modules.Alerts.Infrastructure.Jobs;
using FinanceSentry.Modules.Alerts.Infrastructure.Persistence;
using FinanceSentry.Modules.Alerts.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class AlertsModule
{
    public static IServiceCollection AddAlertsModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AlertsDbContext>(
            o => o.UseNpgsql(config.GetConnectionString("Default")!));

        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IAlertGeneratorService, AlertGeneratorService>();
        services.AddScoped<AlertPurgeJob>();

        return services;
    }
}
