namespace FinanceSentry.Modules.CryptoSync.Domain.Interfaces;

public interface ICryptoExchangeAdapter
{
    string ExchangeName { get; }

    Task ValidateCredentialsAsync(string apiKey, string apiSecret, CancellationToken ct = default);

    Task<IReadOnlyList<CryptoAssetBalance>> GetHoldingsAsync(
        string apiKey,
        string apiSecret,
        CancellationToken ct = default);

    /// <summary>
    /// Fetches all USDT-paired trades for the given asset since (exclusive) <paramref name="sinceTradeId"/>.
    /// Returns an empty list when the user has no trade history for the asset/USDT pair.
    /// </summary>
    Task<IReadOnlyList<CryptoTrade>> GetTradesAsync(
        string apiKey,
        string apiSecret,
        string asset,
        long sinceTradeId,
        CancellationToken ct = default);

    Task DisconnectAsync(CancellationToken ct = default);
}

public sealed record CryptoAssetBalance(
    string Asset,
    decimal FreeQuantity,
    decimal LockedQuantity,
    decimal UsdValue);

public sealed record CryptoTrade(
    long TradeId,
    string Asset,
    string QuoteAsset,
    decimal Quantity,
    decimal PriceUsd,
    decimal QuoteQuantityUsd,
    bool IsBuyer,
    DateTime Timestamp);
