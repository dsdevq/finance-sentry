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
    private const string PageUrl = "https://www.trendforce.com/presscenter/";

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
