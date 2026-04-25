namespace FinanceSentry.Modules.Auth.Application.Commands;

/// <summary>
/// Internal carrier between auth handlers and the AuthController.
/// Never crosses the wire — RawAccessToken / RawRefreshToken are placed into
/// httpOnly cookies by the controller; the JSON response body is just AuthResponse.
/// </summary>
public record AuthResult(AuthResponse Response, string RawRefreshToken, string RawAccessToken);
