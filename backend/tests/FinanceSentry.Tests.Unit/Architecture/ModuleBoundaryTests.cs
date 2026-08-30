namespace FinanceSentry.Tests.Unit.Architecture;

using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

/// <summary>
/// Enforces the modular-monolith boundary: a module may depend on Core and Infrastructure,
/// never on another module. Cross-module reads go through Core read ports (039 pattern) wired
/// in FinanceSentry.Integration. Checked at the .csproj level — without a project reference,
/// no compile-time coupling between modules is possible.
/// </summary>
public class ModuleBoundaryTests
{
    private const string ModulePrefix = "FinanceSentry.Modules.";

    [Fact]
    public void Modules_DoNotReferenceOtherModules()
    {
        var srcDir = Path.Combine(FindBackendRoot(), "src");
        var moduleProjects = Directory.GetDirectories(srcDir, $"{ModulePrefix}*")
            .SelectMany(d => Directory.GetFiles(d, "*.csproj"))
            .ToList();

        moduleProjects.Should().NotBeEmpty("the module projects must be discoverable for the boundary check to mean anything");

        var violations = new List<string>();
        foreach (var project in moduleProjects)
        {
            var moduleName = Path.GetFileNameWithoutExtension(project);
            var references = Regex.Matches(File.ReadAllText(project), "ProjectReference\\s+Include=\"([^\"]+)\"")
                .Select(m => Path.GetFileNameWithoutExtension(m.Groups[1].Value));

            violations.AddRange(references
                .Where(r => r.StartsWith(ModulePrefix, StringComparison.Ordinal) && r != moduleName)
                .Select(r => $"{moduleName} -> {r}"));
        }

        violations.Should().BeEmpty(
            "modules must stay decoupled; expose the data as a Core read port implemented by the owning module instead of referencing it directly");
    }

    private static string FindBackendRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FinanceSentry.sln")))
            dir = dir.Parent;

        return dir?.FullName
            ?? throw new InvalidOperationException("FinanceSentry.sln not found above the test bin directory.");
    }
}
