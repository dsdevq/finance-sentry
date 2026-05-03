namespace FinanceSentry.Modules.Budgets;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Budgets.Application.Services;
using FinanceSentry.Modules.Budgets.Domain.Repositories;
using FinanceSentry.Modules.Budgets.Infrastructure.Persistence;
using FinanceSentry.Modules.Budgets.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class BudgetsModule
{
    internal sealed class ModuleRegistrar : IModuleRegistrar
    {
        public void Register(IServiceCollection services, IConfiguration config)
            => services.AddBudgetsModule(config);
    }

    public static IServiceCollection AddBudgetsModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<BudgetsDbContext>(
            o => o.UseNpgsql(config.GetConnectionString("Default")!, b => b.MigrationsHistoryTable("__EFMigrationsHistory", "public")));

        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<ICategoryNormalizationService, CategoryNormalizationService>();

        return services;
    }
}
