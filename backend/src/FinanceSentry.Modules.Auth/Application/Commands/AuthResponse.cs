namespace FinanceSentry.Modules.Auth.Application.Commands;

public record UserDto(string Id, string Email);

public record AuthResponse(UserDto User, DateTime ExpiresAt);
