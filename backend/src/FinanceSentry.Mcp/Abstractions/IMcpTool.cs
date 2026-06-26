namespace FinanceSentry.Mcp.Abstractions;

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    bool IsReadOnly { get; }
    bool IsStub { get; }

    Task<McpToolResult> InvokeAsync(
        McpToolContext context,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken);
}
