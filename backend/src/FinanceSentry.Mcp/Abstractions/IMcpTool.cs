namespace FinanceSentry.Mcp.Abstractions;

public interface IMcpTool
{
    string Name { get; }

    bool IsReadOnly { get; }

    bool IsStub { get; }

    Task<McpToolResult<object>> InvokeAsync(IReadOnlyDictionary<string, object?> args, CancellationToken ct);
}
