using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Auth.Application.Interfaces;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public sealed record RevokeMcpTokenCommand(string? RefreshToken) : ICommand<Unit>;

public sealed class RevokeMcpTokenCommandHandler(
    IMcpOAuthService mcpOAuthService) : ICommandHandler<RevokeMcpTokenCommand, Unit>
{
    public async Task<Unit> Handle(RevokeMcpTokenCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            await mcpOAuthService.RevokeAsync(request.RefreshToken, cancellationToken);

        return Unit.Value;
    }
}
