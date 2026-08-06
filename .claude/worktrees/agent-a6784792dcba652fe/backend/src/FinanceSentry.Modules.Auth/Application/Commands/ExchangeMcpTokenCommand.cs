using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Auth.Application.Interfaces;
using FinanceSentry.Modules.Auth.Domain.Exceptions;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public sealed record ExchangeMcpTokenCommand(
    string GrantType,
    string? Code = null,
    string? RedirectUri = null,
    string? RefreshToken = null) : ICommand<McpOAuthTokenResponse>;

public sealed class ExchangeMcpTokenCommandHandler(
    IMcpOAuthService mcpOAuthService) : ICommandHandler<ExchangeMcpTokenCommand, McpOAuthTokenResponse>
{
    public Task<McpOAuthTokenResponse> Handle(ExchangeMcpTokenCommand request, CancellationToken cancellationToken)
        => request.GrantType switch
        {
            "authorization_code" when !string.IsNullOrWhiteSpace(request.Code) && !string.IsNullOrWhiteSpace(request.RedirectUri)
                => mcpOAuthService.ExchangeAuthorizationCodeAsync(
                    request.Code,
                    McpLoopbackRedirectUri.Validate(request.RedirectUri),
                    cancellationToken),
            "refresh_token" when !string.IsNullOrWhiteSpace(request.RefreshToken)
                => mcpOAuthService.RefreshAsync(request.RefreshToken, cancellationToken),
            _ => throw new InvalidRefreshTokenException("Unsupported MCP token grant.")
        };
}
