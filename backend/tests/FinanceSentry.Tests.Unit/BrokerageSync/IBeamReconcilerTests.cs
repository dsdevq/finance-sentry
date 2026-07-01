using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FinanceSentry.Tests.Unit.BrokerageSync;

public class IBeamReconcilerTests
{
    private readonly Mock<IIBKRCredentialRepository> _repo = new(MockBehavior.Strict);
    private readonly Mock<IIBeamContainerManager> _containerManager = new(MockBehavior.Strict);
    private readonly Mock<ICredentialEncryptionService> _encryption = new(MockBehavior.Strict);

    private IBeamReconciler CreateSut() =>
        new(_repo.Object, _containerManager.Object, _encryption.Object, NullLogger<IBeamReconciler>.Instance);

    private static IBKRCredential MakeCredential() =>
        new(Guid.NewGuid(), [1], [2], [3], [4], [5], [6], keyVersion: 1);

    [Fact]
    public async Task ReconcileAllAsync_SkipsRunningContainers()
    {
        var credential = MakeCredential();
        _repo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync([credential]);
        _containerManager
            .Setup(m => m.IsRunningAsync(credential.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateSut().ReconcileAllAsync(default);

        _containerManager.Verify(
            m => m.SpawnAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _encryption.Verify(e => e.Decrypt(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ReconcileAllAsync_DecryptsAndSpawns_WhenContainerNotRunning()
    {
        var credential = MakeCredential();
        _repo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync([credential]);
        _containerManager
            .Setup(m => m.IsRunningAsync(credential.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _encryption
            .Setup(e => e.Decrypt(credential.EncryptedUsername, credential.UsernameIv, credential.UsernameAuthTag, 1))
            .Returns("user1");
        _encryption
            .Setup(e => e.Decrypt(credential.EncryptedPassword, credential.PasswordIv, credential.PasswordAuthTag, 1))
            .Returns("pw1");
        _containerManager
            .Setup(m => m.SpawnAsync(credential.Id, "user1", "pw1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateSut().ReconcileAllAsync(default);

        _containerManager.Verify(
            m => m.SpawnAsync(credential.Id, "user1", "pw1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReconcileAllAsync_ContinuesAfterFailure_OnFollowingCredentials()
    {
        var failing = MakeCredential();
        var succeeding = MakeCredential();
        _repo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([failing, succeeding]);

        _containerManager
            .Setup(m => m.IsRunningAsync(failing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _encryption
            .Setup(e => e.Decrypt(failing.EncryptedUsername, failing.UsernameIv, failing.UsernameAuthTag, 1))
            .Throws(new InvalidOperationException("decrypt fail"));

        _containerManager
            .Setup(m => m.IsRunningAsync(succeeding.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _encryption
            .Setup(e => e.Decrypt(succeeding.EncryptedUsername, succeeding.UsernameIv, succeeding.UsernameAuthTag, 1))
            .Returns("user2");
        _encryption
            .Setup(e => e.Decrypt(succeeding.EncryptedPassword, succeeding.PasswordIv, succeeding.PasswordAuthTag, 1))
            .Returns("pw2");
        _containerManager
            .Setup(m => m.SpawnAsync(succeeding.Id, "user2", "pw2", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateSut().ReconcileAllAsync(default);

        _containerManager.Verify(
            m => m.SpawnAsync(succeeding.Id, "user2", "pw2", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
