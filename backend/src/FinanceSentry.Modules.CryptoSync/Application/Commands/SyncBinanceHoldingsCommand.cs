using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.CryptoSync.Application.Services;
using FinanceSentry.Modules.CryptoSync.Domain;
using FinanceSentry.Modules.CryptoSync.Domain.Exceptions;
using FinanceSentry.Modules.CryptoSync.Domain.Interfaces;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.CryptoSync.Application.Commands;

public sealed record SyncBinanceHoldingsCommand(Guid UserId) : ICommand<SyncBinanceHoldingsResult>;

public sealed record SyncBinanceHoldingsResult(int HoldingsCount, DateTime SyncedAt);

public sealed class SyncBinanceHoldingsCommandHandler : ICommandHandler<SyncBinanceHoldingsCommand, SyncBinanceHoldingsResult>
{
    private const decimal QuantityTolerance = 0.01m;
    private const decimal MaximumTrustedCostToValueRatio = 20m;

    private readonly IBinanceCredentialRepository _credentialRepository;
    private readonly ICryptoHoldingRepository _holdingRepository;
    private readonly ICryptoExchangeAdapter _adapter;
    private readonly ICredentialEncryptionService _encryption;
    private readonly CostBasisCalculator _costBasisCalculator;
    private readonly ILogger<SyncBinanceHoldingsCommandHandler> _logger;

    public SyncBinanceHoldingsCommandHandler(
        IBinanceCredentialRepository credentialRepository,
        ICryptoHoldingRepository holdingRepository,
        ICryptoExchangeAdapter adapter,
        ICredentialEncryptionService encryption,
        CostBasisCalculator costBasisCalculator,
        ILogger<SyncBinanceHoldingsCommandHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _holdingRepository = holdingRepository;
        _adapter = adapter;
        _encryption = encryption;
        _costBasisCalculator = costBasisCalculator;
        _logger = logger;
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

            // Reconcile: the aggregator only returns assets the user still holds (dust
            // and zero balances are already dropped), so anything persisted but missing
            // here was sold out — delete it instead of leaving a stale $0 holding.
            var freshAssets = holdings
                .Select(h => h.Asset)
                .ToHashSet(StringComparer.Ordinal);
            var persisted = await _holdingRepository.GetByUserIdAsync(request.UserId, ct);
            var stale = persisted
                .Where(h => !freshAssets.Contains(h.Asset))
                .ToList();
            if (stale.Count > 0)
            {
                _holdingRepository.RemoveRange(stale);
                await _holdingRepository.SaveChangesAsync(ct);
            }

            await UpdateCostBasisAsync(request.UserId, apiKey, apiSecret, ct);

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

    private async Task UpdateCostBasisAsync(Guid userId, string apiKey, string apiSecret, CancellationToken ct)
    {
        var persisted = await _holdingRepository.GetByUserIdAsync(userId, ct);

        foreach (var holding in persisted)
        {
            IReadOnlyList<CryptoTrade> trades;
            try
            {
                trades = await _adapter.GetTradesAsync(apiKey, apiSecret, holding.Asset, holding.LastTradeId, ct);
            }
            catch (BinanceException ex)
            {
                _logger.LogWarning(ex,
                    "Trade history fetch failed for {Asset} (user {UserId}); cost basis left unchanged.",
                    holding.Asset, userId);
                continue;
            }

            if (trades.Count == 0 && holding.TradeCount > 0)
            {
                continue;
            }

            var seed = holding.TradeCount > 0
                ? new CostBasisResult(
                    CostBasisUsd: holding.CostBasisUsd ?? 0m,
                    AverageBuyPriceUsd: holding.AverageBuyPriceUsd ?? 0m,
                    RemainingQuantity: holding.AverageBuyPriceUsd is > 0m && holding.CostBasisUsd is not null
                        ? holding.CostBasisUsd.Value / holding.AverageBuyPriceUsd.Value
                        : 0m,
                    RealizedPnlUsd: holding.RealizedPnlUsd ?? 0m,
                    LastTradeAt: holding.LastTradeAt,
                    LastTradeId: holding.LastTradeId,
                    TradeCount: holding.TradeCount)
                : null;

            var result = _costBasisCalculator.Compute(trades, seed);

            if (result.TradeCount == 0)
            {
                continue;
            }

            var currentQuantity = holding.FreeQuantity + holding.LockedQuantity;
            var costBasis = TrustCostBasisForCurrentPosition(result, currentQuantity, holding.UsdValue);

            holding.SetCostBasis(
                costBasis,
                costBasis is null ? null : result.AverageBuyPriceUsd,
                result.RealizedPnlUsd,
                result.LastTradeAt,
                result.LastTradeId,
                result.TradeCount);
        }

        await _holdingRepository.SaveChangesAsync(ct);
    }

    private static decimal? TrustCostBasisForCurrentPosition(
        CostBasisResult result,
        decimal currentQuantity,
        decimal currentValueUsd)
    {
        if (currentQuantity <= 0m)
        {
            return 0m;
        }

        if (result.AverageBuyPriceUsd <= 0m || result.RemainingQuantity <= 0m)
        {
            return null;
        }

        var quantityDelta = Math.Abs(result.RemainingQuantity - currentQuantity);
        var tolerance = Math.Max(currentQuantity, result.RemainingQuantity) * QuantityTolerance;

        decimal costBasis;
        if (quantityDelta <= tolerance)
        {
            costBasis = result.CostBasisUsd;
        }
        else if (result.RemainingQuantity > currentQuantity)
        {
            costBasis = result.AverageBuyPriceUsd * currentQuantity;
        }
        else
        {
            return null;
        }

        if (currentValueUsd > 0m && costBasis / currentValueUsd > MaximumTrustedCostToValueRatio)
        {
            return null;
        }

        return Math.Round(costBasis, 4);
    }
}
