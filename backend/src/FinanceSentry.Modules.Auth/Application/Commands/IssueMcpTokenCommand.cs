using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Auth.Application.Interfaces;
using FinanceSentry.Modules.Auth.Domain.Entities;
using FinanceSentry.Modules.Auth.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public sealed record IssueMcpTokenCommand(string RawRefreshToken) : ICommand<IssueMcpTokenResult>;

public sealed record IssueMcpTokenResult(string McpToken, string Email, string UserId, DateTime ExpiresAt);

public sealed class IssueMcpTokenCommandHandler(
    IRefreshTokenService refreshTokenService,
    ITokenService tokenService,
    UserManager<ApplicationUser> userManager) : ICommandHandler<IssueMcpTokenCommand, IssueMcpTokenResult>
{
    public async Task<IssueMcpTokenResult> Handle(IssueMcpTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await refreshTokenService.ValidateAsync(request.RawRefreshToken, cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        var user = await userManager.FindByIdAsync(existing.UserId)
            ?? throw new InvalidRefreshTokenException();

        var token = tokenService.GenerateMcpToken(user);
        var expires = DateTime.UtcNow.AddDays(365);

        return new IssueMcpTokenResult(token, user.Email!, user.Id, expires);
    }
}
