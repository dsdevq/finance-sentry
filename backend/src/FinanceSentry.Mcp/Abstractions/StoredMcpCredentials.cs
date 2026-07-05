namespace FinanceSentry.Mcp.Abstractions;

public sealed record StoredMcpCredentials(
    string ApiBaseUrl,
    string FrontendBaseUrl,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserId,
    string Email,
    string Scope);
