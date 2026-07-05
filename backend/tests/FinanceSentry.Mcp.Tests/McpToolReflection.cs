using System.Reflection;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tests;

internal static class McpToolReflection
{
    private static readonly Assembly McpAssembly = typeof(FinanceSentry.Mcp.Tools.GetAccountSummaryTool).Assembly;

    public static IReadOnlyList<Type> GetToolTypes()
        => McpAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .OrderBy(t => t.Name)
            .ToList();

    public static IReadOnlyList<string> GetToolNames()
        => GetToolTypes()
            .SelectMany(GetToolNames)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<string> GetToolNames(Type toolType)
        => toolType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>())
            .Where(attribute => attribute?.Name is not null)
            .Select(attribute => attribute!.Name!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
}
