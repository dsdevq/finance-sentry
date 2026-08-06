namespace FinanceSentry.Modules.Companion.Application.Services;

using FinanceSentry.Modules.Companion.Domain;

/// <summary>Pure quiet-hours evaluation for realtime deferral (feature 031). Handles wrap-around windows.</summary>
public static class QuietHours
{
    public static bool IsWithin(CompanionNotificationSetting setting, DateTimeOffset nowUtc)
    {
        if (setting.QuietHoursStartLocal is not { } start || setting.QuietHoursEndLocal is not { } end)
        {
            return false;
        }

        var localHour = LocalHour(setting.TimeZoneId, nowUtc);
        return start <= end
            ? localHour >= start && localHour < end          // same-day window, e.g. 1–5
            : localHour >= start || localHour < end;          // wrap-around, e.g. 22–7
    }

    /// <summary>Hour-of-day (0–23) in the given IANA tz; falls back to UTC on an unknown/invalid tz.</summary>
    public static int LocalHour(string? timeZoneId, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return nowUtc.UtcDateTime.Hour;
        }

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTime(nowUtc, tz).Hour;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return nowUtc.UtcDateTime.Hour;
        }
    }
}
