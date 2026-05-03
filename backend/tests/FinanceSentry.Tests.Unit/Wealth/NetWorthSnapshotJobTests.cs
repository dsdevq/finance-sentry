namespace FinanceSentry.Tests.Unit.Wealth;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Wealth.Infrastructure.Jobs;
using FluentAssertions;
using Moq;
using Xunit;

public class NetWorthSnapshotJobTests
{
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static Mock<IBankingTotalsReader> BankingMock(decimal total)
    {
        var mock = new Mock<IBankingTotalsReader>();
        mock.Setup(r => r.GetActiveUserIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([UserId]);
        mock.Setup(r => r.GetTotalUsdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(total);
        return mock;
    }

    private static Mock<ICryptoHoldingsReader> CryptoMock(decimal usdValue)
    {
        var mock = new Mock<ICryptoHoldingsReader>();
        mock.Setup(r => r.GetHoldingsAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CryptoHoldingSummary("BTC", 1m, 0m, usdValue, DateTime.UtcNow, "binance")]);
        return mock;
    }

    private static Mock<IBrokerageHoldingsReader> BrokerageMock(decimal usdValue)
    {
        var mock = new Mock<IBrokerageHoldingsReader>();
        mock.Setup(r => r.GetHoldingsAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new BrokerageHoldingSummary("AAPL", "equity", 10m, usdValue, DateTime.UtcNow, "ibkr")]);
        return mock;
    }

    [Fact]
    public async Task ExecuteAsync_SumsAllAssetClassesAndPersistsSnapshot()
    {
        NetWorthSnapshotData? captured = null;
        var snapshotServiceMock = new Mock<INetWorthSnapshotService>();
        snapshotServiceMock
            .Setup(s => s.PersistSnapshotAsync(UserId, It.IsAny<NetWorthSnapshotData>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, NetWorthSnapshotData, CancellationToken>((_, data, _) => captured = data)
            .Returns(Task.CompletedTask);

        var sut = new NetWorthSnapshotJob(
            BankingMock(total: 2000m).Object,
            CryptoMock(usdValue: 500m).Object,
            BrokerageMock(usdValue: 1000m).Object,
            snapshotServiceMock.Object);

        await sut.ExecuteAsync();

        captured.Should().NotBeNull();
        captured!.BankingTotal.Should().Be(2000m);
        captured.CryptoTotal.Should().Be(500m);
        captured.BrokerageTotal.Should().Be(1000m);
    }

    [Fact]
    public async Task ExecuteForUserAsync_PersistsSnapshotForSpecificUser()
    {
        Guid? capturedUserId = null;
        var snapshotServiceMock = new Mock<INetWorthSnapshotService>();
        snapshotServiceMock
            .Setup(s => s.PersistSnapshotAsync(It.IsAny<Guid>(), It.IsAny<NetWorthSnapshotData>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, NetWorthSnapshotData, CancellationToken>((uid, _, _) => capturedUserId = uid)
            .Returns(Task.CompletedTask);

        var bankingMock = new Mock<IBankingTotalsReader>();
        bankingMock.Setup(r => r.GetTotalUsdAsync(UserId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(1500m);

        var sut = new NetWorthSnapshotJob(
            bankingMock.Object,
            CryptoMock(usdValue: 0m).Object,
            BrokerageMock(usdValue: 0m).Object,
            snapshotServiceMock.Object);

        await sut.ExecuteForUserAsync(UserId);

        capturedUserId.Should().Be(UserId);
    }
}
