namespace FinanceSentry.Modules.Auth.Application.Interfaces;

public record GoogleUserInfo(string GoogleId, string Email, string? DisplayName);

public interface IGoogleCredentialVerifier
{
    Task<GoogleUserInfo> VerifyAsync(string credential);
}
