using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace FinanceSentry.Tests.Unit.BrokerageSync;

public class ConnectIBKRCommandTests
{
    private readonly Mock<IIBKRCredentialRepository> _credentialRepo = new(MockBehavior.Strict);
    private readonly Mock<IBrokerAdapter> _adapter = new(MockBehavior.Strict);
    private readonly Mock<ICredentialEncryptionService> _encryption = new(MockBehavior.Strict);
    private readonly Mock<IMediator> _mediator = new(MockBehavior.Strict);

    private ConnectIBKRCommandHandler CreateHandler() =>
        new(_credentialRepo.Object, _adapter.Object, _encryption.Object, _mediator.Object);

    private static EncryptionResult FakeEncryption() =>
        new(Ciphertext: [1], Iv: [2], AuthTag: [3], KeyVersion: 1);

    [Fact]
    public async Task Handle_AlreadyConnected_ThrowsBrokerAlreadyConnectedException()
    {
        var userId = Guid.NewGuid();
        var existing = new IBKRCredential(userId, [1], [2], [3], [4], [5], [6], 1, "U1234567");

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = () => CreateHandler().Handle(new ConnectIBKRCommand(userId, "user", "pass"), default);

        await act.Should().ThrowAsync<BrokerAlreadyConnectedException>();
    }

    [Fact]
    public async Task Handle_ValidCredentials_CallsAuthenticateAsync()
    {
        var userId = Guid.NewGuid();

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);
        _adapter
            .Setup(a => a.AuthenticateAsync("testuser", "testpass", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _adapter
            .Setup(a => a.GetAccountIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("U1234567");
        _encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns(FakeEncryption());
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mediator
            .Setup(m => m.Send(It.IsAny<SyncIBKRHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncIBKRHoldingsResult(0, DateTime.UtcNow));

        await CreateHandler().Handle(new ConnectIBKRCommand(userId, "testuser", "testpass"), default);

        _adapter.Verify(a => a.AuthenticateAsync("testuser", "testpass", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCredentials_NeverStoresPlaintextCredentials()
    {
        var userId = Guid.NewGuid();
        IBKRCredential? savedCredential = null;

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);
        _adapter
            .Setup(a => a.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _adapter
            .Setup(a => a.GetAccountIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("U1234567");
        _encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns(FakeEncryption());
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Callback<IBKRCredential, CancellationToken>((c, _) => savedCredential = c)
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mediator
            .Setup(m => m.Send(It.IsAny<SyncIBKRHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncIBKRHoldingsResult(0, DateTime.UtcNow));

        const string plaintextPassword = "super-secret-ibkr-password";
        await CreateHandler().Handle(new ConnectIBKRCommand(userId, "testuser", plaintextPassword), default);

        savedCredential.Should().NotBeNull();
        savedCredential!.EncryptedPassword.Should().NotContain(
            System.Text.Encoding.UTF8.GetBytes(plaintextPassword),
            "plaintext password must never be stored");
    }

    [Fact]
    public async Task Handle_ValidCredentials_DispatchesSyncCommand()
    {
        var userId = Guid.NewGuid();

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);
        _adapter
            .Setup(a => a.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _adapter
            .Setup(a => a.GetAccountIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("U1234567");
        _encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns(FakeEncryption());
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mediator
            .Setup(m => m.Send(It.IsAny<SyncIBKRHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncIBKRHoldingsResult(0, DateTime.UtcNow));

        await CreateHandler().Handle(new ConnectIBKRCommand(userId, "user", "pass"), default);

        _mediator.Verify(
            m => m.Send(
                It.Is<SyncIBKRHoldingsCommand>(c => c.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
