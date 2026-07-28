namespace FinanceSentry.Modules.Rag.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public sealed class RagDbContextFactory : IDesignTimeDbContextFactory<RagDbContext>
{
    public RagDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("Default")
            ?? "Host=localhost;Database=finance_sentry;Username=finance_user;Password=finance_password";

        var options = new DbContextOptionsBuilder<RagDbContext>()
            .UseNpgsql(connectionString,
                b => b.MigrationsHistoryTable("__ef_migrations_history_rag", "public"))
            .Options;

        return new RagDbContext(options);
    }
}
