namespace FinanceSentry.Mcp.Abstractions;

public record McpToolResult<T>(bool Success, T? Value, string? Error) where T : class;
