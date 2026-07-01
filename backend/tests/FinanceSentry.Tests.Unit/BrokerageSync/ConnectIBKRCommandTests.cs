using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinanceSentry.Tests.Unit.BrokerageSync;

public class ConnectIBKRCommandTests
{
    private readonly Mock<IIBKRCredentialRepository> _credentialRepo = new(MockBehavior.Strict);
    private readonly Mock<ICredentialEncryptionService> _encryption = new(MockBehavior.Strict);

    private ConnectIBKRCommandHandler CreateHandler() =>
        new(_credentialRepo.Object, _encryption.Object);

    private static IBKRCredential ExistingCredential(Guid userId) =>
        new(userId, [1], [2], [3], [4], [5], [6], keyVersion: 1);

    private void SetupHappyPath(Guid userId, IBKRCredential? savedInto = null)
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
    }

    [Fact]
    public async Task Handle_AlreadyConnected_ThrowsBrokerAlreadyConnectedException()
    {
        var userId = Guid.NewGuid();

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingCredential(userId));

        var act = () => CreateHandler().Handle(new ConnectIBKRCommand(userId, "user", "pass"), default);

        await act.Should().ThrowAsync<BrokerAlreadyConnectedException>();
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

        await CreateHandler().Handle(new ConnectIBKRCommand(userId, "user", "pass"), default);

        saved.Should().NotBeNull();
        saved!.UserId.Should().Be(userId);
        saved.IsActive.Should().BeTrue();
        saved.EncryptedUsername.Should().NotBeEmpty();
        saved.EncryptedPassword.Should().NotBeEmpty();
        saved.KeyVersion.Should().Be(1);
    }

    [Fact]
    public async Task Handle_AccountIdDiscoveryDeferredToStage2()
    {
        var userId = Guid.NewGuid();
        SetupHappyPath(userId);

        var result = await CreateHandler().Handle(new ConnectIBKRCommand(userId, "user", "pass"), default);

        result.AccountId.Should().BeEmpty();
        result.HoldingsCount.Should().Be(0);
    }
}
