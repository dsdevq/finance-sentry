using System.Security.Claims;

namespace FinanceSentry.Core.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? principal.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    public static Guid RequireUserId(this ClaimsPrincipal principal)
    {
        return principal.GetUserId()
            ?? throw new UnauthorizedAccessException("UNAUTHORIZED");
    }
}
