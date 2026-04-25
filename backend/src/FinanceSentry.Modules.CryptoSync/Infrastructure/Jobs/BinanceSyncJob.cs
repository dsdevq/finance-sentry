using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.CryptoSync.Application.Commands;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.CryptoSync.Infrastructure.Jobs;

public sealed class BinanceSyncJob(
    IBinanceCredentialRepository credentialRepository,
    ICommandHandler<SyncBinanceHoldingsCommand, SyncBinanceHoldingsResult> syncHandler,
    ILogger<BinanceSyncJob> logger)
{
    public async Task ExecuteAsync()
    {
        var activeCredentials = await credentialRepository.GetAllActiveAsync();

        foreach (var credential in activeCredentials)
        {
            try
            {
                await syncHandler.Handle(new SyncBinanceHoldingsCommand(credential.UserId), CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to sync Binance holdings for user {UserId}",
                    credential.UserId);
            }
        }
    }
}
