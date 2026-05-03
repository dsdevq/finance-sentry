using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Jobs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FinanceSentry.Tests.Unit.BrokerageSync;

public class IBKRSyncJobTests
{
    private readonly Mock<IIBKRCredentialRepository> _credentialRepo = new(MockBehavior.Loose);
    private readonly Mock<ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult>> _syncHandler = new(MockBehavior.Loose);

    private readonly Mock<FinanceSentry.Core.Interfaces.IAlertGeneratorService> _alerts = new(MockBehavior.Loose);
    private readonly Mock<FinanceSentry.Core.Interfaces.IUserAlertPreferencesReader> _userPrefs = new(MockBehavior.Loose);

    private IBKRSyncJob CreateJob() =>
        new(_credentialRepo.Object, _syncHandler.Object, _alerts.Object, _userPrefs.Object, NullLogger<IBKRSyncJob>.Instance);

    private static IBKRCredential MakeCredential(Guid userId) =>
        new(userId, "U1234567");

    [Fact]
    public async Task ExecuteAsync_IteratesAllActiveCredentials()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        _credentialRepo
            .Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeCredential(user1), MakeCredential(user2)]);

        _syncHandler
            .Setup(h => h.Handle(It.IsAny<SyncIBKRHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncIBKRHoldingsResult(0, DateTime.UtcNow));

        await CreateJob().ExecuteAsync();

        _syncHandler.Verify(
            h => h.Handle(It.IsAny<SyncIBKRHoldingsCommand>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_DispatchesSyncCommandPerCredential()
    {
        var userId = Guid.NewGuid();

        _credentialRepo
            .Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeCredential(userId)]);

        SyncIBKRHoldingsCommand? capturedCommand = null;
        _syncHandler
            .Setup(h => h.Handle(It.IsAny<SyncIBKRHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .Callback<SyncIBKRHoldingsCommand, CancellationToken>((cmd, _) =>
                capturedCommand = cmd)
            .ReturnsAsync(new SyncIBKRHoldingsResult(1, DateTime.UtcNow));

        await CreateJob().ExecuteAsync();

        capturedCommand.Should().NotBeNull();
        capturedCommand!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task ExecuteAsync_OneCredentialFails_OtherCredentialsStillSynced()
    {
        var failingUser = Guid.NewGuid();
        var successUser = Guid.NewGuid();

        _credentialRepo
            .Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeCredential(failingUser), MakeCredential(successUser)]);

        _syncHandler
            .Setup(h => h.Handle(
                It.Is<SyncIBKRHoldingsCommand>(c => c.UserId == failingUser),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated sync failure"));

        _syncHandler
            .Setup(h => h.Handle(
                It.Is<SyncIBKRHoldingsCommand>(c => c.UserId == successUser),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncIBKRHoldingsResult(2, DateTime.UtcNow));

        await CreateJob().Invoking(j => j.ExecuteAsync()).Should().NotThrowAsync();

        _syncHandler.Verify(
            h => h.Handle(
                It.Is<SyncIBKRHoldingsCommand>(c => c.UserId == successUser),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
