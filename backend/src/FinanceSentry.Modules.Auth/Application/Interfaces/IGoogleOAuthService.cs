namespace FinanceSentry.Modules.Auth.Application.Interfaces;

public record GoogleUserInfo(string Sub, string Email, string? Name);

public interface IGoogleOAuthService
{
    string GetAuthorizationUrl(string state);
    Task<GoogleUserInfo> ExchangeCodeAsync(string code);
}
