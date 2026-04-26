using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinanceSentry.Tests.Unit.BrokerageSync;

public class ConnectIBKRCommandTests
{
    private readonly Mock<IIBKRCredentialRepository> _credentialRepo = new(MockBehavior.Strict);
    private readonly Mock<IBrokerAdapter> _adapter = new(MockBehavior.Strict);
    private readonly Mock<ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult>> _syncHandler = new(MockBehavior.Strict);

    private ConnectIBKRCommandHandler CreateHandler() =>
        new(_credentialRepo.Object, _adapter.Object, _syncHandler.Object);

    [Fact]
    public async Task Handle_AlreadyConnected_ThrowsBrokerAlreadyConnectedException()
    {
        var userId = Guid.NewGuid();
        var existing = new IBKRCredential(userId, "U1234567");

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = () => CreateHandler().Handle(new ConnectIBKRCommand(userId), default);

        await act.Should().ThrowAsync<BrokerAlreadyConnectedException>();
    }

    [Fact]
    public async Task Handle_GatewayAuthenticated_VerifiesSessionAndDiscoversAccount()
    {
        var userId = Guid.NewGuid();

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);
        _adapter
            .Setup(a => a.EnsureSessionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _adapter
            .Setup(a => a.GetAccountIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("U1234567");
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _syncHandler
            .Setup(h => h.Handle(It.IsAny<SyncIBKRHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncIBKRHoldingsResult(0, DateTime.UtcNow));

        var result = await CreateHandler().Handle(new ConnectIBKRCommand(userId), default);

        result.AccountId.Should().Be("U1234567");
        _adapter.Verify(a => a.EnsureSessionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_StoredCredential_HoldsOnlyAccountIdAndUserId()
    {
        var userId = Guid.NewGuid();
        IBKRCredential? savedCredential = null;

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);
        _adapter
            .Setup(a => a.EnsureSessionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _adapter
            .Setup(a => a.GetAccountIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("U1234567");
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Callback<IBKRCredential, CancellationToken>((c, _) => savedCredential = c)
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _syncHandler
            .Setup(h => h.Handle(It.IsAny<SyncIBKRHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncIBKRHoldingsResult(0, DateTime.UtcNow));

        await CreateHandler().Handle(new ConnectIBKRCommand(userId), default);

        savedCredential.Should().NotBeNull();
        savedCredential!.UserId.Should().Be(userId);
        savedCredential.AccountId.Should().Be("U1234567");
        savedCredential.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OnSuccess_DispatchesSyncCommand()
    {
        var userId = Guid.NewGuid();

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);
        _adapter
            .Setup(a => a.EnsureSessionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _adapter
            .Setup(a => a.GetAccountIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("U1234567");
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _syncHandler
            .Setup(h => h.Handle(It.IsAny<SyncIBKRHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncIBKRHoldingsResult(0, DateTime.UtcNow));

        await CreateHandler().Handle(new ConnectIBKRCommand(userId), default);

        _syncHandler.Verify(
            h => h.Handle(
                It.Is<SyncIBKRHoldingsCommand>(c => c.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
