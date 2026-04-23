using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using MediatR;

namespace FinanceSentry.Modules.BrokerageSync.Application.Commands;

public sealed record ConnectIBKRCommand(
    Guid UserId,
    string Username,
    string Password) : IRequest<ConnectIBKRResult>;

public sealed record ConnectIBKRResult(int HoldingsCount, DateTime ConnectedAt);

public sealed class ConnectIBKRCommandHandler : IRequestHandler<ConnectIBKRCommand, ConnectIBKRResult>
{
    private readonly IIBKRCredentialRepository _credentialRepository;
    private readonly IBrokerAdapter _adapter;
    private readonly ICredentialEncryptionService _encryption;
    private readonly IMediator _mediator;

    public ConnectIBKRCommandHandler(
        IIBKRCredentialRepository credentialRepository,
        IBrokerAdapter adapter,
        ICredentialEncryptionService encryption,
        IMediator mediator)
    {
        _credentialRepository = credentialRepository;
        _adapter = adapter;
        _encryption = encryption;
        _mediator = mediator;
    }

    public async Task<ConnectIBKRResult> Handle(ConnectIBKRCommand request, CancellationToken ct)
    {
        var existing = await _credentialRepository.GetByUserIdAsync(request.UserId, ct);
        if (existing is not null && existing.IsActive)
            throw new BrokerAlreadyConnectedException(
                "An IBKR account is already connected for this user.");

        await _adapter.AuthenticateAsync(request.Username, request.Password, ct);

        var accountId = await _adapter.GetAccountIdAsync(ct);

        var encUsername = _encryption.Encrypt(request.Username);
        var encPassword = _encryption.Encrypt(request.Password);

        var credential = new IBKRCredential(
            request.UserId,
            encUsername.Ciphertext, encUsername.Iv, encUsername.AuthTag,
            encPassword.Ciphertext, encPassword.Iv, encPassword.AuthTag,
            encUsername.KeyVersion,
            accountId);

        await _credentialRepository.AddAsync(credential, ct);
        await _credentialRepository.SaveChangesAsync(ct);

        var syncResult = await _mediator.Send(new SyncIBKRHoldingsCommand(request.UserId), ct);

        return new ConnectIBKRResult(syncResult.HoldingsCount, syncResult.SyncedAt);
    }
}
