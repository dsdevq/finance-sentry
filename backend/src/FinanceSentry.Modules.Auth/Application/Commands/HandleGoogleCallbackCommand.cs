using MediatR;
using FinanceSentry.Modules.Auth.Application.DTOs;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public record HandleGoogleCallbackCommand(string Code, string State) : IRequest<AuthResult>;
