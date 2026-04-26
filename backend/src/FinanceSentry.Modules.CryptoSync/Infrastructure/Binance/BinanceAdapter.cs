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
