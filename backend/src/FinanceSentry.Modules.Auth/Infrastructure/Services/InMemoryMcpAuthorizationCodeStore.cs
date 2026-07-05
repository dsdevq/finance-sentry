using System.Security.Cryptography;
using FinanceSentry.Modules.Auth.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace FinanceSentry.Modules.Auth.Infrastructure.Services;

public sealed class InMemoryMcpAuthorizationCodeStore(IMemoryCache cache) : IMcpAuthorizationCodeStore
{
    private const int CodeLifetimeMinutes = 5;

    public Task<string> IssueAsync(string userId, string email, string redirectUri, CancellationToken cancellationToken = default)
    {
        var code = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        cache.Set(CacheKey(code), new McpAuthorizationCodePayload(userId, email, redirectUri), TimeSpan.FromMinutes(CodeLifetimeMinutes));
        return Task.FromResult(code);
    }

    public Task<McpAuthorizationCodePayload?> ConsumeAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        if (!cache.TryGetValue<McpAuthorizationCodePayload>(CacheKey(code), out var payload))
            return Task.FromResult<McpAuthorizationCodePayload?>(null);

        cache.Remove(CacheKey(code));

        if (!string.Equals(payload.RedirectUri, redirectUri, StringComparison.Ordinal))
            return Task.FromResult<McpAuthorizationCodePayload?>(null);

        return Task.FromResult<McpAuthorizationCodePayload?>(payload);
    }

    private static string CacheKey(string code) => $"mcp-auth-code:{code}";
}
