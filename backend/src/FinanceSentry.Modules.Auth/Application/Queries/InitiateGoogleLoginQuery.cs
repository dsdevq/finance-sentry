using MediatR;

namespace FinanceSentry.Modules.Auth.Application.Queries;

public record InitiateGoogleLoginQuery : IRequest<string>;
