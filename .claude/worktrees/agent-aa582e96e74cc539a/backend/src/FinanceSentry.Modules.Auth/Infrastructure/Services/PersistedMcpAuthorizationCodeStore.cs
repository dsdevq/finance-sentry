using System.Security.Cryptography;
using System.Text;
using FinanceSentry.Modules.Auth.Application.Interfaces;
using FinanceSentry.Modules.Auth.Domain.Entities;
using FinanceSentry.Modules.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceSentry.Modules.Auth.Infrastructure.Services;

public sealed class PersistedMcpAuthorizationCodeStore(AuthDbContext db) : IMcpAuthorizationCodeStore
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);

    public async Task<string> IssueAsync(string userId, string email, string redirectUri, CancellationToken cancellationToken = default)
    {
        var rawCode = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        db.McpAuthorizationCodes.Add(new McpAuthorizationCode(
            userId,
            email,
            Hash(rawCode),
            redirectUri,
            DateTime.UtcNow.Add(CodeLifetime)));

        await db.SaveChangesAsync(cancellationToken);
        return rawCode;
    }

    public async Task<McpAuthorizationCodePayload?> ConsumeAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        var codeHash = Hash(code);
        var entity = await db.McpAuthorizationCodes
            .FirstOrDefaultAsync(x => x.CodeHash == codeHash, cancellationToken);

        if (entity is null || !entity.IsValidFor(redirectUri))
            return null;

        entity.Consume();
        await db.SaveChangesAsync(cancellationToken);

        return new McpAuthorizationCodePayload(entity.UserId, entity.Email, entity.RedirectUri);
    }

    private static string Hash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
