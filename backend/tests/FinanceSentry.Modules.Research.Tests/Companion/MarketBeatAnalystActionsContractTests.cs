namespace FinanceSentry.Modules.Research.Tests.Companion;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Infrastructure.Sources;
using FluentAssertions;
using Xunit;

/// <summary>
/// External-contract test (constitution-mandated) for the MarketBeat daily ratings table structure
/// (feature 030, T017). Fixture-based; a live smoke test is provided but skipped by default so CI
/// never depends on the network. Structural drift MUST throw <see cref="AnalystSourceParseException"/>.
/// </summary>
public sealed class MarketBeatAnalystActionsContractTests
{
    private const string RatingsFixture = """
    <html><body>
      <table class="ratings-table">
        <thead>
          <tr><th>Company</th><th>Action</th><th>Brokerage</th><th>Rating Change</th><th>Price Target</th></tr>
        </thead>
        <tbody>
          <tr>
            <td><a href="/stocks/NASDAQ/MU/">Micron Technology</a><div class="ticker-area">MU</div></td>
            <td>Upgrade</td>
            <td>Morgan Stanley</td>
            <td>Equal-Weight → Overweight</td>
            <td>$98.00 → $135.00</td>
          </tr>
          <tr>
            <td><div class="ticker-area">AAPL</div><a href="/stocks/NASDAQ/AAPL/">Apple Inc.</a></td>
            <td>Boost Price Target</td>
            <td>Wedbush</td>
            <td>Outperform</td>
            <td>$250.00 → $270.00</td>
          </tr>
        </tbody>
      </table>
    </body></html>
    """;

    [Fact]
    public async Task Parse_extracts_ticker_firm_action_rating_and_targets()
    {
        var actions = await MarketBeatAnalystActionsSource.ParseAsync(RatingsFixture, "https://www.marketbeat.com/ratings/");

        actions.Should().HaveCount(2);

        var mu = actions[0];
        mu.Ticker.Should().Be("MU");
        mu.Firm.Should().Be("Morgan Stanley");
        mu.ActionType.Should().Be(AnalystActionType.Upgrade);
        mu.PriorRating.Should().Be("Equal-Weight");
        mu.NewRating.Should().Be("Overweight");
        mu.PriorTarget.Should().Be(98.00m);
        mu.NewTarget.Should().Be(135.00m);
        mu.SourceUrl.Should().Be("https://www.marketbeat.com/ratings/");

        var aapl = actions[1];
        aapl.Ticker.Should().Be("AAPL");
        aapl.ActionType.Should().Be(AnalystActionType.TargetChange);
        aapl.PriorTarget.Should().Be(250.00m);
        aapl.NewTarget.Should().Be(270.00m);
        aapl.NewRating.Should().Be("Outperform");
    }

    [Fact]
    public async Task Parse_throws_when_ratings_table_is_missing()
    {
        const string drifted = "<html><body><div>no table here</div></body></html>";

        var act = async () => await MarketBeatAnalystActionsSource.ParseAsync(drifted, null);

        await act.Should().ThrowAsync<AnalystSourceParseException>();
    }

    [Fact]
    public async Task Parse_throws_when_expected_column_renamed()
    {
        const string missingBrokerage = """
        <table><thead><tr><th>Company</th><th>Action</th><th>Rating</th></tr></thead>
        <tbody><tr><td><div class="ticker-area">MU</div></td><td>Upgrade</td><td>Buy</td></tr></tbody></table>
        """;

        var act = async () => await MarketBeatAnalystActionsSource.ParseAsync(missingBrokerage, null);

        await act.Should().ThrowAsync<AnalystSourceParseException>();
    }

    [Fact(Skip = "Live network smoke test — run manually to confirm MarketBeat markup still parses.")]
    public async Task Live_marketbeat_page_still_parses()
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        var html = await http.GetStringAsync("https://www.marketbeat.com/ratings/");

        var actions = await MarketBeatAnalystActionsSource.ParseAsync(html, "https://www.marketbeat.com/ratings/");

        actions.Should().NotBeEmpty();
    }
}
