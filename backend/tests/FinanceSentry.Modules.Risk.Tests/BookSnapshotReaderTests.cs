using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.Risk.Application.Services;
using FinanceSentry.Modules.Risk.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceSentry.Modules.Risk.Tests;

/// <summary>
/// Tests for <see cref="BookFiguresReader"/> (the canonical computation) and
/// <see cref="BookSnapshotReader"/> (the adapter that maps BookFigures → BookSnapshot
/// for the Risk module's internal domain).
/// </summary>
public sealed class BookSnapshotReaderTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    // ── Fake readers ────────────────────────────────────────────────────────────

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

        public Task<DateTime?> GetLatestSuccessfulSyncAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<DateTime?>(DateTime.UtcNow);
    }

    private static BookFiguresReader BuildFiguresReader(
        bool cryptoThrows = false,
        IReadOnlyList<CryptoHoldingSummary>? crypto = null,
        bool brokerageThrows = false,
        IReadOnlyList<BrokerageHoldingSummary>? brokerage = null,
        bool bankingThrows = false,
        decimal bankingTotal = 0m)
        => new(
            new FakeCryptoReader(cryptoThrows, crypto ?? []),
            new FakeBrokerageReader(brokerageThrows, brokerage ?? []),
            new FakeBankingReader(bankingThrows, bankingTotal),
            NullLogger<BookFiguresReader>.Instance);

    // ── BookFiguresReader tests ──────────────────────────────────────────────────

    [Fact]
    public async Task BookFigures_AllSourcesOk_ReturnsFullBook_NotStale()
    {
        var reader = BuildFiguresReader(
            crypto: [new CryptoHoldingSummary("BTC", 1m, 0m, 5000m, DateTime.UtcNow, "binance")],
            brokerage: [new BrokerageHoldingSummary("NVDA", "STK", 10m, 4000m, DateTime.UtcNow, "ibkr")],
            bankingTotal: 1000m);

        var figures = await reader.ReadAsync(UserId);

        figures.IsStale.Should().BeFalse();
        figures.TotalUsd.Should().Be(10000m);
        figures.CashUsd.Should().Be(1000m);
        figures.BankingCashUsd.Should().Be(1000m);
        figures.BrokerageCashUsd.Should().Be(0m);
        figures.InvestedValueUsd.Should().Be(9000m);
        figures.Positions.Should().HaveCount(2);
    }

    [Fact]
    public async Task BookFigures_IdleBrokerageCash_BucketsAsCash_NotAsPosition()
    {
        var reader = BuildFiguresReader(
            brokerage:
            [
                new BrokerageHoldingSummary("AAPL", "STK", 10m, 1900m, DateTime.UtcNow, "ibkr"),
                new BrokerageHoldingSummary("EUR", "CASH", 800m, 923m, DateTime.UtcNow, "ibkr"),
            ],
            bankingTotal: 5000m);

        var figures = await reader.ReadAsync(UserId);

        figures.BrokerageCashUsd.Should().Be(923m);
        figures.BankingCashUsd.Should().Be(5000m);
        figures.CashUsd.Should().Be(5923m);
        figures.InvestedValueUsd.Should().Be(1900m);
        figures.TotalUsd.Should().Be(7823m);
        figures.Positions.Should().ContainSingle(p => p.Symbol == "AAPL");
        figures.Positions.Should().NotContain(p => p.Symbol == "EUR");
    }

    [Fact]
    public async Task BookFigures_PositionAssetClasses_AreNormalizedFromInstrumentType()
    {
        var reader = BuildFiguresReader(
            brokerage:
            [
                new BrokerageHoldingSummary("AAPL", "STK", 10m, 1000m, DateTime.UtcNow, "ibkr"),
                new BrokerageHoldingSummary("TLT", "BOND", 5m, 500m, DateTime.UtcNow, "ibkr"),
            ],
            crypto: [new CryptoHoldingSummary("BTC", 1m, 0m, 2000m, DateTime.UtcNow, "binance")]);

        var figures = await reader.ReadAsync(UserId);

        figures.Positions.Should().Contain(p => p.Symbol == "AAPL" && p.AssetClass == AssetClassNormalizer.Equities);
        figures.Positions.Should().Contain(p => p.Symbol == "TLT" && p.AssetClass == AssetClassNormalizer.Bonds);
        figures.Positions.Should().Contain(p => p.Symbol == "BTC" && p.AssetClass == AssetClassNormalizer.Crypto);
    }

    [Fact]
    public async Task BookFigures_OneSourceFails_MarksStale_ButKeepsOthers()
    {
        var reader = BuildFiguresReader(
            cryptoThrows: true,
            brokerage: [new BrokerageHoldingSummary("NVDA", "STK", 10m, 4000m, DateTime.UtcNow, "ibkr")],
            bankingTotal: 1000m);

        var figures = await reader.ReadAsync(UserId);

        figures.IsStale.Should().BeTrue();
        figures.StaleSources.Should().Contain("crypto");
        figures.Positions.Should().ContainSingle();
    }

    [Fact]
    public async Task BookFigures_AllSourcesFail_ReturnsEmptyStaleBook()
    {
        var reader = BuildFiguresReader(cryptoThrows: true, brokerageThrows: true, bankingThrows: true);

        var figures = await reader.ReadAsync(UserId);

        figures.IsStale.Should().BeTrue();
        figures.StaleSources.Should().HaveCount(3);
        figures.TotalUsd.Should().Be(0m);
    }

    // ── BookSnapshotReader adapter tests ────────────────────────────────────────

    [Fact]
    public async Task BookSnapshot_DelegatesTo_BookFiguresReader_MapsFieldsCorrectly()
    {
        var figuresReader = BuildFiguresReader(
            crypto: [new CryptoHoldingSummary("BTC", 1m, 0m, 5000m, DateTime.UtcNow, "binance")],
            brokerage:
            [
                new BrokerageHoldingSummary("AAPL", "STK", 10m, 4000m, DateTime.UtcNow, "ibkr"),
                new BrokerageHoldingSummary("USD", "CASH", 1m, 500m, DateTime.UtcNow, "ibkr"),
            ],
            bankingTotal: 1000m);

        var snapshotReader = new BookSnapshotReader(figuresReader);
        var book = await snapshotReader.ReadAsync(UserId);

        book.IsStale.Should().BeFalse();
        book.TotalUsd.Should().Be(10500m);
        book.CashUsd.Should().Be(1500m);
        book.BankingCashUsd.Should().Be(1000m);
        book.BrokerageCashUsd.Should().Be(500m);
        // Positions excludes the idle CASH holding
        book.Positions.Should().HaveCount(2);
        book.Positions.Should().Contain(p => p.Symbol == "BTC" && p.Sleeve == RiskSleeve.Crypto);
        book.Positions.Should().Contain(p => p.Symbol == "AAPL" && p.Sleeve == RiskSleeve.Brokerage);
    }
}
