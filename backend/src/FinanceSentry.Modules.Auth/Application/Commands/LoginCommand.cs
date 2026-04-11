using FinanceSentry.Modules.Auth.Application.DTOs;
using MediatR;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
