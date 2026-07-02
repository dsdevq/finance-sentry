using FinanceSentry.Modules.BrokerageSync.Application.Connect;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceSentry.Tests.Unit.BrokerageSync;

public class IBKRConnectOrchestratorTests
{
    [Fact]
    public void Start_WhenNoInFlightSession_CreatesNew()
    {
        var store = new IBKRConnectSessionStore();
        var scopeFactory = new NoOpScopeFactory();
        var sut = new IBKRConnectOrchestrator(
            scopeFactory, store, NullLogger<IBKRConnectOrchestrator>.Instance);

        var userId = Guid.NewGuid();
        var sessionId = sut.Start(userId, "u", "p");

        sessionId.Should().NotBe(Guid.Empty);
        store.Get(sessionId, userId).Should().NotBeNull();
    }

    [Fact]
    public void Start_WhenInFlightSessionExists_ReturnsExistingSessionId()
    {
        var store = new IBKRConnectSessionStore();
        var scopeFactory = new NoOpScopeFactory();
        var sut = new IBKRConnectOrchestrator(
            scopeFactory, store, NullLogger<IBKRConnectOrchestrator>.Instance);

        var userId = Guid.NewGuid();
        var (existing, _) = store.Create(userId);

        var returned = sut.Start(userId, "u", "p");

        returned.Should().Be(existing);
    }

    [Fact]
    public void Start_WhenPriorSessionIsTerminal_CreatesNewSession()
    {
        var store = new IBKRConnectSessionStore();
        var scopeFactory = new NoOpScopeFactory();
        var sut = new IBKRConnectOrchestrator(
            scopeFactory, store, NullLogger<IBKRConnectOrchestrator>.Instance);

        var userId = Guid.NewGuid();
        var (dead, _) = store.Create(userId);
        store.MarkFailed(dead, "IBKR_INVALID_CREDENTIALS", "…");

        var fresh = sut.Start(userId, "u", "p");

        fresh.Should().NotBe(dead);
    }

    private sealed class NoOpScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new NoOpScope();

        private sealed class NoOpScope : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; } = new EmptyProvider();
            public void Dispose() { }
        }

        private sealed class EmptyProvider : IServiceProvider
        {
            public object? GetService(Type serviceType) => null;
        }
    }
}
