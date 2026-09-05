namespace FinanceSentry.Tests.Unit.Subscriptions;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Subscriptions.Application.Services;
using FinanceSentry.Modules.Subscriptions.Domain;
using FinanceSentry.Modules.Subscriptions.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for the cross-module <c>IActiveSubscriptionsReader</c> adapter (#538).
/// </summary>
public class ActiveSubscriptionsReaderTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static DetectedSubscription Make(
        string normalized, string display, string kind = SubscriptionKinds.Subscription)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return DetectedSubscription.Create(
            UserId.ToString(), normalized, display, "monthly", 15m, 15m, "EUR",
            today.AddDays(-30), today, occurrenceCount: 3, confidenceScore: 3,
            category: null, kind: kind);
    }

    private static (ActiveSubscriptionsReader reader, Mock<IDetectedSubscriptionRepository> repo) MakeSut(
        params DetectedSubscription[] rows)
    {
        var repo = new Mock<IDetectedSubscriptionRepository>();
        repo.Setup(r => r.GetActiveByUserIdAsync(UserId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);
        return (new ActiveSubscriptionsReader(repo.Object), repo);
    }

    [Fact]
    public async Task GetActiveCommitmentMerchantKeys_ReadsOnlyActiveRows()
    {
        // Status filtering lives in the repository's active-only query; going through
        // GetByUserIdAsync would silently pull dismissed and completed commitments in.
        var (reader, repo) = MakeSut(Make("netflix", "Netflix"));

        await reader.GetActiveCommitmentMerchantKeysAsync(UserId);

        repo.Verify(r => r.GetActiveByUserIdAsync(UserId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(
            r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetActiveCommitmentMerchantKeys_IncludesInstallments()
    {
        // Unlike GetActiveSubscriptionsAsync, which is scoped to recurring services for
        // cash-flow projection, "committed spend" covers every kind of active commitment.
        var (reader, _) = MakeSut(
            Make("netflix", "Netflix"),
            Make("installment:allo:2340", "Алло", SubscriptionKinds.Installment));

        var keys = await reader.GetActiveCommitmentMerchantKeysAsync(UserId);

        keys.Should().BeEquivalentTo(["netflix", "installment:allo:2340"]);
    }

    [Fact]
    public async Task GetActiveSubscriptions_StillExcludesInstallments()
    {
        // Regression guard: the new method must not have widened the existing one, which
        // Liquidity's cash-flow projection depends on.
        var (reader, _) = MakeSut(
            Make("netflix", "Netflix"),
            Make("installment:allo:2340", "Алло", SubscriptionKinds.Installment));

        var summaries = await reader.GetActiveSubscriptionsAsync(UserId);

        summaries.Should().ContainSingle().Which.MerchantNameDisplay.Should().Be("Netflix");
    }

    [Fact]
    public async Task GetActiveCommitmentMerchantKeys_NoActiveCommitments_ReturnsEmptySet()
    {
        var (reader, _) = MakeSut();

        var keys = await reader.GetActiveCommitmentMerchantKeysAsync(UserId);

        keys.Should().BeEmpty();
    }
}
