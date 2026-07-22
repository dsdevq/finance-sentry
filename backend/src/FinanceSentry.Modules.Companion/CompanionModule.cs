namespace FinanceSentry.Modules.Companion;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Companion.Application.Services;
using FinanceSentry.Modules.Companion.Domain.Repositories;
using FinanceSentry.Modules.Companion.Infrastructure.Persistence;
using FinanceSentry.Modules.Companion.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class CompanionModule
{
    internal sealed class ModuleRegistrar : IModuleRegistrar
    {
        public void Register(IServiceCollection services, IConfiguration config)
            => services.AddCompanionModule(config);
    }

    public static IServiceCollection AddCompanionModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<CompanionDbContext>(
            o => o.UseNpgsql(
                config.GetConnectionString("Default")!,
                b => b.MigrationsHistoryTable("__ef_migrations_history_companion", "public")));

        services.Configure<CompanionOptions>(config.GetSection(CompanionOptions.SectionName));

        services.AddScoped<INotificationSettingRepository, NotificationSettingRepository>();
        services.AddScoped<ICompanionEventRepository, CompanionEventRepository>();
        services.AddScoped<ICompanionCaptureStateRepository, CompanionCaptureStateRepository>();

        return services;
    }
}
