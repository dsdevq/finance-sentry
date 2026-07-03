using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Application.Connect;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FinanceSentry.Tests.Unit.BrokerageSync;

public class IBKRConnectorTests
{
    private readonly Mock<IIBKRCredentialRepository> _credentialRepo = new(MockBehavior.Strict);
    private readonly Mock<ICredentialEncryptionService> _encryption = new(MockBehavior.Strict);
    private readonly Mock<IIBeamContainerManager> _containerManager = new(MockBehavior.Strict);
    private readonly Mock<ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult>> _syncHandler = new(MockBehavior.Strict);

    private IBKRConnector CreateConnector() => new(
        _credentialRepo.Object,
        _encryption.Object,
        _containerManager.Object,
        _syncHandler.Object,
        NullLogger<IBKRConnector>.Instance);

    private static IBKRCredential Existing(Guid userId) =>
        new(userId, [1], [2], [3], [4], [5], [6], keyVersion: 1);

    private void SetupHappyPath(bool authResult = true, int holdings = 3)
    {
        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);
        _encryption
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns((string s) => new EncryptionResult([(byte)s.Length], [1], [2], 1));
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.Update(It.IsAny<IBKRCredential>()));
        _containerManager
            .Setup(m => m.SpawnAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _containerManager
            .Setup(m => m.WaitForAuthAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);
        _containerManager
            .Setup(m => m.StopAndRemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _syncHandler
            .Setup(h => h.Handle(It.IsAny<SyncIBKRHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncIBKRHoldingsResult(holdings, DateTime.UtcNow));
    }

    [Fact]
    public async Task Connect_HappyPath_ReturnsResult_WithHoldingsCount()
    {
        var userId = Guid.NewGuid();
        SetupHappyPath(holdings: 7);

        var result = await CreateConnector().ConnectAsync(userId, "u", "p", CancellationToken.None);

        result.HoldingsCount.Should().Be(7);
    }

    [Fact]
    public async Task Connect_ExistingActiveCredential_Throws_DUPLICATE_WithoutTouchingContainer()
    {
        var userId = Guid.NewGuid();
        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Existing(userId));

        var act = () => CreateConnector().ConnectAsync(userId, "u", "p", CancellationToken.None);

        var ex = await act.Should().ThrowAsync<IBKRConnectException>();
        ex.Which.ErrorCode.Should().Be("IBKR_DUPLICATE");
        _containerManager.Verify(
            m => m.SpawnAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Connect_ExistingInactiveCredential_RotatesInPlaceInsteadOfAdding()
    {
        var userId = Guid.NewGuid();
        var stale = Existing(userId);
        stale.Deactivate();

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stale);
        _encryption
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns((string s) => new EncryptionResult([(byte)s.Length], [1], [2], 1));
        _credentialRepo
            .Setup(r => r.Update(It.IsAny<IBKRCredential>()));
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _containerManager
            .Setup(m => m.SpawnAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _containerManager
            .Setup(m => m.WaitForAuthAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _syncHandler
            .Setup(h => h.Handle(It.IsAny<SyncIBKRHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncIBKRHoldingsResult(0, DateTime.UtcNow));

        await CreateConnector().ConnectAsync(userId, "u", "p", CancellationToken.None);

        _credentialRepo.Verify(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()), Times.Never);
        _credentialRepo.Verify(r => r.Update(It.Is<IBKRCredential>(c => c.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Connect_AuthTimeout_Throws_INVALID_CREDENTIALS_AndRollsBack()
    {
        var userId = Guid.NewGuid();
        SetupHappyPath(authResult: false);

        var act = () => CreateConnector().ConnectAsync(userId, "u", "p", CancellationToken.None);
        var ex = await act.Should().ThrowAsync<IBKRConnectException>();
        ex.Which.ErrorCode.Should().Be("IBKR_INVALID_CREDENTIALS");

        _containerManager.Verify(m => m.StopAndRemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _credentialRepo.Verify(r => r.Update(It.Is<IBKRCredential>(c => !c.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Connect_SpawnHttpFailure_Throws_GATEWAY_UNAVAILABLE_AndRollsBack()
    {
        var userId = Guid.NewGuid();
        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);
        _encryption
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns((string s) => new EncryptionResult([(byte)s.Length], [1], [2], 1));
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.Update(It.IsAny<IBKRCredential>()));
        _containerManager
            .Setup(m => m.SpawnAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Docker daemon unreachable"));
        _containerManager
            .Setup(m => m.StopAndRemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var act = () => CreateConnector().ConnectAsync(userId, "u", "p", CancellationToken.None);
        var ex = await act.Should().ThrowAsync<IBKRConnectException>();
        ex.Which.ErrorCode.Should().Be("IBKR_GATEWAY_UNAVAILABLE");

        _credentialRepo.Verify(r => r.Update(It.Is<IBKRCredential>(c => !c.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Connect_ClientDisconnect_PropagatesCancellation_AndRollsBack()
    {
        var userId = Guid.NewGuid();
        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);
        _encryption
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns((string s) => new EncryptionResult([(byte)s.Length], [1], [2], 1));
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.Update(It.IsAny<IBKRCredential>()));
        _containerManager
            .Setup(m => m.SpawnAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _containerManager
            .Setup(m => m.WaitForAuthAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        _containerManager
            .Setup(m => m.StopAndRemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var act = () => CreateConnector().ConnectAsync(userId, "u", "p", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _containerManager.Verify(m => m.StopAndRemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _credentialRepo.Verify(r => r.Update(It.Is<IBKRCredential>(c => !c.IsActive)), Times.Once);
    }
}
