using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.Jobs;

public sealed class IBKRSyncJob(
    IIBKRCredentialRepository credentialRepository,
    ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult> syncHandler,
    IAlertGeneratorService alerts,
    IUserAlertPreferencesReader userPreferences,
    ILogger<IBKRSyncJob> logger)
{
    private const string Provider = "ibkr";

    public async Task ExecuteAsync()
    {
        var activeCredentials = await credentialRepository.GetAllActiveAsync();

        foreach (var credential in activeCredentials)
        {
            try
            {
                await syncHandler.Handle(new SyncIBKRHoldingsCommand(credential.UserId), CancellationToken.None);
                await TryResolveSyncFailureAsync(credential.UserId);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to sync IBKR holdings for user {UserId}",
                    credential.UserId);

                await TryGenerateSyncFailureAsync(credential.UserId, ex);
            }
        }
    }

    private async Task TryResolveSyncFailureAsync(Guid userId)
    {
        try
        {
            var prefs = await userPreferences.GetAsync(userId);
            if (prefs is null || !prefs.SyncFailureAlerts) return;
            await alerts.ResolveSyncFailureAlertAsync(userId, Provider, null);
        }
        catch
        {
            // best-effort
        }
    }

    private async Task TryGenerateSyncFailureAsync(Guid userId, Exception ex)
    {
        try
        {
            var prefs = await userPreferences.GetAsync(userId);
            if (prefs is null || !prefs.SyncFailureAlerts) return;
            await alerts.GenerateSyncFailureAlertAsync(userId, Provider, null, null, ex.GetType().Name);
        }
        catch
        {
            // best-effort
        }
    }
}
