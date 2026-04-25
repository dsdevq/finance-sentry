using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Auth.Application.DTOs;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public record GetMeQuery(string RawRefreshToken) : IQuery<GetMeResult>;

public record GetMeResult(AuthResponse Response, string RawAccessToken);
