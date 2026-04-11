using FinanceSentry.Modules.Auth.Application.DTOs;
using MediatR;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public record RegisterCommand(string Email, string Password) : IRequest<AuthResponse>;
