using FinanceSentry.Modules.CryptoSync.Application.Commands;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.CryptoSync.Infrastructure.Jobs;

public sealed class BinanceSyncJob
{
    private readonly IBinanceCredentialRepository _credentialRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<BinanceSyncJob> _logger;

    public BinanceSyncJob(
        IBinanceCredentialRepository credentialRepository,
        IMediator mediator,
        ILogger<BinanceSyncJob> logger)
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
                await _mediator.Send(new SyncBinanceHoldingsCommand(credential.UserId));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to sync Binance holdings for user {UserId}",
                    credential.UserId);
            }
        }
    }
}
