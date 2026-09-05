namespace FinanceSentry.Tests.Unit.BankSync.Application;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

/// <summary>
/// The dashboard composes cash flow (and therefore the savings rate) and top categories from
/// ONE counterparty classification. Classifying per reader would let the same month's rent be
/// income in one widget and a transfer in another.
/// </summary>
public class DashboardQueryCounterpartyTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task GetDashboardData_ClassifiesCounterpartiesOnce_AndSharesTheResult()
    {
        var classification = new CounterpartyClassificationResult(
            [],
            [new CounterpartyMonthlyFlow("2026-05", "Mom", FlowRoles.FamilySupport, 0m, 50m)]);

        var counterparties = new Mock<ICounterpartyClassificationService>();
        counterparties.Setup(s => s.ClassifyForWindowAsync(UserId, 6, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(classification);

        CounterpartyClassificationResult? seenByFlow = null;
        CounterpartyClassificationResult? seenByCategories = null;

        var moneyFlow = new Mock<IMoneyFlowStatisticsService>();
        moneyFlow.Setup(s => s.GetMonthlyFlowAsync(UserId, It.IsAny<CounterpartyClassificationResult>(), 6, It.IsAny<CancellationToken>()))
                 .Callback<Guid, CounterpartyClassificationResult, int, CancellationToken>((_, c, _, _) => seenByFlow = c)
                 .ReturnsAsync([]);

        var categories = new Mock<IMerchantCategoryStatisticsService>();
        categories.Setup(s => s.GetTopCategoriesAsync(UserId, It.IsAny<CounterpartyClassificationResult>(), It.IsAny<int>(), 6, It.IsAny<CancellationToken>()))
                  .Callback<Guid, CounterpartyClassificationResult, int, int, CancellationToken>((_, c, _, _, _) => seenByCategories = c)
                  .ReturnsAsync([]);

        var aggregation = new Mock<IAggregationService>();
        aggregation.Setup(s => s.GetAggregatedBalanceAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        aggregation.Setup(s => s.GetAccountCountByTypeAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = new DashboardQueryService(
            aggregation.Object,
            moneyFlow.Object,
            categories.Object,
            counterparties.Object,
            new Mock<ISyncJobRepository>().Object);

        await sut.GetDashboardDataAsync(UserId, months: 6);

        counterparties.Verify(
            s => s.ClassifyForWindowAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
        seenByFlow.Should().BeSameAs(classification);
        seenByCategories.Should().BeSameAs(classification);
    }
}
