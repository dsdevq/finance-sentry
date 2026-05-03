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
    }
}
