namespace FinanceSentry.Mcp.Abstractions;

public sealed class McpToolResult
{
    public bool IsSuccess { get; private init; }
    public object? Payload { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static McpToolResult Success(object payload) =>
        new() { IsSuccess = true, Payload = payload };

    public static McpToolResult Error(string message) =>
        new() { IsSuccess = false, ErrorMessage = message };
}
