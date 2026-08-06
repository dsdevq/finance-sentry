using FluentAssertions;
using Xunit;

namespace FinanceSentry.Mcp.Tests.ContractTests;

public sealed class ToolMutationNamingContractTests
{
    [Fact]
    public void Only_AgreedTools_Use_ReservedMutationPrefixes()
    {
        string[] mutationPrefixes = ["create_", "update_", "delete_", "trigger_", "dismiss_"];

        var mutatingNames = McpToolReflection.GetToolNames()
            .Where(name => mutationPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        mutatingNames.Should().BeEquivalentTo(["delete_thesis"],
            because: "the current MCP surface intentionally exposes only one mutation with a reserved prefix");
    }

    [Fact]
    public void KnownMutationTools_DoNotUseReadPrefixes()
    {
        string[] readPrefixes = ["get_", "list_", "search_"];
        string[] writeToolNames = ["add_to_watchlist", "remove_from_watchlist", "save_thesis", "delete_thesis"];

        var incorrectlyNamed = writeToolNames
            .Where(name => readPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        incorrectlyNamed.Should().BeEmpty(
            because: "write tools should use explicit action verbs rather than read-oriented prefixes");
    }
}
