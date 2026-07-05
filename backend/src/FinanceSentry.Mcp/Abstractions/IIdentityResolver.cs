namespace FinanceSentry.Mcp.Abstractions;

/// <summary>
/// Resolves the caller's identity for MCP requests.
/// HTTP transport uses the authenticated HttpContext user.
/// stdio transport falls back to a locally provisioned MCP_TOKEN.
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
