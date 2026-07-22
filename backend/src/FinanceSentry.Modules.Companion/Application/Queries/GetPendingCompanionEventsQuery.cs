namespace FinanceSentry.Modules.Companion.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Companion.API.Responses;
using FinanceSentry.Modules.Companion.Domain;
using FinanceSentry.Modules.Companion.Domain.Repositories;

/// <summary>
/// The user's undelivered companion events for the agent to deliver (feature 031, US2). Read-only —
/// does NOT mark delivered; the agent acks explicitly after delivering.
/// </summary>
public record GetPendingCompanionEventsQuery(
    Guid UserId, int Limit, bool IncludeHeldForDigest) : IQuery<CompanionEventsResult>;

public class GetPendingCompanionEventsQueryHandler(
    ICompanionEventRepository events, INotificationSettingRepository settings)
    : IQueryHandler<GetPendingCompanionEventsQuery, CompanionEventsResult>
{
    private static readonly EventDisposition[] Undelivered =
        [EventDisposition.Pending, EventDisposition.Dispatched,
         EventDisposition.DeferredQuietHours, EventDisposition.SuppressedByRateLimit];

    public async Task<CompanionEventsResult> Handle(GetPendingCompanionEventsQuery query, CancellationToken ct)
    {
        var dispositions = query.IncludeHeldForDigest
            ? [.. Undelivered, EventDisposition.HeldForDigest]
            : Undelivered;

        var rows = await events.ListByDispositionAsync(query.UserId, dispositions, query.Limit, ct);
        var mode = (await settings.GetOrDefaultAsync(query.UserId, ct)).Mode;

        var dtos = rows
            .Select(e => new CompanionEventDto(
                e.Id, e.Kind.ToString(), e.Subject, e.Severity, e.Summary,
                e.ReferenceId, e.Disposition.ToString(), e.OccurredAt))
            .ToList();

        return new CompanionEventsResult(dtos, mode.ToString(), DateTimeOffset.UtcNow);
    }
}
