using FinanceSentry.Modules.Auth.Application.Interfaces;
using MediatR;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public class LogoutCommandHandler(IRefreshTokenService refreshTokenService) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await refreshTokenService.RevokeAsync(request.UserId, cancellationToken);
    }
}
