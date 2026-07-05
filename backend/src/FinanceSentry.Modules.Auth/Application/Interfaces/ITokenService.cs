using FinanceSentry.Modules.Auth.Domain.Entities;

namespace FinanceSentry.Modules.Auth.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user);
    (string Token, DateTime ExpiresAt) GenerateMcpAccessToken(ApplicationUser user);
}
