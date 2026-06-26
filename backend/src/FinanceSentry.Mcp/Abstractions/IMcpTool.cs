namespace FinanceSentry.Mcp.Abstractions;

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    bool IsReadOnly { get; }
    bool IsStub { get; }
    string? StubReason { get; }

    Task<McpToolResult> InvokeAsync(
        Guid userId,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken cancellationToken = default);
}
