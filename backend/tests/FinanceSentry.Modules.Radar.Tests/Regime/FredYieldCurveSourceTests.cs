using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Infrastructure.MarketData;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinanceSentry.Modules.Radar.Tests.Regime;

public sealed class FredYieldCurveSourceTests
{
    private sealed class ThrowingHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => throw new InvalidOperationException("HTTP must not be called when keyless");
    }

    private static FredYieldCurveSource Source(RegimeOptions options)
        => new(new ThrowingHttpClientFactory(), Options.Create(options), NullLogger<FredYieldCurveSource>.Instance);

    [Fact]
    public void IsConfigured_False_WhenKeyBlank()
    {
        var source = Source(new RegimeOptions());
        source.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_False_WhenDisabled_EvenWithKey()
    {
        var opts = new RegimeOptions();
        opts.Fred.Enabled = false;
        opts.Fred.ApiKey = "abc";
        Source(opts).IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsNull_AndIssuesNoRequest_WhenKeyless()
    {
        var source = Source(new RegimeOptions()); // blank key; factory throws if called
        var result = await source.GetLatestAsync();
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_SkipsDotPlaceholders_AndParsesNumbers()
    {
        const string json = """
        {
          "observations": [
            { "date": "2026-08-08", "value": "." },
            { "date": "2026-08-07", "value": "3.69" },
            { "date": "2026-08-06", "value": "3.71" }
          ]
        }
        """;

        var parsed = FredYieldCurveSource.Parse(json);
        parsed.Should().HaveCount(2);
        parsed.Select(p => p.Value).Should().BeEquivalentTo(new[] { 3.69m, 3.71m });
    }

    [Fact]
    public void Parse_LatestPicksMostRecentValidByDate()
    {
        const string json = """
        {
          "observations": [
            { "date": "2026-08-08", "value": "." },
            { "date": "2026-08-07", "value": "3.69" },
            { "date": "2026-08-06", "value": "3.71" }
          ]
        }
        """;

        var latest = Domain.Regime.YieldObservation.Latest(FredYieldCurveSource.Parse(json));
        latest.Should().NotBeNull();
        latest!.Date.Should().Be(new DateOnly(2026, 8, 7));
        latest.Value.Should().Be(3.69m);
    }

    [Fact]
    public void Parse_Throws_WhenNoObservationsArray()
    {
        Action act = () => FredYieldCurveSource.Parse("""{ "error_code": 400, "error_message": "Bad Request" }""");
        act.Should().Throw<FredParseException>();
    }

    [Fact]
    public void Parse_Throws_OnNonJson()
    {
        Action act = () => FredYieldCurveSource.Parse("<html>challenge</html>");
        act.Should().Throw<FredParseException>();
    }

    [Fact]
    public void Parse_ReturnsEmpty_WhenAllPlaceholders()
    {
        const string json = """{ "observations": [ { "date": "2026-08-08", "value": "." } ] }""";
        FredYieldCurveSource.Parse(json).Should().BeEmpty();
    }
}
