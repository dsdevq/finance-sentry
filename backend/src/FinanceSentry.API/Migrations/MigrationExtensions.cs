namespace FinanceSentry.API.Migrations;

using FinanceSentry.Modules.Auth.Infrastructure.Persistence;
using FinanceSentry.Modules.BankSync.Infrastructure.Categorization;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using FinanceSentry.Modules.CryptoSync.Infrastructure.Persistence;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Persistence;
using FinanceSentry.Modules.Alerts.Infrastructure.Persistence;
using FinanceSentry.Modules.Budgets.Infrastructure.Persistence;
using FinanceSentry.Modules.Subscriptions.Infrastructure.Persistence;
using FinanceSentry.Modules.Wealth.Infrastructure.Persistence;
using FinanceSentry.Modules.Research.Infrastructure.Persistence;
using FinanceSentry.Modules.Radar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class MigrationExtensions
{
    public static WebApplication MigrateAllModules(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;

        MigrateContext<AuthDbContext>(sp, app.Logger);
        MigrateContext<BankSyncDbContext>(sp, app.Logger);
        MigrateContext<CryptoSyncDbContext>(sp, app.Logger);
        MigrateContext<BrokerageSyncDbContext>(sp, app.Logger);
        MigrateContext<AlertsDbContext>(sp, app.Logger);
        MigrateContext<BudgetsDbContext>(sp, app.Logger);
        MigrateContext<SubscriptionsDbContext>(sp, app.Logger);
        MigrateContext<WealthDbContext>(sp, app.Logger);
        MigrateContext<ResearchDbContext>(sp, app.Logger);
        MigrateContext<RadarDbContext>(sp, app.Logger);

        SeedBankSyncCategories(sp, app.Logger);

        return app;
    }

    private static void SeedBankSyncCategories(IServiceProvider sp, ILogger logger)
    {
        try
        {
            CategorySeeder.Seed(sp.GetRequiredService<BankSyncDbContext>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Category reference seeding failed. Startup will continue.");
        }
    }

    private static void MigrateContext<TContext>(IServiceProvider sp, ILogger logger)
        where TContext : DbContext
    {
        try
        {
            sp.GetRequiredService<TContext>().Database.Migrate();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration failed for {Context}. Startup will continue.", typeof(TContext).Name);
        }
    }
}
