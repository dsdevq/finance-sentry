namespace FinanceSentry.Mcp.Abstractions;

public interface IMcpTool
{
    string Name { get; }

    string Description { get; }

    bool IsReadOnly { get; }

    bool IsStub { get; }

    Task<McpToolResult<object>> InvokeAsync(
        IReadOnlyDictionary<string, object?> args,
        McpToolContext context,
        CancellationToken ct);
}
