using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.CryptoSync.Domain;
using FinanceSentry.Modules.CryptoSync.Domain.Exceptions;
using FinanceSentry.Modules.CryptoSync.Domain.Interfaces;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;
using MediatR;

namespace FinanceSentry.Modules.CryptoSync.Application.Commands;

public sealed record ConnectBinanceCommand(
    Guid UserId,
    string ApiKey,
    string ApiSecret) : IRequest<ConnectBinanceResult>;

public sealed record ConnectBinanceResult(
    int HoldingsCount,
    DateTime SyncedAt);

public sealed class ConnectBinanceCommandHandler : IRequestHandler<ConnectBinanceCommand, ConnectBinanceResult>
{
    private readonly IBinanceCredentialRepository _credentialRepository;
    private readonly ICryptoExchangeAdapter _adapter;
    private readonly ICredentialEncryptionService _encryption;
    private readonly IMediator _mediator;

    public ConnectBinanceCommandHandler(
        IBinanceCredentialRepository credentialRepository,
        ICryptoExchangeAdapter adapter,
        ICredentialEncryptionService encryption,
        IMediator mediator)
    {
        _credentialRepository = credentialRepository;
        _adapter = adapter;
        _encryption = encryption;
        _mediator = mediator;
    }

    public async Task<ConnectBinanceResult> Handle(ConnectBinanceCommand request, CancellationToken ct)
    {
        var existing = await _credentialRepository.GetByUserIdAsync(request.UserId, ct);
        if (existing is not null && existing.IsActive)
        {
            throw new BinanceException("A Binance account is already connected for this user.", -1001);
        }

        await _adapter.ValidateCredentialsAsync(request.ApiKey, request.ApiSecret, ct);

        var encryptedKey = _encryption.Encrypt(request.ApiKey);
        var encryptedSecret = _encryption.Encrypt(request.ApiSecret);

        var credential = BinanceCredential.Create(
            request.UserId,
            encryptedKey.Ciphertext,
            encryptedKey.Iv,
            encryptedKey.AuthTag,
            encryptedSecret.Ciphertext,
            encryptedSecret.Iv,
            encryptedSecret.AuthTag,
            encryptedKey.KeyVersion);

        await _credentialRepository.AddAsync(credential, ct);
        await _credentialRepository.SaveChangesAsync(ct);

        var syncResult = await _mediator.Send(
            new SyncBinanceHoldingsCommand(request.UserId),
            ct);

        return new ConnectBinanceResult(syncResult.HoldingsCount, syncResult.SyncedAt);
    }
}
