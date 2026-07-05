namespace FinanceSentry.Modules.Auth.Application.Interfaces;

public sealed record McpOAuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    DateTime ExpiresAt,
    string UserId,
    string Email,
    string Scope);
