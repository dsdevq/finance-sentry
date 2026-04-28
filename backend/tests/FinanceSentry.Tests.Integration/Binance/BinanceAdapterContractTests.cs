using System.Net;
using System.Text;
using FinanceSentry.Modules.CryptoSync.Domain.Exceptions;
using FinanceSentry.Modules.CryptoSync.Infrastructure.Binance;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceSentry.Tests.Integration.Binance;

public class BinanceAdapterContractTests
{
    private const string FakeApiKey = "testapikeytestapikeytestapikeytestapikeytestapikey123456";
    private const string FakeApiSecret = "testsecrettestsecrettestsecrettestsecrettestsecret123456";

    private static BinanceHttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Binance:BaseUrl"] = "https://api.binance.com",
                ["Binance:RecvWindowMs"] = "5000",
            })
            .Build();
        return new BinanceHttpClient(http, config);
    }

    private static BinanceAdapter CreateAdapter(BinanceHttpClient httpClient)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Binance:DustThresholdUsd"] = "0.01",
            })
            .Build();
        return new BinanceAdapter(httpClient, new BinanceHoldingsAggregator(), NullLogger<BinanceAdapter>.Instance, config);
    }

    [Fact]
    public async Task GetAccountAsync_ParsesBalancesCorrectly()
    {
        var handler = new FakeHttpMessageHandler(
            @"{""balances"":[{""asset"":""BTC"",""free"":""0.5"",""locked"":""0.1""},{""asset"":""ETH"",""free"":""2.0"",""locked"":""0.0""}]}");

        var client = CreateHttpClient(handler);
        var result = await client.GetAccountAsync(FakeApiKey, FakeApiSecret);

        result.Balances.Should().HaveCount(2);
        result.Balances[0].Asset.Should().Be("BTC");
        result.Balances[0].Free.Should().Be("0.5");
        result.Balances[1].Asset.Should().Be("ETH");
    }

    [Fact]
    public async Task GetAccountAsync_ThrowsBinanceException_OnAuthFailure()
    {
        var handler = new FakeHttpMessageHandler(
            @"{""code"":-2014,""msg"":""API-key format invalid.""}",
            HttpStatusCode.Unauthorized);

        var client = CreateHttpClient(handler);
        var act = async () => await client.GetAccountAsync(FakeApiKey, FakeApiSecret);

        await act.Should().ThrowAsync<BinanceException>()
            .WithMessage("*API-key format invalid*");
    }

    [Fact]
    public async Task GetHoldingsAsync_FiltersDustAssets()
    {
        var accountJson = @"{""balances"":[
            {""asset"":""BTC"",""free"":""0.5"",""locked"":""0.0""},
            {""asset"":""TINY"",""free"":""0.000001"",""locked"":""0.0""}
        ]}";
        var pricesJson = @"[{""symbol"":""BTCUSDT"",""price"":""60000""}]";

        var handler = new FakeMultiHttpMessageHandler(new Dictionary<string, string>
        {
            ["/api/v3/account"] = accountJson,
            ["/api/v3/ticker/price"] = pricesJson,
        });

        var client = CreateHttpClient(handler);
        var adapter = CreateAdapter(client);

        var holdings = await adapter.GetHoldingsAsync(FakeApiKey, FakeApiSecret);

        holdings.Should().HaveCount(1);
        holdings[0].Asset.Should().Be("BTC");
        holdings[0].UsdValue.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task GetHoldingsAsync_StablecoinPricedAt1()
    {
        var accountJson = @"{""balances"":[{""asset"":""USDT"",""free"":""500"",""locked"":""0""}]}";
        var pricesJson = @"[]";

        var handler = new FakeMultiHttpMessageHandler(new Dictionary<string, string>
        {
            ["/api/v3/account"] = accountJson,
            ["/api/v3/ticker/price"] = pricesJson,
        });

        var client = CreateHttpClient(handler);
        var adapter = CreateAdapter(client);

        var holdings = await adapter.GetHoldingsAsync(FakeApiKey, FakeApiSecret);

        holdings.Should().HaveCount(1);
        holdings[0].UsdValue.Should().Be(500m);
    }

    [Fact]
    public async Task GetAccountAsync_RequestIncludesHmacSignature()
    {
        string? capturedUrl = null;
        var handler = new CapturingHttpMessageHandler(url =>
        {
            capturedUrl = url;
            return (@"{""balances"":[]}");
        });

        var client = CreateHttpClient(handler);
        await client.GetAccountAsync(FakeApiKey, FakeApiSecret);

        capturedUrl.Should().Contain("signature=");
        capturedUrl.Should().Contain("timestamp=");
        capturedUrl.Should().Contain("recvWindow=");
    }
}

// ── Test doubles ──────────────────────────────────────────────────────────────

public sealed class FakeHttpMessageHandler(
    string responseBody,
    HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var responseMessage = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(responseMessage);
    }
}

public sealed class FakeMultiHttpMessageHandler(Dictionary<string, string> responses) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.PathAndQuery ?? string.Empty;
        var matchedKey = responses.Keys.FirstOrDefault(k => url.Contains(k)) ?? string.Empty;
        var body = responses.TryGetValue(matchedKey, out var responseBody) ? responseBody : "{}";

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
    }
}

public sealed class CapturingHttpMessageHandler(Func<string, string> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? string.Empty;
        var body = handler(url);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
    }
}
