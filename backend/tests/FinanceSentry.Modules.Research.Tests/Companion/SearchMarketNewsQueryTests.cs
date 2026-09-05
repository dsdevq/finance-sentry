namespace FinanceSentry.Modules.Research.Tests.Companion;

using FinanceSentry.Modules.Research.Application.Queries;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using FluentAssertions;
using Xunit;

public sealed class SearchMarketNewsQueryTests
{
    [Fact]
    public async Task Thesis_search_reports_degraded_coverage_when_attached_source_is_failing()
    {
        var thesisId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var sources = new FakeNewsSourceRepository();
        sources.Sources.Add(new NewsSource
        {
            Id = sourceId,
            Name = "TrendForce Press Center",
            Kind = NewsSourceKind.Page,
            Url = "https://www.trendforce.com/presscenter/news",
            ThesisId = thesisId,
            ConsecutiveFailures = 18,
            LastFailureReason = "article list not found",
        });

        var result = await new SearchMarketNewsQueryHandler(new FakeNewsRepository(), sources)
            .Handle(new SearchMarketNewsQuery(null, null, thesisId, null, 25), default);

        result.Coverage.Should().Be("degraded");
        result.SourceHealth.Should().ContainSingle();
        result.SourceHealth[0].Status.Should().Be("down");
        result.SourceHealth[0].ConsecutiveFailures.Should().Be(18);
    }

    private sealed class FakeNewsRepository : INewsRepository
    {
        public Task<IReadOnlyList<NewsArticle>> SearchAsync(
            string? query,
            IReadOnlyCollection<string>? tickers,
            Guid? thesisId,
            DateTimeOffset? since,
            int limit,
            CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<NewsArticle>>([]);

        public Task<IReadOnlyList<NewsArticle>> GetForTickerAsync(
            string ticker, DateTimeOffset? since, int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<NewsArticle>>([]);

        public Task<int> InsertNewAsync(IReadOnlyCollection<NewsArticle> articles, CancellationToken ct = default)
            => Task.FromResult(articles.Count);
    }
}
