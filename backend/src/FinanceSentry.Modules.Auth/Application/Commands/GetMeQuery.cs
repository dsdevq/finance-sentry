using FinanceSentry.Modules.Auth.Application.DTOs;
using MediatR;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public record GetMeQuery(string RawRefreshToken) : IRequest<GetMeResult>;

public record GetMeResult(AuthResponse Response, string RawAccessToken);
