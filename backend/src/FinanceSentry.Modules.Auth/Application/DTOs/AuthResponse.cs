namespace FinanceSentry.Modules.Auth.Application.DTOs;

public record AuthResponse(string UserId, string Email, DateTime ExpiresAt);
