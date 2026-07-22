namespace FinanceSentry.Modules.Companion.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Companion.API.Responses;
using FinanceSentry.Modules.Companion.Domain.Repositories;

/// <summary>Reads a user's effective companion notification settings (feature 031, US1).</summary>
public record GetNotificationModeQuery(Guid UserId) : IQuery<NotificationModeDto>;

public class GetNotificationModeQueryHandler(INotificationSettingRepository settings)
    : IQueryHandler<GetNotificationModeQuery, NotificationModeDto>
{
    public async Task<NotificationModeDto> Handle(GetNotificationModeQuery query, CancellationToken ct)
    {
        var s = await settings.GetOrDefaultAsync(query.UserId, ct);
        return new NotificationModeDto(
            s.Mode.ToString(),
            new QuietHoursDto(s.QuietHoursStartLocal, s.QuietHoursEndLocal, s.TimeZoneId),
            s.MaxProactivePerHour,
            s.DigestHourLocal,
            s.UpdatedAt);
    }
}
