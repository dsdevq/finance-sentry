using FinanceSentry.Modules.CryptoSync.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FinanceSentry.Modules.CryptoSync.Infrastructure.Binance;

public sealed class BinanceAdapter : ICryptoExchangeAdapter
{
    private static readonly HashSet<string> Stablecoins = new(StringComparer.OrdinalIgnoreCase)
    {
        "USDT", "USDC", "BUSD", "TUSD", "USDP", "DAI", "FDUSD",
    };

    private readonly BinanceHttpClient _httpClient;
    private readonly decimal _dustThresholdUsd;

    public string ExchangeName => "binance";

    public BinanceAdapter(BinanceHttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _dustThresholdUsd = decimal.TryParse(
            configuration["Binance:DustThresholdUsd"],
            out var threshold) ? threshold : 0.01m;
    }

    public async Task ValidateCredentialsAsync(string apiKey, string apiSecret, CancellationToken ct = default)
    {
        await _httpClient.GetAccountAsync(apiKey, apiSecret, ct);
    }

    public async Task<IReadOnlyList<CryptoAssetBalance>> GetHoldingsAsync(
        string apiKey,
        string apiSecret,
        CancellationToken ct = default)
    {
        var accountTask = _httpClient.GetAccountAsync(apiKey, apiSecret, ct);
        var pricesTask = _httpClient.GetAllPricesAsync(ct);

        await Task.WhenAll(accountTask, pricesTask);

        var account = accountTask.Result;
        var prices = pricesTask.Result;

        var priceMap = prices.ToDictionary(p => p.Symbol, p => decimal.Parse(p.Price), StringComparer.OrdinalIgnoreCase);

        var holdings = new List<CryptoAssetBalance>();
        foreach (var balance in account.Balances)
        {
            if (!decimal.TryParse(balance.Free, out var free)) { free = 0m; }
            if (!decimal.TryParse(balance.Locked, out var locked)) { locked = 0m; }

            var total = free + locked;
            if (total <= 0m) { continue; }

            var usdValue = ComputeUsdValue(balance.Asset, total, priceMap);
            if (usdValue < _dustThresholdUsd) { continue; }

            holdings.Add(new CryptoAssetBalance(balance.Asset, free, locked, usdValue));
        }

        return holdings;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    private static decimal ComputeUsdValue(
        string asset,
        decimal quantity,
        Dictionary<string, decimal> priceMap)
    {
        if (Stablecoins.Contains(asset))
        {
            return quantity;
        }

        var usdtPair = $"{asset}USDT";
        if (priceMap.TryGetValue(usdtPair, out var usdtPrice))
        {
            return quantity * usdtPrice;
        }

        var btcPair = $"{asset}BTC";
        if (priceMap.TryGetValue(btcPair, out var btcPrice) &&
            priceMap.TryGetValue("BTCUSDT", out var btcUsdtPrice))
        {
            return quantity * btcPrice * btcUsdtPrice;
        }

        return 0m;
    }
}
