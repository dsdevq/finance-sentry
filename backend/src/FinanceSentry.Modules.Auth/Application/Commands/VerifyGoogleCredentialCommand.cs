using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Auth.Application.DTOs;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public record VerifyGoogleCredentialCommand(string Credential) : ICommand<AuthResult>;
