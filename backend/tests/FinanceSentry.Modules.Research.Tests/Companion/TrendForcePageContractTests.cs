namespace FinanceSentry.Modules.Research.Tests.Companion;

using FinanceSentry.Modules.Research.Infrastructure.Sources;
using FluentAssertions;
using Xunit;

/// <summary>
/// External-contract test (constitution-mandated) for the TrendForce press-center article-list
/// structure (feature 030, T038). Fixture-based; a live smoke test is provided but skipped by default.
/// Structural drift MUST throw <see cref="NewsSourceParseException"/>.
/// </summary>
public sealed class TrendForcePageContractTests
{
    private const string PageUrl = "https://www.trendforce.com/presscenter/news";

    private const string Fixture = """
    <html><body>
      <div class="press-list">
        <article>
          <a href="/presscenter/news/20260721-12345.html"><h3 class="title">DRAM prices rise on HBM demand</h3></a>
          <time datetime="2026-07-21T00:00:00Z">Jul 21, 2026</time>
          <p class="summary">Contract DRAM prices climbed as HBM allocation tightened supply.</p>
        </article>
        <article>
          <a href="https://www.trendforce.com/presscenter/news/20260720-99999.html"><h3 class="title">NAND flash quarterly update</h3></a>
          <time datetime="2026-07-20">Jul 20, 2026</time>
        </article>
      </div>
    </body></html>
    """;

    private const string CurrentShapeFixture = """
    <html><body>
      <div class="navbar navbar-inner press-news-list">
        <div class="list-items text-left">
          <div class="list-item">
            <div class="row">
              <div class="col-md-12 margin-t-20 padding-l-r-15">
                <h3 class="font-size-18"><a class="title-link" href="/presscenter/news/20260709-13140.html"><strong>Long-Term Agreements Cap Price Increases; Server DRAM Contract Prices Expected to Rise 13-18% QoQ in 3Q26, Says TrendForce</strong></a></h3>
                <h4 class="font-size-16 color-green margin-t-10"><strong>9 July 2026</strong></h4>
                <p class="font-size-16">TrendForce's latest memory pricing survey indicates expected price hikes.</p>
              </div>
            </div>
          </div>
        </div>
      </div>
      <ul class="list"><li><a href="/presscenter/news/Semiconductors">Semiconductors</a></li></ul>
    </body></html>
    """;

    [Fact]
    public async Task Parse_extracts_title_url_and_date_from_article_list()
    {
        var articles = await TrendForcePageSource.ParseAsync(Fixture, PageUrl);

        articles.Should().HaveCount(2);

        var first = articles[0];
        first.Title.Should().Be("DRAM prices rise on HBM demand");
        first.Url.Should().Be("https://www.trendforce.com/presscenter/news/20260721-12345.html",
            "a relative href must resolve against the page URL");
        first.PublishedAt.Should().Be(DateTimeOffset.Parse("2026-07-21T00:00:00Z"));
        first.Summary.Should().Contain("HBM allocation");

        articles[1].Title.Should().Be("NAND flash quarterly update");
        articles[1].Url.Should().Be("https://www.trendforce.com/presscenter/news/20260720-99999.html");
    }

    [Fact]
    public async Task Parse_throws_when_article_list_is_missing()
    {
        const string drifted = "<html><body><div>no articles here</div></body></html>";

        var act = async () => await TrendForcePageSource.ParseAsync(drifted, PageUrl);

        await act.Should().ThrowAsync<NewsSourceParseException>();
    }

    [Fact]
    public async Task Parse_extracts_current_press_news_list_shape()
    {
        var articles = await TrendForcePageSource.ParseAsync(CurrentShapeFixture, PageUrl);

        articles.Should().ContainSingle();
        articles[0].Title.Should().Contain("Server DRAM Contract Prices");
        articles[0].Url.Should().Be("https://www.trendforce.com/presscenter/news/20260709-13140.html");
        articles[0].PublishedAt.Should().Be(DateTimeOffset.Parse("9 July 2026"));
        articles[0].Summary.Should().Contain("memory pricing");
    }

    [Fact(Skip = "Live network smoke test — run manually to confirm TrendForce markup still parses.")]
    public async Task Live_trendforce_page_still_parses()
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        var html = await http.GetStringAsync(PageUrl);

        var articles = await TrendForcePageSource.ParseAsync(html, PageUrl);

        articles.Should().NotBeEmpty();
    }
}
