namespace FinanceSentry.Tests.Unit.Wealth;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Wealth.Application.Services;
using FinanceSentry.Modules.Wealth.Domain;
using FinanceSentry.Modules.Wealth.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

public class NetWorthSnapshotServiceReplaceTests
{
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly NetWorthSnapshotData Jan = new(new DateOnly(2026, 1, 31), 1000m, 500m, 250m);
    private static readonly NetWorthSnapshotData Feb = new(new DateOnly(2026, 2, 28), 1100m, 600m, 300m);

    private readonly Mock<INetWorthSnapshotRepository> _repo = new();
    private readonly NetWorthSnapshotService _sut;

    public NetWorthSnapshotServiceReplaceTests()
    {
        _sut = new NetWorthSnapshotService(_repo.Object);
    }

    [Fact]
    public async Task ReplaceAllSnapshotsAsync_DeletesBeforeInserting()
    {
        var callOrder = new List<string>();
        _repo.Setup(r => r.DeleteAllByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("delete"))
            .Returns(Task.CompletedTask);
        _repo.Setup(r => r.PersistAsync(It.IsAny<NetWorthSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("persist"))
            .Returns(Task.CompletedTask);

        await _sut.ReplaceAllSnapshotsAsync(UserId, [Jan, Feb], CancellationToken.None);

        callOrder[0].Should().Be("delete");
        callOrder.Skip(1).Should().AllBe("persist");
    }

    [Fact]
    public async Task ReplaceAllSnapshotsAsync_InsertsAllSnapshots()
    {
        var persisted = new List<NetWorthSnapshot>();
        _repo.Setup(r => r.DeleteAllByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repo.Setup(r => r.PersistAsync(It.IsAny<NetWorthSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<NetWorthSnapshot, CancellationToken>((s, _) => persisted.Add(s))
            .Returns(Task.CompletedTask);

        await _sut.ReplaceAllSnapshotsAsync(UserId, [Jan, Feb], CancellationToken.None);

        persisted.Should().HaveCount(2);
        persisted[0].SnapshotDate.Should().Be(Jan.SnapshotDate);
        persisted[0].TotalNetWorth.Should().Be(Jan.BankingTotal + Jan.BrokerageTotal + Jan.CryptoTotal);
        persisted[1].SnapshotDate.Should().Be(Feb.SnapshotDate);
    }

    [Fact]
    public async Task ReplaceAllSnapshotsAsync_EmptyList_DeletesOnlyNoInserts()
    {
        _repo.Setup(r => r.DeleteAllByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ReplaceAllSnapshotsAsync(UserId, [], CancellationToken.None);

        _repo.Verify(r => r.DeleteAllByUserIdAsync(UserId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.PersistAsync(It.IsAny<NetWorthSnapshot>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
