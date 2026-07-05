namespace FinanceSentry.Modules.Auth.Application.Interfaces;

public interface IMcpOAuthService
{
    Task<McpOAuthTokenResponse> ExchangeAuthorizationCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default);
    Task<McpOAuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}
