using FinanceSentry.Modules.CryptoSync.Domain.Exceptions;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;
using MediatR;

namespace FinanceSentry.Modules.CryptoSync.Application.Commands;

public sealed record DisconnectBinanceCommand(Guid UserId) : IRequest;

public sealed class DisconnectBinanceCommandHandler : IRequestHandler<DisconnectBinanceCommand>
{
    private readonly IBinanceCredentialRepository _credentialRepository;
    private readonly ICryptoHoldingRepository _holdingRepository;

    public DisconnectBinanceCommandHandler(
        IBinanceCredentialRepository credentialRepository,
        ICryptoHoldingRepository holdingRepository)
    {
        _credentialRepository = credentialRepository;
        _holdingRepository = holdingRepository;
    }

    public async Task Handle(DisconnectBinanceCommand request, CancellationToken ct)
    {
        var credential = await _credentialRepository.GetByUserIdAsync(request.UserId, ct);
        if (credential is null || !credential.IsActive)
        {
            throw new BinanceException("No Binance account is connected for this user.", -1002);
        }

        credential.Deactivate();
        _credentialRepository.Update(credential);

        await _holdingRepository.DeleteByUserIdAsync(request.UserId, ct);
        await _credentialRepository.SaveChangesAsync(ct);
    }
}
