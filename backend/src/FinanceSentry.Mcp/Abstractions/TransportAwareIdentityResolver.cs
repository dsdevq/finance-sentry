using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FinanceSentry.Core.Auth;
using Microsoft.AspNetCore.Http;

namespace FinanceSentry.Mcp.Abstractions;

public sealed class TransportAwareIdentityResolver(
    IHttpContextAccessor httpContextAccessor,
    LocalMcpSession localSession) : IIdentityResolver
{
    public Guid? GetUserId()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
            return principal.GetUserId();

        return localSession.GetPrincipal()?.GetUserId();
    }

    public string? GetEmail()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            return principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? principal.FindFirst(ClaimTypes.Email)?.Value;
        }

        principal = localSession.GetPrincipal();
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

            return localSession.GetPrincipal()?.GetUserId().HasValue == true;
        }
    }
}
