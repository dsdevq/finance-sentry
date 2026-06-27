using FinanceSentry.Modules.Auth.Domain.Entities;

namespace FinanceSentry.Modules.Auth.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user);

    /// <summary>
    /// Issues a long-lived JWT scoped to MCP usage (aud=mcp). The MCP server validates
    /// this token locally against the shared JWT secret to resolve the caller's identity.
    /// </summary>
    string GenerateMcpToken(ApplicationUser user);
}
