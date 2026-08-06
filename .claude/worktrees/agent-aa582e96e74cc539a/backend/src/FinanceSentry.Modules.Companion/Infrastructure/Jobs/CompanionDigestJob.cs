namespace FinanceSentry.Modules.Companion.Infrastructure.Jobs;

using FinanceSentry.Modules.Companion.Application.Services;
using FinanceSentry.Modules.Companion.Domain;
using FinanceSentry.Modules.Companion.Domain.Repositories;
using Hangfire;
using Microsoft.Extensions.Logging;

/// <summary>
/// Daily digest trigger (feature 031, US3). Runs hourly; for each digest-mode user whose local hour
/// matches their digest hour AND who has events held for the digest, wakes the agent once to compose
/// the consolidated roll-up. The agent pulls the held events, delivers, and acks (so they don't
/// repeat). No held events ⇒ no wake (no forced empty message).
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 120)]
public sealed class CompanionDigestJob(
    INotificationSettingRepository settings,
    ICompanionEventRepository events,
    IAgentWakeDispatcher dispatcher,
    ILogger<CompanionDigestJob> logger)
{
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var digestUsers = await settings.ListByModeAsync(NotificationMode.Digest, ct);

        foreach (var setting in digestUsers)
        {
            if (QuietHours.LocalHour(setting.TimeZoneId, now) != setting.DigestHourLocal)
            {
                continue;
            }

            var held = await events.ListHeldForDigestAsync(setting.UserId, ct);
            if (held.Count == 0)
            {
                continue;
            }

            await dispatcher.WakeDigestAsync(setting.UserId, held.Count, ct);
            logger.LogInformation("Companion digest wake for {User}: {Count} held events", setting.UserId, held.Count);
        }
    }
}
