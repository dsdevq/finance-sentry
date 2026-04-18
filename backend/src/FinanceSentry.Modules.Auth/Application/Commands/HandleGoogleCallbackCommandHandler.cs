using FinanceSentry.Modules.Auth.Application.DTOs;
using FinanceSentry.Modules.Auth.Application.Interfaces;
using FinanceSentry.Modules.Auth.Domain.Entities;
using FinanceSentry.Modules.Auth.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public class HandleGoogleCallbackCommandHandler(
    IGoogleOAuthService googleOAuthService,
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService,
    AuthDbContext db) : IRequestHandler<HandleGoogleCallbackCommand, AuthResult>
{
    public async Task<AuthResult> Handle(HandleGoogleCallbackCommand request, CancellationToken cancellationToken)
    {
        var oauthState = await db.OAuthStates
            .FirstOrDefaultAsync(s => s.State == request.State, cancellationToken);

        if (oauthState is null || oauthState.IsUsed || oauthState.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("INVALID_OAUTH_STATE");

        oauthState.IsUsed = true;
        await db.SaveChangesAsync(cancellationToken);

        var googleUser = await googleOAuthService.ExchangeCodeAsync(request.Code);

        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleUser.Sub, cancellationToken);

        if (user is null)
        {
            user = await userManager.FindByEmailAsync(googleUser.Email);
            if (user is not null)
            {
                user.GoogleId = googleUser.Sub;
                await userManager.UpdateAsync(user);
            }
        }

        if (user is null)
        {
            user = new ApplicationUser
            {
                Email = googleUser.Email,
                UserName = googleUser.Email,
                EmailConfirmed = true,
                GoogleId = googleUser.Sub
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }

        var accessToken = tokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(60);
        var (rawRefreshToken, _) = await refreshTokenService.IssueAsync(user.Id, cancellationToken);

        return new AuthResult(new AuthResponse(accessToken, expiresAt, user.Id), rawRefreshToken);
    }
}
