namespace FinanceSentry.Modules.Auth.Application.DTOs;

/// <summary>Internal result returned by Login/Register handlers that includes both the access token response and the raw refresh token for cookie setting.</summary>
public record AuthResult(AuthResponse Response, string RawRefreshToken);
