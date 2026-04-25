using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.CryptoSync.Domain;
using FinanceSentry.Modules.CryptoSync.Domain.Exceptions;
using FinanceSentry.Modules.CryptoSync.Domain.Interfaces;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;

namespace FinanceSentry.Modules.CryptoSync.Application.Commands;

public sealed record ConnectBinanceCommand(
    Guid UserId,
    string ApiKey,
    string ApiSecret) : ICommand<ConnectBinanceResult>;

public sealed record ConnectBinanceResult(
    int HoldingsCount,
    DateTime SyncedAt);

public sealed class ConnectBinanceCommandHandler(
    IBinanceCredentialRepository credentialRepository,
    ICryptoExchangeAdapter adapter,
    ICredentialEncryptionService encryption,
    ICommandHandler<SyncBinanceHoldingsCommand, SyncBinanceHoldingsResult> syncHandler)
    : ICommandHandler<ConnectBinanceCommand, ConnectBinanceResult>
{
    public async Task<ConnectBinanceResult> Handle(ConnectBinanceCommand command, CancellationToken cancellationToken)
    {
        var existing = await credentialRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (existing is not null && existing.IsActive)
        {
            throw new BinanceAlreadyConnectedException();
        }

        await adapter.ValidateCredentialsAsync(command.ApiKey, command.ApiSecret, cancellationToken);

        var encryptedKey = encryption.Encrypt(command.ApiKey);
        var encryptedSecret = encryption.Encrypt(command.ApiSecret);

        var credential = BinanceCredential.Create(
            command.UserId,
            encryptedKey.Ciphertext,
            encryptedKey.Iv,
            encryptedKey.AuthTag,
            encryptedSecret.Ciphertext,
            encryptedSecret.Iv,
            encryptedSecret.AuthTag,
            encryptedKey.KeyVersion);

        await credentialRepository.AddAsync(credential, cancellationToken);
        await credentialRepository.SaveChangesAsync(cancellationToken);

        var syncResult = await syncHandler.Handle(
            new SyncBinanceHoldingsCommand(command.UserId),
            cancellationToken);

        return new ConnectBinanceResult(syncResult.HoldingsCount, syncResult.SyncedAt);
    }
}
