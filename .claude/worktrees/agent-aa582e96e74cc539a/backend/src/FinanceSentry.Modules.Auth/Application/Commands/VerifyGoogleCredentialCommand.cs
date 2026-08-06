using FinanceSentry.Core.Cqrs;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public record VerifyGoogleCredentialRequest(string Credential);

public record VerifyGoogleCredentialCommand(string Credential) : ICommand<AuthResult>;
