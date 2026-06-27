using FinanceSentry.Modules.CryptoSync.Domain.Exceptions;
using FinanceSentry.Modules.CryptoSync.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.CryptoSync.Infrastructure.Binance;

/// <summary>
/// HTTP orchestration only. Fans out the four wallet endpoints + price call in
/// parallel, tolerates permission failures on optional sources, and hands the
/// raw responses to <see cref="BinanceHoldingsAggregator"/> to produce the
/// per-asset balance list.
/// </summary>
public sealed class BinanceAdapter : ICryptoExchangeAdapter
{
    private readonly BinanceHttpClient _httpClient;
    private readonly BinanceHoldingsAggregator _aggregator;
    private readonly ILogger<BinanceAdapter> _logger;
    private readonly decimal _dustThresholdUsd;

    public string ExchangeName => "binance";

    public BinanceAdapter(
        BinanceHttpClient httpClient,
        BinanceHoldingsAggregator aggregator,
        ILogger<BinanceAdapter> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _aggregator = aggregator;
        _logger = logger;
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
        // Spot is the source of truth for credential health — fail loudly here.
        var spotTask = _httpClient.GetAccountAsync(apiKey, apiSecret, ct);
        var pricesTask = _httpClient.GetAllPricesAsync(ct);

        // Funding + Earn require additional permissions on the API key (Read-Only
        // is enough but the user may have scoped the key narrower). Treat as
        // best-effort: log and continue if any one of these is rejected.
        var fundingTask = SafeFetchAsync(
            () => _httpClient.GetFundingAssetsAsync(apiKey, apiSecret, ct),
            "Funding wallet",
            (IReadOnlyList<BinanceFundingAsset>)Array.Empty<BinanceFundingAsset>());

        var flexibleEarnTask = SafeFetchAsync(
            () => _httpClient.GetFlexibleEarnPositionsAsync(apiKey, apiSecret, ct),
            "Simple Earn (flexible)",
            new BinanceEarnPage<BinanceFlexibleEarnPosition>([], 0));

        var lockedEarnTask = SafeFetchAsync(
            () => _httpClient.GetLockedEarnPositionsAsync(apiKey, apiSecret, ct),
            "Simple Earn (locked)",
            new BinanceEarnPage<BinanceLockedEarnPosition>([], 0));

        await Task.WhenAll(spotTask, pricesTask, fundingTask, flexibleEarnTask, lockedEarnTask);

        return _aggregator.Aggregate(
            spotTask.Result,
            fundingTask.Result,
            flexibleEarnTask.Result,
            lockedEarnTask.Result,
            pricesTask.Result,
            _dustThresholdUsd);
    }

    public async Task<IReadOnlyList<CryptoTrade>> GetTradesAsync(
        string apiKey,
        string apiSecret,
        string asset,
        long sinceTradeId,
        CancellationToken ct = default)
    {
        const int pageLimit = 1000;
        const int maxPages = 20;

        if (string.Equals(asset, "USDT", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(asset, "USDC", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(asset, "BUSD", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(asset, "FDUSD", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(asset, "DAI", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var quoteCandidates = new[] { "USDT", "USDC", "FDUSD", "BUSD" };
        var allTrades = new List<CryptoTrade>();

        foreach (var quote in quoteCandidates)
        {
            var symbol = $"{asset.ToUpperInvariant()}{quote}";
            var fromId = sinceTradeId;
            for (var page = 0; page < maxPages; page++)
            {
                IReadOnlyList<BinanceTradeRow> rows;
                try
                {
                    rows = await _httpClient.GetMyTradesAsync(apiKey, apiSecret, symbol, fromId, pageLimit, ct);
                }
                catch (BinanceException ex)
                {
                    _logger.LogDebug(ex, "Binance trade history for {Symbol} unavailable (likely no such pair).", symbol);
                    break;
                }

                if (rows.Count == 0) break;

                foreach (var r in rows)
                {
                    allTrades.Add(new CryptoTrade(
                        TradeId: r.Id,
                        Asset: asset.ToUpperInvariant(),
                        QuoteAsset: quote,
                        Quantity: decimal.Parse(r.Quantity, System.Globalization.CultureInfo.InvariantCulture),
                        PriceUsd: decimal.Parse(r.Price, System.Globalization.CultureInfo.InvariantCulture),
                        QuoteQuantityUsd: decimal.Parse(r.QuoteQuantity, System.Globalization.CultureInfo.InvariantCulture),
                        IsBuyer: r.IsBuyer,
                        Timestamp: DateTimeOffset.FromUnixTimeMilliseconds(r.TimeMs).UtcDateTime));
                }

                if (rows.Count < pageLimit) break;
                fromId = rows[^1].Id + 1;
            }
        }

        return allTrades
            .OrderBy(t => t.Timestamp)
            .ThenBy(t => t.TradeId)
            .ToList();
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    private async Task<T> SafeFetchAsync<T>(Func<Task<T>> fetcher, string label, T fallback)
    {
        try
        {
            return await fetcher();
        }
        catch (BinanceException ex)
        {
            _logger.LogWarning(
                ex,
                "Skipping Binance source '{Source}' — request was rejected (likely missing API-key permission). Sync continues without this data.",
                label);
            return fallback;
        }
    }
}
