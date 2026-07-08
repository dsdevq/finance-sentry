namespace FinanceSentry.Modules.Research.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.Extensions.Options;

public record GetPostmortemPacketQuery(
    Guid UserId, DateOnly PeriodStart, DateOnly PeriodEnd) : IQuery<PostmortemPacketDto>;

/// <summary>
/// FR-008c: compiles every terminal event in a period with its decision notes, entry/exit prices,
/// and excess return, plus rejected-candidate counterfactuals from the same period (SC-004; empty
/// until 019 ships since no Candidate rows exist yet).
/// </summary>
public class GetPostmortemPacketQueryHandler(
    IThesisEventRepository eventRepo,
    IThesisPerformanceCalculator calculator,
    IOptions<FrictionConfig> frictionOptions)
    : IQueryHandler<GetPostmortemPacketQuery, PostmortemPacketDto>
{
    private static readonly ThesisEventType[] TerminalNonCounterfactualTypes =
    [
        ThesisEventType.Closed,
        ThesisEventType.Expired,
    ];

    public async Task<PostmortemPacketDto> Handle(GetPostmortemPacketQuery query, CancellationToken ct)
    {
        var periodEvents = await eventRepo.ListForPeriodAsync(
            query.UserId, query.PeriodStart, query.PeriodEnd, ct);

        var terminal = periodEvents
            .Where(e => TerminalNonCounterfactualTypes.Contains(e.EventType))
            .ToList();
        var rejected = periodEvents
            .Where(e => e.EventType == ThesisEventType.Rejected)
            .ToList();

        var entries = new List<PostmortemEntryDto>();
        foreach (var terminalEvent in terminal)
        {
            entries.Add(await BuildEntryAsync(query.UserId, terminalEvent, ct));
        }

        var counterfactuals = new List<PostmortemEntryDto>();
        foreach (var rejectedEvent in rejected)
        {
            counterfactuals.Add(await BuildEntryAsync(query.UserId, rejectedEvent, ct));
        }

        return new PostmortemPacketDto(query.PeriodStart, query.PeriodEnd, entries, counterfactuals);
    }

    private async Task<PostmortemEntryDto> BuildEntryAsync(
        Guid userId, ThesisEvent terminalEvent, CancellationToken ct)
    {
        var subjectEvents = await eventRepo.ListAsync(userId, terminalEvent.SubjectId, ct);
        var created = subjectEvents.FirstOrDefault(e => e.EventType == ThesisEventType.Created);

        decimal? excessReturnPct = null;
        if (created is not null)
        {
            var input = new ThesisPerformanceInput(
                terminalEvent.SubjectId,
                ThesisEventType.Created,
                created.Timestamp,
                created.SubjectPrice,
                created.BenchmarkPrice,
                terminalEvent.EventType,
                terminalEvent.Timestamp,
                terminalEvent.SubjectPrice,
                terminalEvent.BenchmarkPrice,
                terminalEvent.Ticker,
                frictionOptions.Value);

            var result = calculator.Calculate(input);
            excessReturnPct = result.ExcessReturnPct;
        }

        return new PostmortemEntryDto(
            terminalEvent.SubjectId,
            terminalEvent.Ticker,
            created?.DecisionNote,
            terminalEvent.DecisionNote,
            created?.SubjectPrice,
            terminalEvent.SubjectPrice,
            excessReturnPct,
            terminalEvent.EventType);
    }
}
