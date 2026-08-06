using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Tools;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinanceSentry.Mcp.Tests;

public sealed class GetResearchContextToolTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IQueryHandler<GetResearchContextQuery, ResearchContextPacketDto>> _handler = new();

    private GetResearchContextTool CreateTool(Guid? resolvedUserId = null)
        => new(_handler.Object, new FakeIdentityResolver { ResolvedUserId = resolvedUserId });

    [Fact]
    public async Task ExecuteAsync_ReturnsNull_WhenIdentityUnresolved()
    {
        var tool = CreateTool(resolvedUserId: null);

        var result = await tool.ExecuteAsync(ticker: "MU");

        result.Should().BeNull();
        _handler.Verify(
            h => h.Handle(It.IsAny<GetResearchContextQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsNull_WhenBothThesisIdAndTickerMissing()
    {
        var tool = CreateTool(UserId);

        var result = await tool.ExecuteAsync();

        result.Should().BeNull();
        _handler.Verify(
            h => h.Handle(It.IsAny<GetResearchContextQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UsesAuthenticatedIdentity_AsQueryUser()
    {
        GetResearchContextQuery? captured = null;
        _handler
            .Setup(h => h.Handle(It.IsAny<GetResearchContextQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetResearchContextQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(new ResearchContextPacketDto(
                "Ticker", null, "MU", null, [], 0, DateTimeOffset.UtcNow));
        var tool = CreateTool(UserId);

        await tool.ExecuteAsync(ticker: "MU", question: "what changed?", maxChunks: 8);

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(UserId);
        captured.Ticker.Should().Be("MU");
        captured.Question.Should().Be("what changed?");
        captured.MaxChunks.Should().Be(8);
    }
}
