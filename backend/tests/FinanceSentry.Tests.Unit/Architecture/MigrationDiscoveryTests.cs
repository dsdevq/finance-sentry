namespace FinanceSentry.Tests.Unit.Architecture;

using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

/// <summary>
/// Every EF migration class must carry the two attributes EF Core uses to discover it:
/// <c>[DbContext]</c> (which context it belongs to) and <c>[Migration]</c> (its id).
/// <c>dotnet ef migrations add</c> emits them in the <c>.Designer.cs</c> partial; a
/// migration written by hand without them compiles, updates the model snapshot, and is
/// then silently skipped by <c>Database.Migrate()</c> — the model expects columns the
/// database never gets, and every query on that table fails at runtime.
/// </summary>
public class MigrationDiscoveryTests
{
    private static IEnumerable<Type> MigrationTypes()
    {
        var files = Directory.GetFiles(AppContext.BaseDirectory, "FinanceSentry.*.dll")
            .Where(f => !Path.GetFileName(f).StartsWith("FinanceSentry.Tests.", StringComparison.Ordinal));

        foreach (var file in files)
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(file);
            }
            catch (BadImageFormatException)
            {
                continue;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).ToArray()!;
            }

            foreach (var type in types)
            {
                if (type.IsClass && !type.IsAbstract && typeof(Migration).IsAssignableFrom(type))
                {
                    yield return type;
                }
            }
        }
    }

    [Fact]
    public void Every_migration_class_carries_the_attributes_EF_needs_to_discover_it()
    {
        var migrations = MigrationTypes().ToList();
        migrations.Should().NotBeEmpty("the module assemblies with migrations are referenced by this test project");

        var undiscoverable = migrations
            .Where(t => t.GetCustomAttribute<MigrationAttribute>() is null
                        || t.GetCustomAttribute<DbContextAttribute>() is null)
            .Select(t => t.FullName)
            .ToList();

        undiscoverable.Should().BeEmpty(
            "a migration without [Migration] and [DbContext] is never applied by Database.Migrate(); " +
            "add the .Designer.cs partial (or the attributes) for: {0}", string.Join(", ", undiscoverable));
    }

    [Fact]
    public void Migration_ids_are_unique_per_context()
    {
        var duplicates = MigrationTypes()
            .Select(t => (
                Context: t.GetCustomAttribute<DbContextAttribute>()?.ContextType,
                Id: t.GetCustomAttribute<MigrationAttribute>()?.Id))
            .Where(x => x.Context is not null && x.Id is not null)
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.Context!.Name}:{g.Key.Id}")
            .ToList();

        duplicates.Should().BeEmpty();
    }
}
