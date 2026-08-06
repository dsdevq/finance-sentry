namespace FinanceSentry.Tests.Unit.Fx;

using System.Net;
using System.Text;
using FinanceSentry.Infrastructure.Fx;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class OpenErApiExchangeRateProviderTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
            });
    }

    private static OpenErApiExchangeRateProvider Build(HttpStatusCode status, string body)
    {
        var http = new HttpClient(new StubHandler(status, body))
        {
            BaseAddress = new Uri("https://open.er-api.com/"),
        };
        return new OpenErApiExchangeRateProvider(http, NullLogger<OpenErApiExchangeRateProvider>.Instance);
    }

    [Fact]
    public async Task GetUsdRatesAsync_InvertsUnitsPerUsdToUsdPerUnit()
    {
        const string body =
            """{"result":"success","base_code":"USD","rates":{"USD":1,"EUR":0.9,"UAH":40}}""";
        var provider = Build(HttpStatusCode.OK, body);

        var rates = await provider.GetUsdRatesAsync();

        rates.Should().NotBeNull();
        rates!["EUR"].Should().BeApproximately(1.1111m, 0.0001m); // 1 / 0.9
        rates["UAH"].Should().Be(0.025m); // 1 / 40
        rates["USD"].Should().Be(1m);
    }

    [Fact]
    public async Task GetUsdRatesAsync_ReturnsNull_OnNonSuccessResult()
    {
        const string body = """{"result":"error","rates":{"EUR":0.9}}""";
        var provider = Build(HttpStatusCode.OK, body);

        (await provider.GetUsdRatesAsync()).Should().BeNull();
    }

    [Fact]
    public async Task GetUsdRatesAsync_ReturnsNull_OnHttpError()
    {
        var provider = Build(HttpStatusCode.InternalServerError, "oops");

        (await provider.GetUsdRatesAsync()).Should().BeNull();
    }

    [Fact]
    public async Task GetUsdRatesAsync_SkipsZeroRates()
    {
        const string body =
            """{"result":"success","base_code":"USD","rates":{"EUR":0.9,"BAD":0}}""";
        var provider = Build(HttpStatusCode.OK, body);

        var rates = await provider.GetUsdRatesAsync();

        rates.Should().NotBeNull();
        rates!.Should().ContainKey("EUR");
        rates.Should().NotContainKey("BAD");
    }
}
