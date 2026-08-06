using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Application.Connect;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FinanceSentry.Tests.Unit.BrokerageSync;

public class IBKRConnectorTests
{
    private readonly Mock<IIBKRCredentialRepository> _credentialRepo = new(MockBehavior.Strict);
    private readonly Mock<ICredentialEncryptionService> _encryption = new(MockBehavior.Strict);

    private IBKRConnector CreateConnector() => new(
        _credentialRepo.Object,
        _encryption.Object,
        NullLogger<IBKRConnector>.Instance);

    private static ConnectIBKRArtifacts Artifacts() =>
        new("FINSENTRY", "access-token", "token-secret", "sig-pem", "enc-pem", "dh-pem");

    private static IBKRCredential Existing(Guid userId) => new(
        userId, "FINSENTRY", "access-token", "dh-pem",
        [1], [2], [3], [4], [5], [6], [7], [8], [9], keyVersion: 1);

    private void SetupEncryption() =>
        _encryption
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns((string s) => new EncryptionResult([(byte)s.Length], [1], [2], 1));

    [Fact]
    public async Task Connect_NewUser_EncryptsSecrets_Persists_AndReturnsPendingResult()
    {
        var userId = Guid.NewGuid();
        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);
        SetupEncryption();
        _credentialRepo
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateConnector().ConnectAsync(userId, Artifacts(), CancellationToken.None);

        result.HoldingsCount.Should().Be(0);
        // The three secret artifacts (token secret + two RSA keys) are encrypted.
        _encryption.Verify(e => e.Encrypt(It.IsAny<string>()), Times.Exactly(3));
        _credentialRepo.Verify(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Connect_ExistingActiveCredential_Throws_DUPLICATE()
    {
        var userId = Guid.NewGuid();
        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Existing(userId));

        var act = () => CreateConnector().ConnectAsync(userId, Artifacts(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<IBKRConnectException>();
        ex.Which.ErrorCode.Should().Be("IBKR_DUPLICATE");
        _credentialRepo.Verify(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()), Times.Never);
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
        SetupEncryption();
        _credentialRepo.Setup(r => r.Update(It.IsAny<IBKRCredential>()));
        _credentialRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateConnector().ConnectAsync(userId, Artifacts(), CancellationToken.None);

        _credentialRepo.Verify(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()), Times.Never);
        _credentialRepo.Verify(r => r.Update(It.Is<IBKRCredential>(c => c.IsActive)), Times.Once);
    }
}
