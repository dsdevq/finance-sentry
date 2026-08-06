namespace FinanceSentry.Modules.Companion.API.Responses;

public record QuietHoursDto(int? StartLocal, int? EndLocal, string? TimeZone);

/// <summary>The effective companion notification settings for a user (feature 031).</summary>
public record NotificationModeDto(
    string Mode,
    QuietHoursDto QuietHours,
    int MaxProactivePerHour,
    int DigestHourLocal,
    DateTimeOffset UpdatedAt);
