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

public class IBKRConnectRunnerTests
{
    private readonly Mock<IIBKRCredentialRepository> _credentialRepo = new(MockBehavior.Strict);
    private readonly Mock<ICredentialEncryptionService> _encryption = new(MockBehavior.Strict);
    private readonly Mock<IIBeamContainerManager> _containerManager = new(MockBehavior.Strict);
    private readonly Mock<ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult>> _syncHandler = new(MockBehavior.Strict);
    private readonly IBKRConnectSessionStore _sessionStore = new();

    private IBKRConnectRunner CreateRunner() => new(
        _sessionStore,
        _credentialRepo.Object,
        _encryption.Object,
        _containerManager.Object,
        _syncHandler.Object,
        NullLogger<IBKRConnectRunner>.Instance);

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
    public async Task Run_HappyPath_TransitionsThroughSpawningAwaitingAuthSyncingCompleted()
    {
        var userId = Guid.NewGuid();
        SetupHappyPath(holdings: 7);
        var (sessionId, token) = _sessionStore.Create(userId);

        await CreateRunner().RunAsync(sessionId, userId, "u", "p", token);

        var snap = _sessionStore.Get(sessionId, userId)!;
        snap.Status.Should().Be(IBKRConnectStatus.Completed);
        snap.Result!.HoldingsCount.Should().Be(7);
        snap.ErrorCode.Should().BeNull();
    }

    [Fact]
    public async Task Run_ExistingActiveCredential_MarksFailedALREADY_CONNECTED_WithoutTouchingContainer()
    {
        var userId = Guid.NewGuid();
        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Existing(userId));

        var (sessionId, token) = _sessionStore.Create(userId);
        await CreateRunner().RunAsync(sessionId, userId, "u", "p", token);

        var snap = _sessionStore.Get(sessionId, userId)!;
        snap.Status.Should().Be(IBKRConnectStatus.Failed);
        snap.ErrorCode.Should().Be("IBKR_DUPLICATE");
        _containerManager.Verify(
            m => m.SpawnAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_ExistingInactiveCredential_RotatesInPlaceInsteadOfAdding()
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

        var (sessionId, token) = _sessionStore.Create(userId);
        await CreateRunner().RunAsync(sessionId, userId, "u", "p", token);

        _credentialRepo.Verify(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()), Times.Never);
        _credentialRepo.Verify(r => r.Update(It.Is<IBKRCredential>(c => c.IsActive)), Times.Once);
        _sessionStore.Get(sessionId, userId)!.Status.Should().Be(IBKRConnectStatus.Completed);
    }

    [Fact]
    public async Task Run_AuthTimeout_MarksFailedINVALID_CREDENTIALS_AndRollsBack()
    {
        var userId = Guid.NewGuid();
        SetupHappyPath(authResult: false);
        var (sessionId, token) = _sessionStore.Create(userId);

        await CreateRunner().RunAsync(sessionId, userId, "u", "p", token);

        var snap = _sessionStore.Get(sessionId, userId)!;
        snap.Status.Should().Be(IBKRConnectStatus.Failed);
        snap.ErrorCode.Should().Be("IBKR_INVALID_CREDENTIALS");
        _containerManager.Verify(m => m.StopAndRemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _credentialRepo.Verify(r => r.Update(It.Is<IBKRCredential>(c => !c.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Run_SpawnHttpFailure_MarksFailedGATEWAY_UNAVAILABLE_AndRollsBack()
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

        var (sessionId, token) = _sessionStore.Create(userId);
        await CreateRunner().RunAsync(sessionId, userId, "u", "p", token);

        var snap = _sessionStore.Get(sessionId, userId)!;
        snap.Status.Should().Be(IBKRConnectStatus.Failed);
        snap.ErrorCode.Should().Be("IBKR_GATEWAY_UNAVAILABLE");
        _credentialRepo.Verify(r => r.Update(It.Is<IBKRCredential>(c => !c.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Run_Cancellation_MarksCancelled_AndRollsBack()
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

        var (sessionId, token) = _sessionStore.Create(userId);

        _containerManager
            .Setup(m => m.SpawnAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => _sessionStore.Cancel(sessionId, userId))
            .Returns(Task.CompletedTask);
        _containerManager
            .Setup(m => m.WaitForAuthAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        _containerManager
            .Setup(m => m.StopAndRemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateRunner().RunAsync(sessionId, userId, "u", "p", token);

        var snap = _sessionStore.Get(sessionId, userId)!;
        snap.Status.Should().Be(IBKRConnectStatus.Cancelled);
        _containerManager.Verify(m => m.StopAndRemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _credentialRepo.Verify(r => r.Update(It.Is<IBKRCredential>(c => !c.IsActive)), Times.Once);
    }

    [Fact]
    public void Store_GetForDifferentUser_ReturnsNull()
    {
        var owner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var (sessionId, _) = _sessionStore.Create(owner);

        _sessionStore.Get(sessionId, attacker).Should().BeNull();
    }

    [Fact]
    public void Store_CancelForDifferentUser_ReturnsFalse()
    {
        var owner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var (sessionId, _) = _sessionStore.Create(owner);

        _sessionStore.Cancel(sessionId, attacker).Should().BeFalse();
    }

    [Fact]
    public void Store_FindActiveByUser_ReturnsInFlightSession()
    {
        var userId = Guid.NewGuid();
        var (sessionId, _) = _sessionStore.Create(userId);

        _sessionStore.FindActiveByUser(userId).Should().Be(sessionId);
    }

    [Fact]
    public void Store_FindActiveByUser_IgnoresTerminalSessions()
    {
        var userId = Guid.NewGuid();
        var (sessionId, _) = _sessionStore.Create(userId);
        _sessionStore.MarkFailed(sessionId, "IBKR_INVALID_CREDENTIALS", "…");

        _sessionStore.FindActiveByUser(userId).Should().BeNull();
    }

    [Fact]
    public void Store_FindActiveByUser_IsolatesUsers()
    {
        var owner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        _sessionStore.Create(owner);

        _sessionStore.FindActiveByUser(attacker).Should().BeNull();
    }
}
