namespace FinanceSentry.Tests.Unit.Wealth;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Wealth.Application.Services;
using FinanceSentry.Modules.Wealth.Domain;
using FinanceSentry.Modules.Wealth.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

public class NetWorthSnapshotServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateOnly SnapshotDate = new(2026, 5, 31);

    private static NetWorthSnapshotData MakeData(DateOnly? date = null)
        => new(date ?? SnapshotDate, BankingTotal: 1000m, BrokerageTotal: 500m, CryptoTotal: 250m);

    [Fact]
    public async Task PersistSnapshotAsync_WhenSnapshotAlreadyExists_DoesNotInsert()
    {
        var repositoryMock = new Mock<INetWorthSnapshotRepository>();
        repositoryMock
            .Setup(r => r.ExistsAsync(UserId, SnapshotDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new NetWorthSnapshotService(repositoryMock.Object);

        await sut.PersistSnapshotAsync(UserId, MakeData(), CancellationToken.None);

        repositoryMock.Verify(
            r => r.PersistAsync(It.IsAny<NetWorthSnapshot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PersistSnapshotAsync_WhenNoExistingSnapshot_InsertsWithCorrectTotals()
    {
        NetWorthSnapshot? captured = null;
        var repositoryMock = new Mock<INetWorthSnapshotRepository>();
        repositoryMock
            .Setup(r => r.ExistsAsync(UserId, SnapshotDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repositoryMock
            .Setup(r => r.PersistAsync(It.IsAny<NetWorthSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<NetWorthSnapshot, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);

        var sut = new NetWorthSnapshotService(repositoryMock.Object);

        await sut.PersistSnapshotAsync(UserId, MakeData(), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(UserId);
        captured.SnapshotDate.Should().Be(SnapshotDate);
        captured.BankingTotal.Should().Be(1000m);
        captured.BrokerageTotal.Should().Be(500m);
        captured.CryptoTotal.Should().Be(250m);
        captured.TotalNetWorth.Should().Be(1750m);
        captured.Currency.Should().Be("USD");
        captured.StaleSleeves.Should().BeNull();
    }

    private static (Mock<INetWorthSnapshotRepository> repo, Func<NetWorthSnapshot?> captured) SetupInsertCapture(
        NetWorthSnapshot? previous)
    {
        NetWorthSnapshot? captured = null;
        var repositoryMock = new Mock<INetWorthSnapshotRepository>();
        repositoryMock
            .Setup(r => r.ExistsAsync(UserId, SnapshotDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repositoryMock
            .Setup(r => r.GetLatestByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previous);
        repositoryMock
            .Setup(r => r.PersistAsync(It.IsAny<NetWorthSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<NetWorthSnapshot, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);
        return (repositoryMock, () => captured);
    }

    [Fact]
    public async Task PersistSnapshotAsync_WhenBankingStale_CarriesForwardPreviousValueAndFlags()
    {
        // Regression for the misleading net-worth drop: a lapsed bank connection (Revolut/AIB)
        // reports a reduced-but-nonzero balance. It must carry forward, not record a phantom drop.
        var previous = new NetWorthSnapshot { BankingTotal = 5000m, BrokerageTotal = 9912m, CryptoTotal = 240m };
        var (repo, captured) = SetupInsertCapture(previous);
        var data = new NetWorthSnapshotData(
            SnapshotDate, BankingTotal: 1000m, BrokerageTotal: 9912m, CryptoTotal: 240m,
            BankingFresh: false, BrokerageFresh: true, CryptoFresh: true);

        await new NetWorthSnapshotService(repo.Object).PersistSnapshotAsync(UserId, data, CancellationToken.None);

        captured()!.BankingTotal.Should().Be(5000m); // carried forward, not the stale $1000
        captured()!.StaleSleeves.Should().Be("banking");
    }

    [Fact]
    public async Task PersistSnapshotAsync_WhenBrokerageStale_CarriesForwardPreviousValueAndFlags()
    {
        var previous = new NetWorthSnapshot { BankingTotal = 900m, BrokerageTotal = 9912m, CryptoTotal = 240m };
        var (repo, captured) = SetupInsertCapture(previous);
        var data = new NetWorthSnapshotData(
            SnapshotDate, BankingTotal: 1000m, BrokerageTotal: 9912m, CryptoTotal: 250m,
            BrokerageFresh: false, CryptoFresh: true);

        await new NetWorthSnapshotService(repo.Object).PersistSnapshotAsync(UserId, data, CancellationToken.None);

        captured()!.BrokerageTotal.Should().Be(9912m); // carried forward, not re-counted as fresh
        captured()!.BankingTotal.Should().Be(1000m);
        captured()!.CryptoTotal.Should().Be(250m);
        captured()!.StaleSleeves.Should().Be("brokerage");
    }

    [Fact]
    public async Task PersistSnapshotAsync_WhenSleeveDropsToZeroButPreviouslyHeldValue_TreatsAsFailedSyncAndCarriesForward()
    {
        var previous = new NetWorthSnapshot { BankingTotal = 1000m, BrokerageTotal = 5000m, CryptoTotal = 300m };
        var (repo, captured) = SetupInsertCapture(previous);
        // Brokerage sync failed and returned $0 while reporting "fresh".
        var data = new NetWorthSnapshotData(
            SnapshotDate, BankingTotal: 1000m, BrokerageTotal: 0m, CryptoTotal: 300m,
            BrokerageFresh: true, CryptoFresh: true);

        await new NetWorthSnapshotService(repo.Object).PersistSnapshotAsync(UserId, data, CancellationToken.None);

        captured()!.BrokerageTotal.Should().Be(5000m);
        captured()!.TotalNetWorth.Should().Be(6300m);
        captured()!.StaleSleeves.Should().Be("brokerage");
    }

    [Fact]
    public async Task PersistSnapshotAsync_WhenStaleButNoHistory_UsesBestEffortValueWithoutFlag()
    {
        var (repo, captured) = SetupInsertCapture(previous: null);
        var data = new NetWorthSnapshotData(
            SnapshotDate, BankingTotal: 1000m, BrokerageTotal: 500m, CryptoTotal: 250m,
            BrokerageFresh: false, CryptoFresh: false);

        await new NetWorthSnapshotService(repo.Object).PersistSnapshotAsync(UserId, data, CancellationToken.None);

        captured()!.BrokerageTotal.Should().Be(500m);
        captured()!.CryptoTotal.Should().Be(250m);
        captured()!.StaleSleeves.Should().BeNull();
    }
}
