using FinanceSentry.Core.Cqrs;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public record GetMeQuery(string RawRefreshToken) : IQuery<GetMeResult>;

public record GetMeResult(MeResponse Response, string RawAccessToken);
