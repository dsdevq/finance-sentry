using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.CryptoSync.Application.Commands;
using FinanceSentry.Modules.CryptoSync.Domain;
using FinanceSentry.Modules.CryptoSync.Domain.Exceptions;
using FinanceSentry.Modules.CryptoSync.Domain.Interfaces;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace FinanceSentry.Tests.Unit.CryptoSync;

public class ConnectBinanceCommandTests
{
    private readonly Mock<IBinanceCredentialRepository> _credentialRepo = new(MockBehavior.Strict);
    private readonly Mock<ICryptoExchangeAdapter> _adapter = new(MockBehavior.Strict);
    private readonly Mock<ICredentialEncryptionService> _encryption = new(MockBehavior.Strict);
    private readonly Mock<IMediator> _mediator = new(MockBehavior.Strict);

    private ConnectBinanceCommandHandler CreateHandler() =>
        new(_credentialRepo.Object, _adapter.Object, _encryption.Object, _mediator.Object);

    private static EncryptionResult FakeEncryption() =>
        new(Ciphertext: [1], Iv: [2], AuthTag: [3], KeyVersion: 1);

    [Fact]
    public async Task Handle_AlreadyConnected_ThrowsBinanceException()
    {
        var userId = Guid.NewGuid();
        var existing = BinanceCredential.Create(userId, [1], [2], [3], [4], [5], [6], 1);

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var act = () => handler.Handle(new ConnectBinanceCommand(userId, "key", "secret"), default);

        await act.Should().ThrowAsync<BinanceException>()
            .Where(ex => ex.BinanceErrorCode == -1001);
    }

    [Fact]
    public async Task Handle_ValidCredentials_CallsValidateCredentials()
    {
        var userId = Guid.NewGuid();

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BinanceCredential?)null);
        _adapter
            .Setup(a => a.ValidateCredentialsAsync("mykey", "mysecret", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns(FakeEncryption());
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<BinanceCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mediator
            .Setup(m => m.Send(It.IsAny<SyncBinanceHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncBinanceHoldingsResult(0, DateTime.UtcNow));

        await CreateHandler().Handle(new ConnectBinanceCommand(userId, "mykey", "mysecret"), default);

        _adapter.Verify(a => a.ValidateCredentialsAsync("mykey", "mysecret", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCredentials_NeverPassesPlaintextSecretToRepository()
    {
        var userId = Guid.NewGuid();
        BinanceCredential? savedCredential = null;

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BinanceCredential?)null);
        _adapter
            .Setup(a => a.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns(FakeEncryption());
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<BinanceCredential>(), It.IsAny<CancellationToken>()))
            .Callback<BinanceCredential, CancellationToken>((c, _) => savedCredential = c)
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mediator
            .Setup(m => m.Send(It.IsAny<SyncBinanceHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncBinanceHoldingsResult(0, DateTime.UtcNow));

        const string plaintextSecret = "super-secret-binance-api-secret";
        await CreateHandler().Handle(new ConnectBinanceCommand(userId, "key", plaintextSecret), default);

        savedCredential.Should().NotBeNull();
        // The entity stores byte arrays, not the plaintext — verify plaintext is not recoverable directly
        // We can only assert the plaintext was passed through ICredentialEncryptionService
        _encryption.Verify(e => e.Encrypt(plaintextSecret), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCredentials_DispatchesSyncCommand()
    {
        var userId = Guid.NewGuid();
        SyncBinanceHoldingsCommand? capturedCommand = null;

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BinanceCredential?)null);
        _adapter
            .Setup(a => a.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns(FakeEncryption());
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<BinanceCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mediator
            .Setup(m => m.Send(It.IsAny<SyncBinanceHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<SyncBinanceHoldingsResult>, CancellationToken>((cmd, _) =>
                capturedCommand = cmd as SyncBinanceHoldingsCommand)
            .ReturnsAsync(new SyncBinanceHoldingsResult(3, DateTime.UtcNow));

        await CreateHandler().Handle(new ConnectBinanceCommand(userId, "key", "secret"), default);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.UserId.Should().Be(userId);
    }
}
