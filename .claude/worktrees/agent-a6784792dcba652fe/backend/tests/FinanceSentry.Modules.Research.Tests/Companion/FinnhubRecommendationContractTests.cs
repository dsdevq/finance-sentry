namespace FinanceSentry.Modules.Research.Tests.Companion;

using FinanceSentry.Modules.Research.Infrastructure.Sources;
using FluentAssertions;
using Xunit;

/// <summary>
/// External-contract test (constitution-mandated) for Finnhub's <c>/stock/recommendation</c> JSON
/// shape (feature 037). Contract: specs/037-structured-data-sources/contracts/finnhub-recommendation.md.
/// Asserts the parser reads period + the five consensus counts, tolerates provider additions, and
/// fails loudly only on a structurally-broken body.
/// </summary>
public sealed class FinnhubRecommendationContractTests
{
    private const string SampleJson = """
    [
      { "symbol": "MU", "period": "2026-08-01", "strongBuy": 14, "buy": 20, "hold": 6, "sell": 1, "strongSell": 0 },
      { "symbol": "MU", "period": "2026-07-01", "strongBuy": 13, "buy": 21, "hold": 6, "sell": 1, "strongSell": 0 }
    ]
    """;

    [Fact]
    public void Parse_reads_period_and_all_five_counts()
    {
        var trends = FinnhubRecommendationTrendsService.Parse(SampleJson, "mu");

        trends.Should().HaveCount(2);

        var latest = trends[0];
        latest.Ticker.Should().Be("MU", "the caller's canonical ticker wins over the provider echo");
        latest.Period.Should().Be(new DateOnly(2026, 8, 1));
        latest.StrongBuy.Should().Be(14);
        latest.Buy.Should().Be(20);
        latest.Hold.Should().Be(6);
        latest.Sell.Should().Be(1);
        latest.StrongSell.Should().Be(0);

        trends[1].Period.Should().Be(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public void Parse_ignores_unknown_fields_and_defaults_missing_counts_to_zero()
    {
        const string json = """
        [
          { "symbol": "AAPL", "period": "2026-08-01", "strongBuy": 5, "buy": 10, "newProviderField": "x" }
        ]
        """;

        var trends = FinnhubRecommendationTrendsService.Parse(json, "AAPL");

        trends.Should().HaveCount(1);
        trends[0].StrongBuy.Should().Be(5);
        trends[0].Buy.Should().Be(10);
        trends[0].Hold.Should().Be(0);
        trends[0].Sell.Should().Be(0);
        trends[0].StrongSell.Should().Be(0);
    }

    [Fact]
    public void Parse_skips_rows_with_malformed_period_without_throwing()
    {
        const string json = """
        [
          { "symbol": "AAPL", "period": "not-a-date", "strongBuy": 5 },
          { "symbol": "AAPL", "period": "2026-08-01", "strongBuy": 5 }
        ]
        """;

        var trends = FinnhubRecommendationTrendsService.Parse(json, "AAPL");

        trends.Should().HaveCount(1);
        trends[0].Period.Should().Be(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public void Parse_skips_zero_coverage_rows()
    {
        const string json = """
        [
          { "symbol": "TINY", "period": "2026-08-01", "strongBuy": 0, "buy": 0, "hold": 0, "sell": 0, "strongSell": 0 }
        ]
        """;

        FinnhubRecommendationTrendsService.Parse(json, "TINY")
            .Should().BeEmpty("all-zero months carry no signal (no coverage)");
    }

    [Fact]
    public void Parse_returns_empty_for_empty_array()
    {
        FinnhubRecommendationTrendsService.Parse("[]", "ZZZZ").Should().BeEmpty();
    }

    [Fact]
    public void Parse_throws_on_non_array_root()
    {
        const string errorBody = """{ "error": "Invalid API key" }""";

        var act = () => FinnhubRecommendationTrendsService.Parse(errorBody, "MU");

        act.Should().Throw<AnalystSourceParseException>(
            "a structurally-unexpected body must fail loudly, not read as 'no data'");
    }

    [Fact]
    public void Parse_throws_on_html_body()
    {
        var act = () => FinnhubRecommendationTrendsService.Parse("<html>challenge</html>", "MU");

        act.Should().Throw<AnalystSourceParseException>();
    }

    /// <summary>
    /// Live smoke against the real API — runs only when FINNHUB_API_KEY is present in the
    /// environment (deploy-time/dev check; CI has no key and skips via early return).
    /// </summary>
    [Fact]
    public async Task Live_smoke_real_endpoint_conforms_when_key_present()
    {
        var apiKey = Environment.GetEnvironmentVariable("FINNHUB_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return;
        }

        using var client = new HttpClient { BaseAddress = new Uri("https://finnhub.io/api/v1/") };
        client.DefaultRequestHeaders.Add("X-Finnhub-Token", apiKey);

        var json = await client.GetStringAsync("stock/recommendation?symbol=AAPL");
        var trends = FinnhubRecommendationTrendsService.Parse(json, "AAPL");

        trends.Should().NotBeEmpty("AAPL always has analyst coverage");
        trends[0].Buy.Should().BeGreaterThanOrEqualTo(0);
    }
}
