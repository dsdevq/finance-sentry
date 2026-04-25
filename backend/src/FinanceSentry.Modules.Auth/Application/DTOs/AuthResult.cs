namespace FinanceSentry.Modules.Auth.Application.DTOs;

/// <summary>Internal result returned by Login/Register/Refresh handlers. Access token is set as a cookie by the controller; never sent in the JSON body.</summary>
public record AuthResult(AuthResponse Response, string RawRefreshToken, string RawAccessToken);
