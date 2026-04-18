using System.Security.Cryptography;
using FinanceSentry.Modules.Auth.Application.Interfaces;
using FinanceSentry.Modules.Auth.Domain.Entities;
using FinanceSentry.Modules.Auth.Infrastructure.Persistence;
using MediatR;

namespace FinanceSentry.Modules.Auth.Application.Queries;

public class InitiateGoogleLoginQueryHandler(
    IGoogleOAuthService googleOAuthService,
    AuthDbContext db) : IRequestHandler<InitiateGoogleLoginQuery, string>
{
    public async Task<string> Handle(InitiateGoogleLoginQuery request, CancellationToken cancellationToken)
    {
        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        db.OAuthStates.Add(new OAuthState
        {
            Id = Guid.NewGuid(),
            State = state,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            IsUsed = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        return googleOAuthService.GetAuthorizationUrl(state);
    }
}
