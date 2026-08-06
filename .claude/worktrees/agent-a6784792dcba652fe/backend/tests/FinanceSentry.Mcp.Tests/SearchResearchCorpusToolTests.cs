using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Tools;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using FinanceSentry.Modules.Research.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinanceSentry.Mcp.Tests;

public sealed class SearchResearchCorpusToolTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IQueryHandler<SearchResearchCorpusQuery, ResearchSearchResultDto>> _handler = new();

    private SearchResearchCorpusTool CreateTool(Guid? resolvedUserId = null)
        => new(_handler.Object, new FakeIdentityResolver { ResolvedUserId = resolvedUserId });

    [Fact]
    public async Task ExecuteAsync_ReturnsEmpty_WhenIdentityUnresolved()
    {
        var tool = CreateTool(resolvedUserId: null);

        var result = await tool.ExecuteAsync("memory pricing");

        result.Results.Should().BeEmpty();
        _handler.Verify(
            h => h.Handle(It.IsAny<SearchResearchCorpusQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmpty_ForBlankQuery()
    {
        var tool = CreateTool(UserId);

        var result = await tool.ExecuteAsync("   ");

        result.Results.Should().BeEmpty();
        _handler.Verify(
            h => h.Handle(It.IsAny<SearchResearchCorpusQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UsesAuthenticatedIdentity_AsQueryUser()
    {
        SearchResearchCorpusQuery? captured = null;
        _handler
            .Setup(h => h.Handle(It.IsAny<SearchResearchCorpusQuery>(), It.IsAny<CancellationToken>()))
            .Callback<SearchResearchCorpusQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(new ResearchSearchResultDto("q", [], DateTimeOffset.UtcNow));
        var tool = CreateTool(UserId);

        await tool.ExecuteAsync("memory pricing", tickers: ["mu"], limit: 5);

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(UserId);
        captured.Tickers.Should().Equal("mu");
        captured.Limit.Should().Be(5);
    }

    [Fact]
    public void ParseSourceTypes_IgnoresUnknownValues_AndParsesCaseInsensitively()
    {
        var parsed = SearchResearchCorpusTool.ParseSourceTypes(
            ["newsarticle", "InvestmentThesis", "not-a-source-type"]);

        parsed.Should().Equal(
            ResearchDocumentSourceType.NewsArticle,
            ResearchDocumentSourceType.InvestmentThesis);
    }
}
