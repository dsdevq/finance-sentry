using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using MediatR;

namespace FinanceSentry.Modules.BrokerageSync.Application.Commands;

public sealed record DisconnectIBKRCommand(Guid UserId) : IRequest;

public sealed class DisconnectIBKRCommandHandler : IRequestHandler<DisconnectIBKRCommand>
{
    private readonly IIBKRCredentialRepository _credentialRepository;
    private readonly IBrokerageHoldingRepository _holdingRepository;

    public DisconnectIBKRCommandHandler(
        IIBKRCredentialRepository credentialRepository,
        IBrokerageHoldingRepository holdingRepository)
    {
        _credentialRepository = credentialRepository;
        _holdingRepository = holdingRepository;
    }

    public async Task Handle(DisconnectIBKRCommand request, CancellationToken ct)
    {
        var credential = await _credentialRepository.GetByUserIdAsync(request.UserId, ct)
            ?? throw new BrokerAccountNotFoundException(
                $"No active IBKR account found for user {request.UserId}.");

        credential.Deactivate();
        _credentialRepository.Update(credential);

        await _holdingRepository.DeleteByUserIdAsync(request.UserId, ct);
        await _holdingRepository.SaveChangesAsync(ct);

        await _credentialRepository.SaveChangesAsync(ct);
    }
}
