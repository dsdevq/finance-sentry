namespace FinanceSentry.Modules.Research.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Services;

public record GetMacroCalendarQuery(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<string>? Regions,
    string? MinImportance) : IQuery<IReadOnlyList<MacroEventDto>>;

public class GetMacroCalendarQueryHandler(IMacroCalendarService svc)
    : IQueryHandler<GetMacroCalendarQuery, IReadOnlyList<MacroEventDto>>
{
    public async Task<IReadOnlyList<MacroEventDto>> Handle(GetMacroCalendarQuery query, CancellationToken ct)
    {
        var items = await svc.QueryAsync(query.From, query.To, query.Regions, query.MinImportance, ct);
        return items
            .Select(e => new MacroEventDto(
                e.Id, e.EventDate, e.EventTime, e.Event, e.Region, e.Importance, e.Source))
            .ToList();
    }
}
