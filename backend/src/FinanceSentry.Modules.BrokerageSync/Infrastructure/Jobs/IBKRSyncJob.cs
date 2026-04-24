using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.Jobs;

public sealed class IBKRSyncJob
{
    private readonly IIBKRCredentialRepository _credentialRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<IBKRSyncJob> _logger;

    public IBKRSyncJob(
        IIBKRCredentialRepository credentialRepository,
        IMediator mediator,
        ILogger<IBKRSyncJob> logger)
    {
        _credentialRepository = credentialRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var activeCredentials = await _credentialRepository.GetAllActiveAsync();

        foreach (var credential in activeCredentials)
        {
            try
            {
                await _mediator.Send(new SyncIBKRHoldingsCommand(credential.UserId));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to sync IBKR holdings for user {UserId}",
                    credential.UserId);
            }
        }
    }
}
