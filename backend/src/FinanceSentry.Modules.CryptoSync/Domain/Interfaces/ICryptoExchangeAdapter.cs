namespace FinanceSentry.Modules.CryptoSync.Domain.Interfaces;

public interface ICryptoExchangeAdapter
{
    string ExchangeName { get; }

    Task ValidateCredentialsAsync(string apiKey, string apiSecret, CancellationToken ct = default);

    Task<IReadOnlyList<CryptoAssetBalance>> GetHoldingsAsync(
        string apiKey,
        string apiSecret,
        CancellationToken ct = default);

    Task DisconnectAsync(CancellationToken ct = default);
}

public sealed record CryptoAssetBalance(
    string Asset,
    decimal FreeQuantity,
    decimal LockedQuantity,
    decimal UsdValue);
