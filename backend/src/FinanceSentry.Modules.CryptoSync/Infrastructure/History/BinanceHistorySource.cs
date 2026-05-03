namespace FinanceSentry.Modules.CryptoSync.Infrastructure.History;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;
using FinanceSentry.Modules.CryptoSync.Infrastructure.Binance;
using Microsoft.Extensions.Logging;

public sealed class BinanceHistorySource(
    IBinanceCredentialRepository credentialRepository,
    BinanceHttpClient httpClient,
    ICredentialEncryptionService encryption,
    ILogger<BinanceHistorySource> logger) : IProviderMonthlyHistorySource
{
    private readonly IBinanceCredentialRepository _credentialRepository = credentialRepository;
    private readonly BinanceHttpClient _httpClient = httpClient;
    private readonly ICredentialEncryptionService _encryption = encryption;
    private readonly ILogger<BinanceHistorySource> _logger = logger;

    private const int LookbackDays = 30;

    public async Task<IReadOnlyList<ProviderMonthlyBalance>> GetMonthlyBalancesAsync(
        Guid userId, CancellationToken ct = default)
    {
        var credential = await _credentialRepository.GetByUserIdAsync(userId, ct);
        if (credential is null || !credential.IsActive)
            return [];

        string apiKey;
        string apiSecret;
        try
        {
            apiKey = _encryption.Decrypt(credential.EncryptedApiKey, credential.ApiKeyIv, credential.ApiKeyAuthTag, credential.KeyVersion);
            apiSecret = _encryption.Decrypt(credential.EncryptedApiSecret, credential.ApiSecretIv, credential.ApiSecretAuthTag, credential.KeyVersion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt Binance credentials for user {UserId} during backfill", userId);
            return [];
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            var startTime = now.AddDays(-LookbackDays).ToUnixTimeMilliseconds();
            var endTime = now.ToUnixTimeMilliseconds();

            var snapshotResponse = await _httpClient.GetAccountSnapshotAsync(apiKey, apiSecret, startTime, endTime, ct);
            var prices = await _httpClient.GetAllPricesAsync(ct);
            var priceMap = BuildPriceMap(prices);

            return snapshotResponse.SnapshotVos
                .GroupBy(v => MonthEnd(DateTimeOffset.FromUnixTimeMilliseconds(v.UpdateTime)))
                .Select(g =>
                {
                    var last = g.OrderBy(v => v.UpdateTime).Last();
                    var totalUsd = last.Data.Balances.Sum(b => CalculateUsd(b, priceMap));
                    return new ProviderMonthlyBalance(g.Key, totalUsd, "crypto");
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Binance history fetch failed for user {UserId} during backfill — returning empty", userId);
            return [];
        }
    }

    private static Dictionary<string, decimal> BuildPriceMap(IReadOnlyList<BinancePriceTicker> tickers)
    {
        var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in tickers)
        {
            if (decimal.TryParse(t.Price, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
                map[t.Symbol] = price;
        }
        return map;
    }

    private static decimal CalculateUsd(BinanceBalance balance, Dictionary<string, decimal> priceMap)
    {
        if (!decimal.TryParse(balance.Free, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var free))
            free = 0m;
        if (!decimal.TryParse(balance.Locked, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var locked))
            locked = 0m;

        var total = free + locked;
        if (total == 0m) return 0m;

        var symbol = balance.Asset;
        if (symbol.Equals("USDT", StringComparison.OrdinalIgnoreCase) || symbol.Equals("BUSD", StringComparison.OrdinalIgnoreCase))
            return total;

        if (priceMap.TryGetValue(symbol + "USDT", out var usdtPrice))
            return total * usdtPrice;

        return 0m;
    }

    private static DateOnly MonthEnd(DateTimeOffset dt)
    {
        var daysInMonth = DateTime.DaysInMonth(dt.Year, dt.Month);
        return new DateOnly(dt.Year, dt.Month, daysInMonth);
    }
}
