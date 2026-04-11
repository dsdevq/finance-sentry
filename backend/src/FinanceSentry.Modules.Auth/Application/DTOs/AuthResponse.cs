namespace FinanceSentry.Modules.Auth.Application.DTOs;

public record AuthResponse(string Token, DateTime ExpiresAt, string UserId);
