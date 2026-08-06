namespace FinanceSentry.Modules.Alerts.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Alerts.API.Responses;
using FinanceSentry.Modules.Alerts.Domain.Repositories;

public record GetUnreadCountQuery(Guid UserId) : IQuery<UnreadCountResponse>;

public class GetUnreadCountQueryHandler(IAlertRepository alerts) : IQueryHandler<GetUnreadCountQuery, UnreadCountResponse>
{
    private readonly IAlertRepository _alerts = alerts;

    public async Task<UnreadCountResponse> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var count = await _alerts.GetUnreadCountAsync(request.UserId, cancellationToken);
        return new UnreadCountResponse(count);
    }
}
