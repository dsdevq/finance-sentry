namespace FinanceSentry.Modules.Companion.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Companion.Application.Services;
using FinanceSentry.Modules.Companion.Domain;
using FinanceSentry.Modules.Companion.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public class NotificationSettingRepository(CompanionDbContext db, IOptions<CompanionOptions> options)
    : INotificationSettingRepository
{
    private readonly CompanionOptions _options = options.Value;

    public async Task<CompanionNotificationSetting> GetOrDefaultAsync(Guid userId, CancellationToken ct = default)
    {
        var existing = await db.NotificationSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (existing is not null)
        {
            return existing;
        }

        return new CompanionNotificationSetting
        {
            UserId = userId,
            Mode = NotificationMode.Scan,
            TimeZoneId = _options.DefaultTimeZoneId,
            QuietHoursStartLocal = _options.QuietHoursStartLocal,
            QuietHoursEndLocal = _options.QuietHoursEndLocal,
            MaxProactivePerHour = _options.MaxProactivePerHour,
            DigestHourLocal = _options.DigestHourLocal,
        };
    }

    public async Task<IReadOnlyList<CompanionNotificationSetting>> ListByModeAsync(
        NotificationMode mode, CancellationToken ct = default)
        => await db.NotificationSettings.AsNoTracking().Where(s => s.Mode == mode).ToListAsync(ct);

    public async Task UpsertAsync(CompanionNotificationSetting setting, CancellationToken ct = default)
    {
        var existing = await db.NotificationSettings.FirstOrDefaultAsync(s => s.UserId == setting.UserId, ct);
        if (existing is null)
        {
            setting.UpdatedAt = DateTimeOffset.UtcNow;
            db.NotificationSettings.Add(setting);
        }
        else
        {
            existing.Mode = setting.Mode;
            existing.QuietHoursStartLocal = setting.QuietHoursStartLocal;
            existing.QuietHoursEndLocal = setting.QuietHoursEndLocal;
            existing.TimeZoneId = setting.TimeZoneId;
            existing.MaxProactivePerHour = setting.MaxProactivePerHour;
            existing.DigestHourLocal = setting.DigestHourLocal;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
