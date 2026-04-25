using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;

namespace FinanceSentry.Modules.BrokerageSync.Application.Commands;

public sealed record SyncIBKRHoldingsCommand(Guid UserId) : ICommand<SyncIBKRHoldingsResult>;

public sealed record SyncIBKRHoldingsResult(int HoldingsCount, DateTime SyncedAt);

public sealed class SyncIBKRHoldingsCommandHandler : ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult>
{
    private readonly IIBKRCredentialRepository _credentialRepository;
    private readonly IBrokerageHoldingRepository _holdingRepository;
    private readonly IBrokerAdapter _adapter;
    private readonly ICredentialEncryptionService _encryption;

    public SyncIBKRHoldingsCommandHandler(
        IIBKRCredentialRepository credentialRepository,
        IBrokerageHoldingRepository holdingRepository,
        IBrokerAdapter adapter,
        ICredentialEncryptionService encryption)
    {
        _credentialRepository = credentialRepository;
        _holdingRepository = holdingRepository;
        _adapter = adapter;
        _encryption = encryption;
    }

    public async Task<SyncIBKRHoldingsResult> Handle(SyncIBKRHoldingsCommand request, CancellationToken ct)
    {
        var credential = await _credentialRepository.GetByUserIdAsync(request.UserId, ct)
            ?? throw new InvalidOperationException("No active IBKR credential found for this user.");

        string username;
        string password;

        try
        {
            username = _encryption.Decrypt(
                credential.EncryptedUsername,
                credential.UsernameIv,
                credential.UsernameAuthTag,
                credential.KeyVersion);

            password = _encryption.Decrypt(
                credential.EncryptedPassword,
                credential.PasswordIv,
                credential.PasswordAuthTag,
                credential.KeyVersion);
        }
        catch (Exception ex)
        {
            credential.RecordSyncError("Failed to decrypt credentials.");
            _credentialRepository.Update(credential);
            await _credentialRepository.SaveChangesAsync(ct);
            throw new InvalidOperationException("Failed to decrypt IBKR credentials.", ex);
        }

        try
        {
            await _adapter.AuthenticateAsync(username, password, ct);

            var accountId = credential.AccountId
                ?? await _adapter.GetAccountIdAsync(ct);

            var positions = await _adapter.GetPositionsAsync(accountId, ct);

            var holdings = positions
                .Select(p => new BrokerageHolding(
                    request.UserId,
                    p.Symbol,
                    p.InstrumentType,
                    p.Quantity,
                    p.UsdValue,
                    "ibkr"))
                .ToList();

            await _holdingRepository.UpsertRangeAsync(holdings, ct);
            await _holdingRepository.SaveChangesAsync(ct);

            credential.RecordSyncSuccess();
            _credentialRepository.Update(credential);
            await _credentialRepository.SaveChangesAsync(ct);

            return new SyncIBKRHoldingsResult(holdings.Count, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            credential.RecordSyncError(ex.Message);
            _credentialRepository.Update(credential);
            await _credentialRepository.SaveChangesAsync(ct);
            throw;
        }
    }
}
