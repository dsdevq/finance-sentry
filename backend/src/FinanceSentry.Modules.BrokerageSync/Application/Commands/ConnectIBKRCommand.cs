using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;

namespace FinanceSentry.Modules.BrokerageSync.Application.Commands;

/// <summary>
/// Empty body — under the single-tenant gateway model the user does not supply
/// IBKR credentials. The IBeam sidecar already owns the broker session.
/// </summary>
public sealed record ConnectIBKRRequest;

public sealed record ConnectIBKRCommand(Guid UserId) : ICommand<ConnectIBKRResult>;

public sealed record ConnectIBKRResult(int HoldingsCount, DateTime ConnectedAt, string AccountId);

public sealed class ConnectIBKRCommandHandler(
    IIBKRCredentialRepository credentialRepository,
    IBrokerAdapter adapter,
    ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult> syncHandler)
    : ICommandHandler<ConnectIBKRCommand, ConnectIBKRResult>
{
    public async Task<ConnectIBKRResult> Handle(ConnectIBKRCommand command, CancellationToken cancellationToken)
    {
        var existing = await credentialRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (existing is not null && existing.IsActive)
            throw new BrokerAlreadyConnectedException(
                "An IBKR account is already connected for this user.");

        await adapter.EnsureSessionAsync(cancellationToken);

        var accountId = await adapter.GetAccountIdAsync(cancellationToken);

        var credential = new IBKRCredential(command.UserId, accountId);

        await credentialRepository.AddAsync(credential, cancellationToken);
        await credentialRepository.SaveChangesAsync(cancellationToken);

        var syncResult = await syncHandler.Handle(new SyncIBKRHoldingsCommand(command.UserId), cancellationToken);

        return new ConnectIBKRResult(syncResult.HoldingsCount, syncResult.SyncedAt, accountId);
    }
}
