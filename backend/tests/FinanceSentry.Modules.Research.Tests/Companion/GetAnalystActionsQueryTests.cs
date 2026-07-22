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
    public async Task Ticker_in_universe_reports_inUniverse_coverage()
    {
        var actions = new FakeAnalystActionRepository();
        var universe = new FakeAnalystUniverseRepository();
        universe.Members.Add(new AnalystUniverseMember { Ticker = "MU", Active = true });

        var result = await new GetAnalystActionsQueryHandler(actions, universe)
            .Handle(new GetAnalystActionsQuery("MU", Since, null, 50), default);

        result.Coverage.Should().Be("inUniverse");
    }

    [Fact]
    public async Task Ticker_not_in_universe_reports_notInUniverse_coverage()
    {
        var result = await new GetAnalystActionsQueryHandler(
                new FakeAnalystActionRepository(), new FakeAnalystUniverseRepository())
            .Handle(new GetAnalystActionsQuery("DOGE", Since, null, 50), default);

        result.Coverage.Should().Be("notInUniverse");
    }

    [Fact]
    public async Task No_ticker_reports_marketWide_coverage()
    {
        var result = await new GetAnalystActionsQueryHandler(
                new FakeAnalystActionRepository(), new FakeAnalystUniverseRepository())
            .Handle(new GetAnalystActionsQuery(null, Since, null, 50), default);

        result.Coverage.Should().Be("marketWide");
    }

    [Fact]
    public async Task ActionType_string_is_parsed_and_passed_to_the_repository()
    {
        var actions = new FakeAnalystActionRepository();

        await new GetAnalystActionsQueryHandler(actions, new FakeAnalystUniverseRepository())
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

        var result = await new GetAnalystActionsQueryHandler(actions, new FakeAnalystUniverseRepository())
            .Handle(new GetAnalystActionsQuery("MU", Since, null, 50), default);

        result.Actions.Should().ContainSingle();
        result.Actions[0].ActionType.Should().Be("Upgrade");
        result.Actions[0].Source.Should().Be("marketbeat");
    }
}
