namespace FinanceSentry.Modules.Retention;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Retention.Application;
using FinanceSentry.Modules.Retention.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Cross-cutting retention &amp; backup module (feature 024). Owns the <c>retention</c> schema and the
/// scheduled purge/backup/restore-verify jobs. Enforces policy for tables that are not already
/// governed by a bespoke module job; those remain untouched and are documented in the registry.
/// </summary>
public static class RetentionModule
{
    internal sealed class ModuleRegistrar : IModuleRegistrar
    {
        public void Register(IServiceCollection services, IConfiguration config)
            => services.AddRetentionModule(config);
    }

    public static IServiceCollection AddRetentionModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<RetentionDbContext>(
            o => o.UseNpgsql(
                config.GetConnectionString("Default")!,
                b => b.MigrationsHistoryTable("__ef_migrations_history_retention", "public")));

        services.Configure<RetentionOptions>(config.GetSection(RetentionOptions.SectionName));
        services.Configure<BackupOptions>(config.GetSection(BackupOptions.SectionName));

        return services;
    }
}
