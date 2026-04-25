using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.CryptoSync.Domain;
using FinanceSentry.Modules.CryptoSync.Domain.Exceptions;
using FinanceSentry.Modules.CryptoSync.Domain.Interfaces;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;

namespace FinanceSentry.Modules.CryptoSync.Application.Commands;

public sealed record SyncBinanceHoldingsCommand(Guid UserId) : ICommand<SyncBinanceHoldingsResult>;

public sealed record SyncBinanceHoldingsResult(int HoldingsCount, DateTime SyncedAt);

public sealed class SyncBinanceHoldingsCommandHandler : ICommandHandler<SyncBinanceHoldingsCommand, SyncBinanceHoldingsResult>
{
    private readonly IBinanceCredentialRepository _credentialRepository;
    private readonly ICryptoHoldingRepository _holdingRepository;
    private readonly ICryptoExchangeAdapter _adapter;
    private readonly ICredentialEncryptionService _encryption;

    public SyncBinanceHoldingsCommandHandler(
        IBinanceCredentialRepository credentialRepository,
        ICryptoHoldingRepository holdingRepository,
        ICryptoExchangeAdapter adapter,
        ICredentialEncryptionService encryption)
    {
        _credentialRepository = credentialRepository;
        _holdingRepository = holdingRepository;
        _adapter = adapter;
        _encryption = encryption;
    }

    public async Task<SyncBinanceHoldingsResult> Handle(SyncBinanceHoldingsCommand request, CancellationToken ct)
    {
        var credential = await _credentialRepository.GetByUserIdAsync(request.UserId, ct)
            ?? throw new BinanceException("No active Binance credential found for this user.");

        string apiKey;
        string apiSecret;

        try
        {
            apiKey = _encryption.Decrypt(
                credential.EncryptedApiKey,
                credential.ApiKeyIv,
                credential.ApiKeyAuthTag,
                credential.KeyVersion);

            apiSecret = _encryption.Decrypt(
                credential.EncryptedApiSecret,
                credential.ApiSecretIv,
                credential.ApiSecretAuthTag,
                credential.KeyVersion);
        }
        catch (Exception ex)
        {
            credential.MarkSyncFailed("Failed to decrypt credentials.");
            _credentialRepository.Update(credential);
            await _credentialRepository.SaveChangesAsync(ct);
            throw new BinanceException("Failed to decrypt Binance credentials.", ex);
        }

        try
        {
            var balances = await _adapter.GetHoldingsAsync(apiKey, apiSecret, ct);

            var holdings = balances
                .Select(b => CryptoHolding.Create(
                    request.UserId,
                    b.Asset,
                    b.FreeQuantity,
                    b.LockedQuantity,
                    b.UsdValue))
                .ToList();

            await _holdingRepository.UpsertRangeAsync(holdings, ct);
            await _holdingRepository.SaveChangesAsync(ct);

            var syncedAt = DateTime.UtcNow;
            credential.MarkSynced(syncedAt);
            _credentialRepository.Update(credential);
            await _credentialRepository.SaveChangesAsync(ct);

            return new SyncBinanceHoldingsResult(holdings.Count, syncedAt);
        }
        catch (BinanceException ex)
        {
            credential.MarkSyncFailed(ex.Message);
            _credentialRepository.Update(credential);
            await _credentialRepository.SaveChangesAsync(ct);
            throw;
        }
    }
}
