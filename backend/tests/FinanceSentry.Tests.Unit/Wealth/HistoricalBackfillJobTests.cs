namespace FinanceSentry.Tests.Unit.Wealth;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Wealth.Infrastructure.Jobs;
using FluentAssertions;
using Moq;
using Xunit;

public class HistoricalBackfillJobTests
{
    private static readonly Guid UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly DateOnly Jan31 = new(2026, 1, 31);
    private static readonly DateOnly Feb28 = new(2026, 2, 28);
    private static readonly DateOnly Mar31 = new(2026, 3, 31);

    private readonly Mock<INetWorthSnapshotService> _snapshotService = new();

    private HistoricalBackfillJob BuildJob(params IProviderMonthlyHistorySource[] sources)
        => new(sources, _snapshotService.Object);

    [Fact]
    public async Task ExecuteForUserAsync_SingleSource_AggregatesCorrectly()
    {
        var source = new Mock<IProviderMonthlyHistorySource>();
        source.Setup(s => s.GetMonthlyBalancesAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProviderMonthlyBalance(Jan31, 1000m, "crypto"),
                new ProviderMonthlyBalance(Feb28, 1200m, "crypto"),
            ]);

        IReadOnlyList<NetWorthSnapshotData>? captured = null;
        _snapshotService.Setup(s => s.ReplaceAllSnapshotsAsync(UserId, It.IsAny<IReadOnlyList<NetWorthSnapshotData>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IReadOnlyList<NetWorthSnapshotData>, CancellationToken>((_, s, _) => captured = s)
            .Returns(Task.CompletedTask);

        await BuildJob(source.Object).ExecuteForUserAsync(UserId);

        captured.Should().HaveCount(2);
        captured![0].SnapshotDate.Should().Be(Jan31);
        captured[0].CryptoTotal.Should().Be(1000m);
        captured[0].BankingTotal.Should().Be(0m);
        captured[0].BrokerageTotal.Should().Be(0m);
    }

    [Fact]
    public async Task ExecuteForUserAsync_MultipleSourcesOverlappingMonths_CombinesTotalsCorrectly()
    {
        var cryptoSource = new Mock<IProviderMonthlyHistorySource>();
        cryptoSource.Setup(s => s.GetMonthlyBalancesAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProviderMonthlyBalance(Jan31, 500m, "crypto"),
                new ProviderMonthlyBalance(Feb28, 600m, "crypto"),
            ]);

        var brokerageSource = new Mock<IProviderMonthlyHistorySource>();
        brokerageSource.Setup(s => s.GetMonthlyBalancesAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProviderMonthlyBalance(Feb28, 2000m, "brokerage"),
                new ProviderMonthlyBalance(Mar31, 2100m, "brokerage"),
            ]);

        IReadOnlyList<NetWorthSnapshotData>? captured = null;
        _snapshotService.Setup(s => s.ReplaceAllSnapshotsAsync(UserId, It.IsAny<IReadOnlyList<NetWorthSnapshotData>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IReadOnlyList<NetWorthSnapshotData>, CancellationToken>((_, s, _) => captured = s)
            .Returns(Task.CompletedTask);

        await BuildJob(cryptoSource.Object, brokerageSource.Object).ExecuteForUserAsync(UserId);

        captured.Should().HaveCount(3);

        var list = captured!.ToList();
        var jan = list.Single(s => s.SnapshotDate == Jan31);
        jan.CryptoTotal.Should().Be(500m);
        jan.BrokerageTotal.Should().Be(0m);

        var feb = list.Single(s => s.SnapshotDate == Feb28);
        feb.CryptoTotal.Should().Be(600m);
        feb.BrokerageTotal.Should().Be(2000m);

        var mar = list.Single(s => s.SnapshotDate == Mar31);
        mar.CryptoTotal.Should().Be(0m);
        mar.BrokerageTotal.Should().Be(2100m);
    }

    [Fact]
    public async Task ExecuteForUserAsync_AllSourcesReturnEmpty_CallsReplaceWithEmptyList()
    {
        var source = new Mock<IProviderMonthlyHistorySource>();
        source.Setup(s => s.GetMonthlyBalancesAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        IReadOnlyList<NetWorthSnapshotData>? captured = null;
        _snapshotService.Setup(s => s.ReplaceAllSnapshotsAsync(UserId, It.IsAny<IReadOnlyList<NetWorthSnapshotData>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IReadOnlyList<NetWorthSnapshotData>, CancellationToken>((_, s, _) => captured = s)
            .Returns(Task.CompletedTask);

        await BuildJob(source.Object).ExecuteForUserAsync(UserId);

        captured.Should().BeEmpty();
        _snapshotService.Verify(s => s.ReplaceAllSnapshotsAsync(UserId, It.IsAny<IReadOnlyList<NetWorthSnapshotData>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteForUserAsync_MultipleProvidersSameMonth_SumsBankingBrokerageCryptoSeparately()
    {
        var bankingSource = new Mock<IProviderMonthlyHistorySource>();
        bankingSource.Setup(s => s.GetMonthlyBalancesAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProviderMonthlyBalance(Jan31, 3000m, "banking")]);

        var cryptoSource = new Mock<IProviderMonthlyHistorySource>();
        cryptoSource.Setup(s => s.GetMonthlyBalancesAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProviderMonthlyBalance(Jan31, 1500m, "crypto")]);

        IReadOnlyList<NetWorthSnapshotData>? captured = null;
        _snapshotService.Setup(s => s.ReplaceAllSnapshotsAsync(UserId, It.IsAny<IReadOnlyList<NetWorthSnapshotData>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IReadOnlyList<NetWorthSnapshotData>, CancellationToken>((_, s, _) => captured = s)
            .Returns(Task.CompletedTask);

        await BuildJob(bankingSource.Object, cryptoSource.Object).ExecuteForUserAsync(UserId);

        captured.Should().HaveCount(1);
        captured![0].BankingTotal.Should().Be(3000m);
        captured[0].CryptoTotal.Should().Be(1500m);
        captured[0].BrokerageTotal.Should().Be(0m);
    }
}
