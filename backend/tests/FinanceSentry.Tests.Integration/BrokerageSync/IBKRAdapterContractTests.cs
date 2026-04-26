using System.Net;
using System.Text;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinanceSentry.Tests.Integration.BrokerageSync;

public class IBKRAdapterContractTests
{
    private static IBKRGatewayClient CreateClient(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IBKR:GatewayBaseUrl"] = "http://ibkr-gateway:5000",
            })
            .Build();
        return new IBKRGatewayClient(http, config);
    }

    private static IBKRAdapter CreateAdapter(IBKRGatewayClient client) => new(client);

    [Fact]
    public async Task EnsureSessionAsync_Succeeds_WhenGatewayReturnsAuthenticated()
    {
        var handler = new FakeIBKRMultiHandler(new Dictionary<string, (string body, HttpStatusCode code)>
        {
            ["/v1/api/iserver/auth/status"] = (@"{""authenticated"":true,""connected"":true}", HttpStatusCode.OK),
        });

        var adapter = CreateAdapter(CreateClient(handler));
        var act = async () => await adapter.EnsureSessionAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureSessionAsync_ThrowsBrokerAuthException_WhenStatusNotAuthenticated()
    {
        var handler = new FakeIBKRMultiHandler(new Dictionary<string, (string body, HttpStatusCode code)>
        {
            ["/v1/api/iserver/auth/status"] = (@"{""authenticated"":false,""connected"":false}", HttpStatusCode.OK),
        });

        var adapter = CreateAdapter(CreateClient(handler));
        var act = async () => await adapter.EnsureSessionAsync();

        await act.Should().ThrowAsync<BrokerAuthException>()
            .WithMessage("*not authenticated*");
    }

    [Fact]
    public async Task GetAccountIdAsync_ReturnsFirstAccount()
    {
        var handler = new FakeIBKRMultiHandler(new Dictionary<string, (string body, HttpStatusCode code)>
        {
            ["/v1/api/iserver/accounts"] = (@"{""accounts"":[""U1234567"",""U9999999""]}", HttpStatusCode.OK),
        });

        var adapter = CreateAdapter(CreateClient(handler));
        var accountId = await adapter.GetAccountIdAsync();

        accountId.Should().Be("U1234567");
    }

    [Fact]
    public async Task GetPositionsAsync_ParsesPositionsCorrectly()
    {
        var positionsJson = @"[
            {""conid"":265598,""contractDesc"":""AAPL"",""assetClass"":""STK"",""position"":100,""mktPrice"":175.5,""mktValue"":17550.0},
            {""conid"":272093,""contractDesc"":""MSFT"",""assetClass"":""STK"",""position"":50,""mktPrice"":420.0,""mktValue"":21000.0}
        ]";

        var handler = new FakeIBKRMultiHandler(new Dictionary<string, (string body, HttpStatusCode code)>
        {
            ["/v1/api/portfolio/U1234567/positions/0"] = (positionsJson, HttpStatusCode.OK),
        });

        var adapter = CreateAdapter(CreateClient(handler));
        var positions = await adapter.GetPositionsAsync("U1234567");

        positions.Should().HaveCount(2);
        positions[0].Symbol.Should().Be("AAPL");
        positions[0].InstrumentType.Should().Be("STK");
        positions[0].Quantity.Should().Be(100m);
        positions[0].UsdValue.Should().Be(17550m);
        positions[1].Symbol.Should().Be("MSFT");
        positions[1].UsdValue.Should().Be(21000m);
    }

    [Fact]
    public async Task GetPositionsAsync_IncludesZeroValuePositions()
    {
        var positionsJson = @"[
            {""conid"":265598,""contractDesc"":""AAPL"",""assetClass"":""STK"",""position"":100,""mktPrice"":175.5,""mktValue"":17550.0},
            {""conid"":999999,""contractDesc"":""EXPOPT"",""assetClass"":""OPT"",""position"":1,""mktPrice"":0.0,""mktValue"":0.0}
        ]";

        var handler = new FakeIBKRMultiHandler(new Dictionary<string, (string body, HttpStatusCode code)>
        {
            ["/v1/api/portfolio/U1234567/positions/0"] = (positionsJson, HttpStatusCode.OK),
        });

        var adapter = CreateAdapter(CreateClient(handler));
        var positions = await adapter.GetPositionsAsync("U1234567");

        positions.Should().HaveCount(2);
        positions[1].Symbol.Should().Be("EXPOPT");
        positions[1].UsdValue.Should().Be(0m);
    }
}

// ── Test doubles ──────────────────────────────────────────────────────────────

public sealed class FakeIBKRMultiHandler(Dictionary<string, (string body, HttpStatusCode code)> responses)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.PathAndQuery ?? string.Empty;
        var matchedKey = responses.Keys.FirstOrDefault(k => url.StartsWith(k, StringComparison.OrdinalIgnoreCase));

        if (matchedKey is null)
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

        var (body, code) = responses[matchedKey];
        return Task.FromResult(new HttpResponseMessage(code)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
    }
}
