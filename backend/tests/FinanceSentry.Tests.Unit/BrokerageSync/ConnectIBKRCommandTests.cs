using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FinanceSentry.Tests.Unit.BrokerageSync;

public class ConnectIBKRCommandTests
{
    private readonly Mock<IIBKRCredentialRepository> _credentialRepo = new(MockBehavior.Strict);
    private readonly Mock<ICredentialEncryptionService> _encryption = new(MockBehavior.Strict);
    private readonly Mock<IIBeamContainerManager> _containerManager = new(MockBehavior.Strict);
    private readonly Mock<ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult>> _syncHandler = new(MockBehavior.Strict);

    private ConnectIBKRCommandHandler CreateHandler() =>
        new(
            _credentialRepo.Object,
            _encryption.Object,
            _containerManager.Object,
            _syncHandler.Object,
            NullLogger<ConnectIBKRCommandHandler>.Instance);

    private static IBKRCredential ExistingCredential(Guid userId) =>
        new(userId, [1], [2], [3], [4], [5], [6], keyVersion: 1);

    private void SetupHappyPath(Guid userId, bool authResult = true)
    {
        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
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
        _containerManager
            .Setup(m => m.SpawnAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _containerManager
            .Setup(m => m.WaitForAuthAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);
        _syncHandler
            .Setup(h => h.Handle(It.IsAny<SyncIBKRHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncIBKRHoldingsResult(0, DateTime.UtcNow));
    }

    [Fact]
    public async Task Handle_ExistingActiveCredential_ThrowsBrokerAlreadyConnectedException()
    {
        var userId = Guid.NewGuid();

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingCredential(userId));

        var act = () => CreateHandler().Handle(new ConnectIBKRCommand(userId, "user", "pass"), default);

        await act.Should().ThrowAsync<BrokerAlreadyConnectedException>();
    }

    [Fact]
    public async Task Handle_ExistingInactiveCredential_RotatesInPlaceInsteadOfAdding()
    {
        var userId = Guid.NewGuid();
        var stale = ExistingCredential(userId);
        stale.Deactivate();
        var staleId = stale.Id;

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

        await CreateHandler().Handle(new ConnectIBKRCommand(userId, "u", "p"), default);

        _credentialRepo.Verify(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()), Times.Never);
        _credentialRepo.Verify(r => r.Update(It.Is<IBKRCredential>(c => c.Id == staleId && c.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Handle_EncryptsUsernameAndPasswordSeparately()
    {
        var userId = Guid.NewGuid();
        SetupHappyPath(userId);

        await CreateHandler().Handle(new ConnectIBKRCommand(userId, "ibkr-user", "ibkr-pass"), default);

        _encryption.Verify(e => e.Encrypt("ibkr-user"), Times.Once);
        _encryption.Verify(e => e.Encrypt("ibkr-pass"), Times.Once);
    }

    [Fact]
    public async Task Handle_SpawnsContainerWithPlaintextCredentials()
    {
        var userId = Guid.NewGuid();
        SetupHappyPath(userId);

        await CreateHandler().Handle(new ConnectIBKRCommand(userId, "ibkr-user", "ibkr-pass"), default);

        _containerManager.Verify(
            m => m.SpawnAsync(It.IsAny<Guid>(), "ibkr-user", "ibkr-pass", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DispatchesSyncAfterGatewayAuthenticates()
    {
        var userId = Guid.NewGuid();
        SetupHappyPath(userId);

        await CreateHandler().Handle(new ConnectIBKRCommand(userId, "user", "pass"), default);

        _syncHandler.Verify(
            h => h.Handle(It.Is<SyncIBKRHoldingsCommand>(c => c.UserId == userId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AuthTimeout_ThrowsBrokerAuthException()
    {
        var userId = Guid.NewGuid();
        SetupHappyPath(userId, authResult: false);
        SetupRollback();

        var act = () => CreateHandler().Handle(new ConnectIBKRCommand(userId, "user", "pass"), default);

        await act.Should().ThrowAsync<BrokerAuthException>();
    }

    [Fact]
    public async Task Handle_AuthTimeout_RollsBackCredentialToInactive()
    {
        var userId = Guid.NewGuid();
        IBKRCredential? saved = null;

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);
        _encryption
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns((string s) => new EncryptionResult([(byte)s.Length], [1], [2], 1));
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Callback<IBKRCredential, CancellationToken>((c, _) => saved = c)
            .Returns(Task.CompletedTask);
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
            .ReturnsAsync(false);
        _containerManager
            .Setup(m => m.StopAndRemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var act = () => CreateHandler().Handle(new ConnectIBKRCommand(userId, "user", "pass"), default);

        await act.Should().ThrowAsync<BrokerAuthException>();

        saved.Should().NotBeNull();
        saved!.IsActive.Should().BeFalse();
        _containerManager.Verify(m => m.StopAndRemoveAsync(saved.Id, It.IsAny<CancellationToken>()), Times.Once);
        _credentialRepo.Verify(r => r.Update(It.Is<IBKRCredential>(c => !c.IsActive)), Times.Once);
        _credentialRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_SpawnFailure_RollsBackCredentialToInactive()
    {
        var userId = Guid.NewGuid();
        IBKRCredential? saved = null;

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);
        _encryption
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns((string s) => new EncryptionResult([(byte)s.Length], [1], [2], 1));
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Callback<IBKRCredential, CancellationToken>((c, _) => saved = c)
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.Update(It.IsAny<IBKRCredential>()));
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _containerManager
            .Setup(m => m.SpawnAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Docker daemon unreachable"));
        _containerManager
            .Setup(m => m.StopAndRemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var act = () => CreateHandler().Handle(new ConnectIBKRCommand(userId, "user", "pass"), default);

        await act.Should().ThrowAsync<HttpRequestException>();

        saved!.IsActive.Should().BeFalse();
        _credentialRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private void SetupRollback()
    {
        _credentialRepo.Setup(r => r.Update(It.IsAny<IBKRCredential>()));
        _containerManager
            .Setup(m => m.StopAndRemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_PersistsEncryptedCredentialBoundToUser()
    {
        var userId = Guid.NewGuid();
        IBKRCredential? saved = null;

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);
        _encryption
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns((string s) => new EncryptionResult([(byte)s.Length], [1], [2], 1));
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Callback<IBKRCredential, CancellationToken>((c, _) => saved = c)
            .Returns(Task.CompletedTask);
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

        await CreateHandler().Handle(new ConnectIBKRCommand(userId, "user", "pass"), default);

        saved.Should().NotBeNull();
        saved!.UserId.Should().Be(userId);
        saved.IsActive.Should().BeTrue();
        saved.EncryptedUsername.Should().NotBeEmpty();
        saved.EncryptedPassword.Should().NotBeEmpty();
        saved.KeyVersion.Should().Be(1);
    }
}
