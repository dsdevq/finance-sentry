using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.CryptoSync.Application.Commands;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.CryptoSync.Infrastructure.Jobs;

public sealed class BinanceSyncJob(
    IBinanceCredentialRepository credentialRepository,
    ICommandHandler<SyncBinanceHoldingsCommand, SyncBinanceHoldingsResult> syncHandler,
    IAlertGeneratorService alerts,
    IUserAlertPreferencesReader userPreferences,
    ILogger<BinanceSyncJob> logger)
{
    private const string Provider = "binance";

    public async Task ExecuteAsync()
    {
        var activeCredentials = await credentialRepository.GetAllActiveAsync();

        foreach (var credential in activeCredentials)
        {
            try
            {
                await syncHandler.Handle(new SyncBinanceHoldingsCommand(credential.UserId), CancellationToken.None);
                await TryResolveSyncFailureAsync(credential.UserId);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to sync Binance holdings for user {UserId}",
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
