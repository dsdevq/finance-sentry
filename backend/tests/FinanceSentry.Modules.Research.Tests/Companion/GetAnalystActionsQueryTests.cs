namespace FinanceSentry.Modules.Research.Tests.Companion;

using FinanceSentry.Modules.Research.Application.Queries;
using FinanceSentry.Modules.Research.Domain;
using FluentAssertions;
using Xunit;

/// <summary>
/// Coverage-flag logic for the analyst-actions query (feature 030, T029): distinguishes
/// in-universe / not-in-universe / market-wide so Ledger can tell "no coverage" from "no recent news".
/// </summary>
public sealed class GetAnalystActionsQueryTests
{
    private static readonly DateOnly Since = new(2026, 7, 1);

    [Fact]
    public async Task Ticker_query_includes_latest_recommendation_trends()
    {
        var trends = new FakeRecommendationTrendRepository();
        trends.Trends.Add(new RecommendationTrend
        {
            Ticker = "MU", Period = new DateOnly(2026, 8, 1),
            StrongBuy = 14, Buy = 20, Hold = 6, Sell = 1, StrongSell = 0, Source = "finnhub",
        });
        trends.Trends.Add(new RecommendationTrend
        {
            Ticker = "MU", Period = new DateOnly(2026, 7, 1), Buy = 21, Source = "finnhub",
        });
        trends.Trends.Add(new RecommendationTrend
        {
            Ticker = "NVDA", Period = new DateOnly(2026, 8, 1), Buy = 50, Source = "finnhub",
        });

        var result = await CreateHandler(trends: trends)
            .Handle(new GetAnalystActionsQuery("mu", Since, null, 50), default);

        result.RecommendationTrends.Should().NotBeNull();
        result.RecommendationTrends.Should().HaveCount(2, "only the queried ticker's months belong in the block");
        result.RecommendationTrends![0].Period.Should().Be(new DateOnly(2026, 8, 1), "newest month first");
        result.RecommendationTrends[0].StrongBuy.Should().Be(14);
        result.RecommendationTrends[0].Source.Should().Be("finnhub");
    }

    [Fact]
    public async Task Ticker_query_without_trend_rows_returns_empty_block_not_null()
    {
        var result = await CreateHandler()
            .Handle(new GetAnalystActionsQuery("MU", Since, null, 50), default);

        result.RecommendationTrends.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task Market_wide_query_omits_the_trends_block()
    {
        var trends = new FakeRecommendationTrendRepository();
        trends.Trends.Add(new RecommendationTrend
        {
            Ticker = "MU", Period = new DateOnly(2026, 8, 1), Buy = 20, Source = "finnhub",
        });

        var result = await CreateHandler(trends: trends)
            .Handle(new GetAnalystActionsQuery(null, Since, null, 50), default);

        result.RecommendationTrends.Should().BeNull("a whole-universe query carries no single-ticker consensus");
    }

    private static GetAnalystActionsQueryHandler CreateHandler(
        FakeAnalystActionRepository? actions = null,
        FakeAnalystUniverseRepository? universe = null,
        FakeRecommendationTrendRepository? trends = null)
        => new(
            actions ?? new FakeAnalystActionRepository(),
            universe ?? new FakeAnalystUniverseRepository(),
            trends ?? new FakeRecommendationTrendRepository());

    [Fact]
    public async Task Ticker_in_universe_reports_inUniverse_coverage()
    {
        var actions = new FakeAnalystActionRepository();
        var universe = new FakeAnalystUniverseRepository();
        universe.Members.Add(new AnalystUniverseMember { Ticker = "MU", Active = true });

        var result = await new GetAnalystActionsQueryHandler(actions, universe, new FakeRecommendationTrendRepository())
            .Handle(new GetAnalystActionsQuery("MU", Since, null, 50), default);

        result.Coverage.Should().Be("inUniverse");
    }

    [Fact]
    public async Task Ticker_not_in_universe_reports_notInUniverse_coverage()
    {
        var result = await new GetAnalystActionsQueryHandler(
                new FakeAnalystActionRepository(), new FakeAnalystUniverseRepository(),
                new FakeRecommendationTrendRepository())
            .Handle(new GetAnalystActionsQuery("DOGE", Since, null, 50), default);

        result.Coverage.Should().Be("notInUniverse");
    }

    [Fact]
    public async Task No_ticker_reports_marketWide_coverage()
    {
        var result = await new GetAnalystActionsQueryHandler(
                new FakeAnalystActionRepository(), new FakeAnalystUniverseRepository(),
                new FakeRecommendationTrendRepository())
            .Handle(new GetAnalystActionsQuery(null, Since, null, 50), default);

        result.Coverage.Should().Be("marketWide");
    }

    [Fact]
    public async Task ActionType_string_is_parsed_and_passed_to_the_repository()
    {
        var actions = new FakeAnalystActionRepository();

        await new GetAnalystActionsQueryHandler(actions, new FakeAnalystUniverseRepository(), new FakeRecommendationTrendRepository())
            .Handle(new GetAnalystActionsQuery(null, Since, "downgrade", 50), default);

        actions.LastTypeFilter.Should().Be(AnalystActionType.Downgrade);
    }

    [Fact]
    public async Task Actions_are_projected_to_dtos()
    {
        var actions = new FakeAnalystActionRepository();
        actions.Actions.Add(new AnalystAction
        {
            Ticker = "MU", Firm = "Morgan Stanley", ActionType = AnalystActionType.Upgrade,
            NewRating = "Overweight", NewTarget = 135m, ActionDate = new DateOnly(2026, 7, 20),
            Source = "marketbeat",
        });

        var result = await new GetAnalystActionsQueryHandler(actions, new FakeAnalystUniverseRepository(), new FakeRecommendationTrendRepository())
            .Handle(new GetAnalystActionsQuery("MU", Since, null, 50), default);

        result.Actions.Should().ContainSingle();
        result.Actions[0].ActionType.Should().Be("Upgrade");
        result.Actions[0].Source.Should().Be("marketbeat");
    }

    [Fact]
    public async Task ReferenceId_resolves_exact_source_row()
    {
        var id = Guid.NewGuid();
        var actions = new FakeAnalystActionRepository();
        actions.Actions.Add(new AnalystAction
        {
            Id = id,
            Ticker = "GRAB",
            Firm = "Barclays",
            ActionType = AnalystActionType.Reiterate,
            NewRating = "Overweight",
            ActionDate = new DateOnly(2026, 7, 23),
            Source = "marketbeat",
            SourceUrl = "https://www.marketbeat.com/ratings/",
        });

        var result = await new GetAnalystActionsQueryHandler(actions, new FakeAnalystUniverseRepository(), new FakeRecommendationTrendRepository())
            .Handle(new GetAnalystActionsQuery("NVDA", Since, null, 50, id), default);

        result.Coverage.Should().Be("reference");
        result.Actions.Should().ContainSingle();
        result.Actions[0].Ticker.Should().Be("GRAB");
        result.Actions[0].SourceUrl.Should().Be("https://www.marketbeat.com/ratings/");
    }
}
