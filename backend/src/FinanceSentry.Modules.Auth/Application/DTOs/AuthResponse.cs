namespace FinanceSentry.Modules.Auth.Application.DTOs;

public record UserDto(string Id, string Email);

public record AuthResponse(UserDto User, DateTime ExpiresAt);
