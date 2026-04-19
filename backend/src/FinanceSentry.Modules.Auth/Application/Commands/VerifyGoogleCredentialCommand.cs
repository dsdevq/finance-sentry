using FinanceSentry.Modules.Auth.Application.DTOs;
using MediatR;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public record VerifyGoogleCredentialCommand(string Credential) : IRequest<AuthResult>;
