namespace FinanceSentry.Modules.Retention.Tests;

using FinanceSentry.Modules.Retention.Application;
using FinanceSentry.Modules.Retention.Infrastructure.Backup;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;

/// <summary>
/// FR-006 + spec edge case (feature 024): restore verification must target an isolated scratch database
/// and never write to the production database.
/// </summary>
public sealed class RestoreIsolationTests
{
    private const string AppDatabase = "finance_sentry";

    private static PgDumpRunner BuildRunner()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    $"Host=postgres;Port=5432;Database={AppDatabase};Username=finance_user;Password=secret",
            })
            .Build();
        return new PgDumpRunner(config, Options.Create(new BackupOptions()));
    }

    [Fact]
    public void Scratch_connection_targets_scratch_db_not_production()
    {
        var scratch = "restore_verify_20260806020000";
        var built = new NpgsqlConnectionStringBuilder(BuildRunner().ScratchConnectionString(scratch));

        built.Database.Should().Be(scratch);
        built.Database.Should().NotBe(AppDatabase, "the restore drill must never open the production database");
    }

    [Fact]
    public void Scratch_connection_preserves_host_and_credentials()
    {
        var built = new NpgsqlConnectionStringBuilder(BuildRunner().ScratchConnectionString("restore_verify_x"));

        built.Host.Should().Be("postgres");
        built.Username.Should().Be("finance_user");
        built.Port.Should().Be(5432);
    }
}
