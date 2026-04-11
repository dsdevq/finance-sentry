using FinanceSentry.Modules.Auth.Application.DTOs;
using FinanceSentry.Modules.Auth.Application.Interfaces;
using FinanceSentry.Modules.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public class RefreshCommandHandler(
    IRefreshTokenService refreshTokenService,
    ITokenService tokenService,
    UserManager<ApplicationUser> userManager) : IRequestHandler<RefreshCommand, AuthResult>
{
    public async Task<AuthResult> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var existing = await refreshTokenService.ValidateAsync(request.RawRefreshToken, cancellationToken) ?? throw new UnauthorizedAccessException("INVALID_REFRESH_TOKEN");

        var user = await userManager.FindByIdAsync(existing.UserId) ?? throw new UnauthorizedAccessException("INVALID_REFRESH_TOKEN");
        var (newRaw, _) = await refreshTokenService.RotateAsync(existing, cancellationToken);

        var accessToken = tokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        return new AuthResult(new AuthResponse(accessToken, expiresAt, user.Id), newRaw);
    }
}
