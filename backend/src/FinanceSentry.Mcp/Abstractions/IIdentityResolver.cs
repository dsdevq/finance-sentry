namespace FinanceSentry.Mcp.Abstractions;

/// <summary>
/// Resolves the caller's identity for stdio MCP requests. Backed by a long-lived JWT
/// (MCP_TOKEN env var) issued by POST /api/v1/auth/mcp-token.
/// </summary>
public interface IIdentityResolver
{
    /// <summary>
    /// Returns the configured user's ID, or null when MCP_TOKEN is missing/invalid.
    /// </summary>
    Guid? GetUserId();

    /// <summary>
    /// Returns the configured user's email if available; useful for logs.
    /// </summary>
    string? GetEmail();

    /// <summary>
    /// True when a valid MCP_TOKEN was loaded successfully.
    /// </summary>
    bool IsConfigured { get; }
}
