using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Auth.Application.DTOs;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public record RegisterCommand(string Email, string Password) : ICommand<AuthResult>;
