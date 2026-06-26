namespace FinanceSentry.Mcp.Abstractions;

public record McpToolResult<T>(bool Success, T? Value, string? Error) where T : class;

public static class McpToolResult
{
    public static McpToolResult<object> Success(object value) => new(true, value, null);

    public static McpToolResult<object> Error(string message) => new(false, null, message);
}
