using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.Jobs;

public sealed class IBKRSyncJob(
    IIBKRCredentialRepository credentialRepository,
    ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult> syncHandler,
    ILogger<IBKRSyncJob> logger)
{
    public async Task ExecuteAsync()
    {
        var activeCredentials = await credentialRepository.GetAllActiveAsync();

        foreach (var credential in activeCredentials)
        {
            try
            {
                await syncHandler.Handle(new SyncIBKRHoldingsCommand(credential.UserId), CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to sync IBKR holdings for user {UserId}",
                    credential.UserId);
            }
        }
    }
}
