using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;

namespace FinanceSentry.Modules.BrokerageSync.Application.Commands;

/// <summary>
/// Per-user IBKR connect request. The username and password are encrypted at
/// rest and used to spawn a dedicated IBeam gateway container for the user in
/// stage 2 of the per-user IBKR rollout.
/// </summary>
public sealed record ConnectIBKRRequest(string Username, string Password);

public sealed record ConnectIBKRCommand(Guid UserId, string Username, string Password) : ICommand<ConnectIBKRResult>;

public sealed record ConnectIBKRResult(int HoldingsCount, DateTime ConnectedAt, string AccountId);

public sealed class ConnectIBKRCommandHandler(
    IIBKRCredentialRepository credentialRepository,
    ICredentialEncryptionService encryption)
    : ICommandHandler<ConnectIBKRCommand, ConnectIBKRResult>
{
    public async Task<ConnectIBKRResult> Handle(ConnectIBKRCommand command, CancellationToken cancellationToken)
    {
        var existing = await credentialRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (existing is not null && existing.IsActive)
            throw new BrokerAlreadyConnectedException(
                "An IBKR account is already connected for this user.");

        var encryptedUsername = encryption.Encrypt(command.Username);
        var encryptedPassword = encryption.Encrypt(command.Password);

        var credential = new IBKRCredential(
            command.UserId,
            encryptedUsername.Ciphertext,
            encryptedUsername.Iv,
            encryptedUsername.AuthTag,
            encryptedPassword.Ciphertext,
            encryptedPassword.Iv,
            encryptedPassword.AuthTag,
            encryptedUsername.KeyVersion);

        await credentialRepository.AddAsync(credential, cancellationToken);
        await credentialRepository.SaveChangesAsync(cancellationToken);

        // Holdings sync + AccountId discovery move to stage 2 (Docker orchestration).
        return new ConnectIBKRResult(0, DateTime.UtcNow, string.Empty);
    }
}
