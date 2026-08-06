using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Risk.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceSentry.Modules.Risk.Tests;

public sealed class BookSnapshotReaderTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private sealed class FakeCryptoReader(bool throws, IReadOnlyList<CryptoHoldingSummary> holdings) : ICryptoHoldingsReader
    {
        public Task<IReadOnlyList<CryptoHoldingSummary>> GetHoldingsAsync(Guid userId, CancellationToken ct = default)
            => throws ? throw new InvalidOperationException("boom") : Task.FromResult(holdings);
    }

    private sealed class FakeBrokerageReader(bool throws, IReadOnlyList<BrokerageHoldingSummary> holdings) : IBrokerageHoldingsReader
    {
        public Task<IReadOnlyList<BrokerageHoldingSummary>> GetHoldingsAsync(Guid userId, CancellationToken ct = default)
            => throws ? throw new InvalidOperationException("boom") : Task.FromResult(holdings);
    }

    private sealed class FakeBankingReader(bool throws, decimal total) : IBankingTotalsReader
    {
        public Task<IReadOnlyList<Guid>> GetActiveUserIdsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Guid>>([UserId]);

        public Task<decimal> GetTotalUsdAsync(Guid userId, CancellationToken ct = default)
            => throws ? throw new InvalidOperationException("boom") : Task.FromResult(total);
    }

    [Fact]
    public async Task AllSourcesOk_ReturnsFullBook_NotStale()
    {
        var reader = new BookSnapshotReader(
            new FakeCryptoReader(false, [new CryptoHoldingSummary("BTC", 1m, 0m, 5000m, DateTime.UtcNow, "Binance")]),
            new FakeBrokerageReader(false, [new BrokerageHoldingSummary("NVDA", "STK", 10m, 4000m, DateTime.UtcNow, "IBKR")]),
            new FakeBankingReader(false, 1000m),
            NullLogger<BookSnapshotReader>.Instance);

        var book = await reader.ReadAsync(UserId);

        book.IsStale.Should().BeFalse();
        book.TotalUsd.Should().Be(10000m);
        book.CashUsd.Should().Be(1000m);
        book.Positions.Should().HaveCount(2);
    }

    [Fact]
    public async Task OneSourceFails_MarksStale_ButKeepsOthers()
    {
        var reader = new BookSnapshotReader(
            new FakeCryptoReader(true, []),
            new FakeBrokerageReader(false, [new BrokerageHoldingSummary("NVDA", "STK", 10m, 4000m, DateTime.UtcNow, "IBKR")]),
            new FakeBankingReader(false, 1000m),
            NullLogger<BookSnapshotReader>.Instance);

        var book = await reader.ReadAsync(UserId);

        book.IsStale.Should().BeTrue();
        book.StaleSources.Should().Contain("crypto");
        book.Positions.Should().ContainSingle();
    }

    [Fact]
    public async Task AllSourcesFail_ReturnsEmptyStaleBook()
    {
        var reader = new BookSnapshotReader(
            new FakeCryptoReader(true, []),
            new FakeBrokerageReader(true, []),
            new FakeBankingReader(true, 0m),
            NullLogger<BookSnapshotReader>.Instance);

        var book = await reader.ReadAsync(UserId);

        book.IsStale.Should().BeTrue();
        book.StaleSources.Should().HaveCount(3);
        book.TotalUsd.Should().Be(0m);
    }
}
