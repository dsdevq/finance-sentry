using FinanceSentry.Modules.CryptoSync.Infrastructure.Binance;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Tests.Unit.CryptoSync;

public class BinanceHoldingsAggregatorTests
{
    private readonly BinanceHoldingsAggregator _aggregator = new();

    private static readonly IReadOnlyList<BinancePriceTicker> StandardPrices =
    [
        new("BTCUSDT", "60000"),
        new("ETHUSDT", "3000"),
        new("BNBUSDT", "500"),
    ];

    private static BinanceAccountResponse SpotEmpty() => new([]);

    private static BinanceEarnPage<BinanceFlexibleEarnPosition> FlexibleEmpty() => new([], 0);

    private static BinanceEarnPage<BinanceLockedEarnPosition> LockedEmpty() => new([], 0);

    [Fact]
    public void Aggregate_SpotOnly_ReturnsSingleHolding()
    {
        var spot = new BinanceAccountResponse([new BinanceBalance("BTC", "0.5", "0")]);

        var result = _aggregator.Aggregate(
            spot,
            [],
            FlexibleEmpty(),
            LockedEmpty(),
            StandardPrices,
            dustThresholdUsd: 0.01m);

        result.Should().HaveCount(1);
        result[0].Asset.Should().Be("BTC");
        result[0].FreeQuantity.Should().Be(0.5m);
        result[0].LockedQuantity.Should().Be(0m);
        result[0].UsdValue.Should().Be(30000m); // 0.5 * 60000
    }

    [Fact]
    public void Aggregate_SameAssetAcrossSpotAndFlexibleEarn_SumsQuantities()
    {
        var spot = new BinanceAccountResponse([new BinanceBalance("ETH", "1.0", "0")]);
        var flexible = new BinanceEarnPage<BinanceFlexibleEarnPosition>(
            [new BinanceFlexibleEarnPosition("ETH", "2.5")],
            1);

        var result = _aggregator.Aggregate(
            spot,
            [],
            flexible,
            LockedEmpty(),
            StandardPrices,
            dustThresholdUsd: 0.01m);

        result.Should().HaveCount(1);
        var eth = result[0];
        eth.Asset.Should().Be("ETH");
        eth.FreeQuantity.Should().Be(3.5m);     // 1.0 spot + 2.5 flexible (both "free")
        eth.LockedQuantity.Should().Be(0m);
        eth.UsdValue.Should().Be(10500m);       // 3.5 * 3000
    }

    [Fact]
    public void Aggregate_LockedEarn_TreatedAsLockedQuantity()
    {
        var locked = new BinanceEarnPage<BinanceLockedEarnPosition>(
            [new BinanceLockedEarnPosition("BNB", "10")],
            1);

        var result = _aggregator.Aggregate(
            SpotEmpty(),
            [],
            FlexibleEmpty(),
            locked,
            StandardPrices,
            dustThresholdUsd: 0.01m);

        result.Should().HaveCount(1);
        result[0].Asset.Should().Be("BNB");
        result[0].FreeQuantity.Should().Be(0m);
        result[0].LockedQuantity.Should().Be(10m);
        result[0].UsdValue.Should().Be(5000m);  // 10 * 500
    }

    [Fact]
    public void Aggregate_FundingWalletContributes()
    {
        var funding = new[] { new BinanceFundingAsset("ETH", "0.1", "0", null, null) };

        var result = _aggregator.Aggregate(
            SpotEmpty(),
            funding,
            FlexibleEmpty(),
            LockedEmpty(),
            StandardPrices,
            dustThresholdUsd: 0.01m);

        result.Should().HaveCount(1);
        result[0].Asset.Should().Be("ETH");
        result[0].FreeQuantity.Should().Be(0.1m);
        result[0].UsdValue.Should().Be(300m);
    }

    [Fact]
    public void Aggregate_StablecoinPricedAtOneToOne_NoMarketLookup()
    {
        var spot = new BinanceAccountResponse([new BinanceBalance("USDC", "1234.56", "0")]);

        var result = _aggregator.Aggregate(
            spot,
            [],
            FlexibleEmpty(),
            LockedEmpty(),
            prices: [], // empty price map — stablecoin path bypasses it
            dustThresholdUsd: 0.01m);

        result.Should().HaveCount(1);
        result[0].UsdValue.Should().Be(1234.56m);
    }

    [Fact]
    public void Aggregate_DustBelowThresholdAfterAggregation_IsFiltered()
    {
        // 0.0000001 BTC * 60000 = 0.006 USD < 0.01 threshold
        var spot = new BinanceAccountResponse([new BinanceBalance("BTC", "0.0000001", "0")]);

        var result = _aggregator.Aggregate(
            spot,
            [],
            FlexibleEmpty(),
            LockedEmpty(),
            StandardPrices,
            dustThresholdUsd: 0.01m);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Aggregate_TinySpotPlusRealEarn_NotFilteredAfterAggregation()
    {
        // Tiny spot (0.000001 BTC = 0.06 USD — would fail 1c threshold alone)
        // plus a real Earn position (0.5 BTC = 30000 USD) — must survive.
        var spot = new BinanceAccountResponse([new BinanceBalance("BTC", "0.000001", "0")]);
        var flexible = new BinanceEarnPage<BinanceFlexibleEarnPosition>(
            [new BinanceFlexibleEarnPosition("BTC", "0.5")],
            1);

        var result = _aggregator.Aggregate(
            spot,
            [],
            flexible,
            LockedEmpty(),
            StandardPrices,
            dustThresholdUsd: 0.01m);

        result.Should().HaveCount(1);
        result[0].FreeQuantity.Should().Be(0.500001m);
    }

    [Fact]
    public void Aggregate_AssetWithoutPriceFeed_FilteredByZeroUsdValue()
    {
        // EXOTIC has no USDT or BTC pair → ComputeUsdValue returns 0 → fails dust threshold.
        var spot = new BinanceAccountResponse([new BinanceBalance("EXOTIC", "1000", "0")]);

        var result = _aggregator.Aggregate(
            spot,
            [],
            FlexibleEmpty(),
            LockedEmpty(),
            StandardPrices,
            dustThresholdUsd: 0.01m);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Aggregate_BtcCrossPair_PricesViaBtcUsdt()
    {
        // EXOTIC has only an EXOTIC/BTC pair. Price chain: EXOTIC × 0.001 BTC × 60000 USDT/BTC = 60 USDT.
        var spot = new BinanceAccountResponse([new BinanceBalance("EXOTIC", "1", "0")]);
        var prices = new[]
        {
            new BinancePriceTicker("EXOTICBTC", "0.001"),
            new BinancePriceTicker("BTCUSDT", "60000"),
        };

        var result = _aggregator.Aggregate(
            spot,
            [],
            FlexibleEmpty(),
            LockedEmpty(),
            prices,
            dustThresholdUsd: 0.01m);

        result.Should().HaveCount(1);
        result[0].UsdValue.Should().Be(60m);
    }
}
