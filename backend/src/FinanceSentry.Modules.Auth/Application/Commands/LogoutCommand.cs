using MediatR;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public record LogoutCommand(string UserId) : IRequest;
