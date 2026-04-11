using System.Security.Cryptography;
using System.Text;
using FinanceSentry.Modules.Auth.Application.Interfaces;
using FinanceSentry.Modules.Auth.Domain.Entities;
using FinanceSentry.Modules.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceSentry.Modules.Auth.Infrastructure.Services;

public class RefreshTokenService(AuthDbContext db) : IRefreshTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(30);

    public async Task<(string RawToken, RefreshToken Entity)> IssueAsync(
          string userId, CancellationToken cancellationToken = default)
    {
        var raw = GenerateRawToken();
        var hash = Hash(raw);
        var entity = new RefreshToken(userId, hash, DateTime.UtcNow.Add(TokenLifetime));

        db.RefreshTokens.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return (raw, entity);
    }

    public async Task<RefreshToken?> ValidateAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(rawToken);
        var entity = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        return entity?.IsValid() == true ? entity : null;
    }

    public async Task<(string RawToken, RefreshToken Entity)> RotateAsync(
        RefreshToken existing, CancellationToken cancellationToken = default)
    {
        existing.Revoke();

        var raw = GenerateRawToken();
        var hash = Hash(raw);
        var next = new RefreshToken(existing.UserId, hash, DateTime.UtcNow.Add(TokenLifetime));

        db.RefreshTokens.Add(next);
        await db.SaveChangesAsync(cancellationToken);

        return (raw, next);
    }

    public async Task RevokeAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var t in tokens)
            t.Revoke();

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateRawToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
