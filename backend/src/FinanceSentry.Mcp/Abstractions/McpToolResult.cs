namespace FinanceSentry.Mcp.Abstractions;

public sealed class McpToolResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotAvailable { get; init; }
    public object? Payload { get; init; }
    public string? ErrorMessage { get; init; }

    private McpToolResult() { }

    public static McpToolResult Success(object payload) =>
        new() { IsSuccess = true, Payload = payload };

    public static McpToolResult Error(string message) =>
        new() { IsSuccess = false, ErrorMessage = message };

    public static McpToolResult NotYetAvailable(string reason) =>
        new() { IsSuccess = false, IsNotAvailable = true, ErrorMessage = reason };
}
