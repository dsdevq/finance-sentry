namespace FinanceSentry.Modules.Research.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;

/// <summary>
/// Query analyst/street actions market-wide. All filters optional; <see cref="Ticker"/> null =
/// whole-universe query. <see cref="ActionType"/> is a case-insensitive enum name.
/// </summary>
public record GetAnalystActionsQuery(
    string? Ticker,
    DateOnly Since,
    string? ActionType,
    int Limit) : IQuery<AnalystActionsResult>;

public class GetAnalystActionsQueryHandler(
    IAnalystActionRepository actions,
    IAnalystUniverseRepository universe)
    : IQueryHandler<GetAnalystActionsQuery, AnalystActionsResult>
{
    public async Task<AnalystActionsResult> Handle(GetAnalystActionsQuery query, CancellationToken ct)
    {
        AnalystActionType? typeFilter = null;
        if (!string.IsNullOrWhiteSpace(query.ActionType)
            && Enum.TryParse<AnalystActionType>(query.ActionType, ignoreCase: true, out var parsed))
        {
            typeFilter = parsed;
        }

        var rows = await actions.QueryAsync(query.Ticker, query.Since, typeFilter, query.Limit, ct);

        var coverage = "marketWide";
        if (!string.IsNullOrWhiteSpace(query.Ticker))
        {
            coverage = await universe.IsInUniverseAsync(query.Ticker, ct) ? "inUniverse" : "notInUniverse";
        }

        var dtos = rows
            .Select(a => new AnalystActionDto(
                a.Ticker, a.Firm, a.ActionType.ToString(),
                a.PriorRating, a.NewRating, a.PriorTarget, a.NewTarget,
                a.ActionDate, a.Source, a.SourceUrl, a.IngestedAt))
            .ToList();

        return new AnalystActionsResult(dtos, coverage, DateTimeOffset.UtcNow);
    }
}
