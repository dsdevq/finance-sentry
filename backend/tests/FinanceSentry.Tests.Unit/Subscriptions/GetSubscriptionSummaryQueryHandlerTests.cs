namespace FinanceSentry.Tests.Unit.Subscriptions;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Subscriptions.Application.Queries;
using FinanceSentry.Modules.Subscriptions.Domain;
using FinanceSentry.Modules.Subscriptions.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

public class GetSubscriptionSummaryQueryHandlerTests
{
    private const string UserId = "user-1";

    private static DetectedSubscription ActiveSub(decimal averageAmount, string currency, string cadence = "monthly") =>
        DetectedSubscription.Create(
            UserId,
            merchantNameNormalized: "svc",
            merchantNameDisplay: "Svc",
            cadence: cadence,
            averageAmount: averageAmount,
            lastKnownAmount: averageAmount,
            currency: currency,
            lastChargeDate: new DateOnly(2026, 7, 1),
            nextExpectedDate: new DateOnly(2026, 8, 1),
            occurrenceCount: 3,
            confidenceScore: 90,
            category: null);

    private static DetectedSubscription ActiveInstallment(decimal averageAmount, string currency) =>
        DetectedSubscription.Create(
            UserId,
            merchantNameNormalized: "rozetka",
            merchantNameDisplay: "Rozetka",
            cadence: "monthly",
            averageAmount: averageAmount,
            lastKnownAmount: averageAmount,
            currency: currency,
            lastChargeDate: new DateOnly(2026, 7, 1),
            nextExpectedDate: new DateOnly(2026, 8, 1),
            occurrenceCount: 3,
            confidenceScore: 90,
            category: null,
            kind: SubscriptionKinds.Installment);

    private static GetSubscriptionSummaryQueryHandler BuildHandler(params DetectedSubscription[] active)
    {
        var repo = new Mock<IDetectedSubscriptionRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetActiveByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);
        repo.Setup(r => r.GetByUserIdAsync(UserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);
        return new GetSubscriptionSummaryQueryHandler(repo.Object);
    }

    [Fact]
    public async Task Handle_ExcludesInstallments_FromTotalAndCount()
    {
        // 10 USD subscription + a 100 USD installment — only the subscription counts.
        var handler = BuildHandler(
            ActiveSub(10m, "USD"),
            ActiveInstallment(100m, "USD"));

        var result = await handler.Handle(new GetSubscriptionSummaryQuery(UserId), CancellationToken.None);

        result.TotalMonthlyEstimate.Should().Be(10m);
        result.ActiveCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_MixedCurrencies_NormalizesToUsdBeforeSumming()
    {
        // 500 UAH (~$12) + 10 EUR (~$10.80) must NOT be summed as bare 510.
        var handler = BuildHandler(
            ActiveSub(500m, "UAH"),
            ActiveSub(10m, "EUR"));

        var result = await handler.Handle(new GetSubscriptionSummaryQuery(UserId), CancellationToken.None);

        // 500 * 0.024 + 10 * 1.08 = 12.00 + 10.80 = 22.80
        result.TotalMonthlyEstimate.Should().Be(22.80m);
        result.TotalAnnualEstimate.Should().Be(273.60m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_AnnualCadence_DividesByTwelveThenConverts()
    {
        var handler = BuildHandler(ActiveSub(120m, "EUR", cadence: "annual"));

        var result = await handler.Handle(new GetSubscriptionSummaryQuery(UserId), CancellationToken.None);

        // (120 / 12) * 1.08 = 10 * 1.08 = 10.80
        result.TotalMonthlyEstimate.Should().Be(10.80m);
    }

    [Fact]
    public async Task Handle_NoActiveSubscriptions_ReturnsZeroInUsd()
    {
        var handler = BuildHandler();

        var result = await handler.Handle(new GetSubscriptionSummaryQuery(UserId), CancellationToken.None);

        result.TotalMonthlyEstimate.Should().Be(0m);
        result.ActiveCount.Should().Be(0);
        result.Currency.Should().Be("USD");
    }
}
