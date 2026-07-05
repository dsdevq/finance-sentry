using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FinanceSentry.Core.Auth;
using Microsoft.AspNetCore.Http;

namespace FinanceSentry.Mcp.Abstractions;

public sealed class TransportAwareIdentityResolver(
    IHttpContextAccessor httpContextAccessor,
    JwtIdentityResolver startupTokenResolver) : IIdentityResolver
{
    public Guid? GetUserId()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
            return principal.GetUserId();

        return startupTokenResolver.GetUserId();
    }

    public string? GetEmail()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            return principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? principal.FindFirst(ClaimTypes.Email)?.Value;
        }

        return startupTokenResolver.GetEmail();
    }

    public bool IsConfigured
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated == true)
                return principal.GetUserId().HasValue;

            return startupTokenResolver.IsConfigured;
        }
    }
}
