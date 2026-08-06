using FinanceSentry.Core.Cqrs;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public record RegisterCommand(string Email, string Password) : ICommand<AuthResult>;
