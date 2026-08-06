using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceSentry.Mcp.Tests.ContractTests;

/// <summary>
/// Builds the exact MCP service graph the host runs (<see cref="McpServiceRegistration"/>) and constructs
/// every registered tool from it. A missing handler registration — the class of bug that made
/// <c>get_pending_companion_events</c> return a 500 before #297 (Companion absent from the module list) —
/// fails here deterministically instead of at runtime. Construction only: no DB connection is opened.
/// </summary>
public sealed class ToolResolutionTests
{
    [Fact]
    public void EveryTool_ConstructsFromTheRealServiceGraph()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Port=5432;Database=x;Username=y;Password=z",
                ["ConnectionStrings:ReadOnly"] = "Host=localhost;Port=5432;Database=x;Username=y;Password=z",
                // Minimal config the module registrars validate at registration time.
                ["Deduplication:MasterKeyBase64"] = "MDEyMzQ1Njc4OUFCQ0RFRg==",
            })
            .Build();

        // The host builder registers IConfiguration automatically; mirror that so services that inject
        // it (e.g. LocalMcpCredentialStore) resolve exactly as they do at runtime.
        services.AddSingleton<IConfiguration>(config);
        McpServiceRegistration.RegisterShared(services, config);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var toolTypes = McpToolReflection.GetToolTypes();
        toolTypes.Should().NotBeEmpty("the MCP assembly must expose tool types");

        var failures = new List<string>();
        foreach (var toolType in toolTypes)
        {
            try
            {
                _ = scope.ServiceProvider.GetRequiredService(toolType);
            }
            catch (Exception ex)
            {
                failures.Add($"{toolType.Name}: {ex.GetType().Name} — {ex.Message}");
            }
        }

        failures.Should().BeEmpty(
            "every MCP tool must construct from the wired graph — an unresolved handler is the #297 bug class");
    }
}
