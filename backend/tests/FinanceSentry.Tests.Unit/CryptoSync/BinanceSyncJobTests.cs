using FinanceSentry.Modules.CryptoSync.Application.Commands;
using FinanceSentry.Modules.CryptoSync.Domain;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;
using FinanceSentry.Modules.CryptoSync.Infrastructure.Jobs;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FinanceSentry.Tests.Unit.CryptoSync;

public class BinanceSyncJobTests
{
    private readonly Mock<IBinanceCredentialRepository> _credentialRepo = new(MockBehavior.Loose);
    private readonly Mock<IMediator> _mediator = new(MockBehavior.Loose);

    private BinanceSyncJob CreateJob() =>
        new(_credentialRepo.Object, _mediator.Object, NullLogger<BinanceSyncJob>.Instance);

    private static BinanceCredential MakeCredential(Guid userId) =>
        BinanceCredential.Create(userId, [1], [2], [3], [4], [5], [6], 1);

    [Fact]
    public async Task ExecuteAsync_IteratesAllActiveCredentials()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        _credentialRepo
            .Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeCredential(user1), MakeCredential(user2)]);

        _mediator
            .Setup(m => m.Send(It.IsAny<SyncBinanceHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncBinanceHoldingsResult(0, DateTime.UtcNow));

        await CreateJob().ExecuteAsync();

        _mediator.Verify(
            m => m.Send(It.IsAny<SyncBinanceHoldingsCommand>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_DispatchesSyncCommandPerCredential()
    {
        var userId = Guid.NewGuid();

        _credentialRepo
            .Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeCredential(userId)]);

        SyncBinanceHoldingsCommand? capturedCommand = null;
        _mediator
            .Setup(m => m.Send(It.IsAny<SyncBinanceHoldingsCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<SyncBinanceHoldingsResult>, CancellationToken>((cmd, _) =>
                capturedCommand = cmd as SyncBinanceHoldingsCommand)
            .ReturnsAsync(new SyncBinanceHoldingsResult(1, DateTime.UtcNow));

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

        _mediator
            .Setup(m => m.Send(
                It.Is<SyncBinanceHoldingsCommand>(c => c.UserId == failingUser),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated sync failure"));

        _mediator
            .Setup(m => m.Send(
                It.Is<SyncBinanceHoldingsCommand>(c => c.UserId == successUser),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncBinanceHoldingsResult(2, DateTime.UtcNow));

        // Should not throw even when one credential fails
        await CreateJob().Invoking(j => j.ExecuteAsync()).Should().NotThrowAsync();

        _mediator.Verify(
            m => m.Send(
                It.Is<SyncBinanceHoldingsCommand>(c => c.UserId == successUser),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
