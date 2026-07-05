namespace FinanceSentry.Mcp.Abstractions;

/// <summary>
/// Resolves the caller's identity for MCP requests.
/// HTTP transport uses the authenticated HttpContext user.
/// stdio transport uses locally stored OAuth credentials.
/// </summary>
public interface IIdentityResolver
{
    /// <summary>
    /// Returns the configured user's ID, or null when no valid MCP identity is available.
    /// </summary>
    Guid? GetUserId();

    /// <summary>
    /// Returns the configured user's email if available; useful for logs.
    /// </summary>
    string? GetEmail();

    /// <summary>
    /// True when a valid MCP identity was loaded successfully.
    /// </summary>
    bool IsConfigured { get; }
}
