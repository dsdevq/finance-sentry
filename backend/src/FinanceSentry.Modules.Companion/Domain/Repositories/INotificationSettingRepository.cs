namespace FinanceSentry.Modules.Companion.Domain.Repositories;

using FinanceSentry.Modules.Companion.Domain;

public interface INotificationSettingRepository
{
    /// <summary>The user's setting, or a defaulted (unsaved) instance when no row exists yet.</summary>
    Task<CompanionNotificationSetting> GetOrDefaultAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Insert or update the user's setting.</summary>
    Task UpsertAsync(CompanionNotificationSetting setting, CancellationToken ct = default);
}
