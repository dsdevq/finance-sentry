using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FinanceSentry.Core.Auth;
using Microsoft.AspNetCore.Http;

namespace FinanceSentry.Mcp.Abstractions;

public sealed class TransportAwareIdentityResolver(
    IHttpContextAccessor httpContextAccessor,
    LocalMcpAccessTokenProvider localTokenProvider) : IIdentityResolver
{
    public Guid? GetUserId()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
            return principal.GetUserId();

        return localTokenProvider.GetCurrentPrincipal()?.GetUserId();
    }

    public string? GetEmail()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            return principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? principal.FindFirst(ClaimTypes.Email)?.Value;
        }

        principal = localTokenProvider.GetCurrentPrincipal();
        return principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? principal?.FindFirst(ClaimTypes.Email)?.Value;
    }

    public bool IsConfigured
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated == true)
                return principal.GetUserId().HasValue;

            return localTokenProvider.GetCurrentPrincipal()?.GetUserId().HasValue == true;
        }
    }
}
