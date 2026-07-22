namespace FinanceSentry.Modules.Companion.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Companion.API.Responses;
using FinanceSentry.Modules.Companion.Domain;
using FinanceSentry.Modules.Companion.Domain.Repositories;

/// <summary>
/// Sets a user's notification mode (feature 031, US1). Invalid mode is rejected and the previous mode
/// left unchanged (FR-005). Takes effect on the next event, no deploy (FR-003).
/// </summary>
public record SetNotificationModeCommand(Guid UserId, string Mode) : ICommand<NotificationModeDto>;

public class SetNotificationModeCommandHandler(INotificationSettingRepository settings)
    : ICommandHandler<SetNotificationModeCommand, NotificationModeDto>
{
    public async Task<NotificationModeDto> Handle(SetNotificationModeCommand cmd, CancellationToken ct)
    {
        if (!Enum.TryParse<NotificationMode>(cmd.Mode?.Trim(), ignoreCase: true, out var mode)
            || !Enum.IsDefined(mode))
        {
            throw new ArgumentException(
                $"Unknown notification mode '{cmd.Mode}'. Expected quiet | digest | scan | realtime.");
        }

        var setting = await settings.GetOrDefaultAsync(cmd.UserId, ct);
        setting.Mode = mode;
        await settings.UpsertAsync(setting, ct);

        return new NotificationModeDto(
            setting.Mode.ToString(),
            new QuietHoursDto(setting.QuietHoursStartLocal, setting.QuietHoursEndLocal, setting.TimeZoneId),
            setting.MaxProactivePerHour,
            setting.DigestHourLocal,
            DateTimeOffset.UtcNow);
    }
}
