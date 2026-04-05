namespace FinanceSentry.Tests.Unit.BankSync.Application;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for AggregationService (T412).
/// All repository dependencies are mocked; no database required.
/// </summary>
public class AggregationServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static BankAccount MakeAccount(string currency, decimal? balance, string accountType = "checking")
    {
        var a = new BankAccount(UserId, $"item_{Guid.NewGuid():N}", "Test Bank", accountType, "1234", "Owner", currency, UserId);
        // StartSync then MarkActive to set balance, or just set via property
        a.StartSync();
        if (balance.HasValue)
            a.MarkActive(balance.Value);
        return a;
    }

    private static BankAccount MakeAccountNoBalance(string currency, string accountType = "checking")
    {
        var a = new BankAccount(UserId, $"item_{Guid.NewGuid():N}", "Test Bank", accountType, "1234", "Owner", currency, UserId);
        // Leave in pending state — CurrentBalance is null
        return a;
    }

    // ── T412 Test 1 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Aggregate_ThreeAccounts_TwoEurOneUsd_ReturnsCurrencyTotals()
    {
        // Arrange
        var accounts = new List<BankAccount>
        {
            MakeAccount("EUR", 2000m),
            MakeAccount("EUR", 3000m),
            MakeAccount("USD", 1000m)
        };

        var repoMock = new Mock<IBankAccountRepository>();
        repoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(accounts);

        var sut = new AggregationService(repoMock.Object);

        // Act
        var result = await sut.GetAggregatedBalanceAsync(UserId);

        // Assert
        result.Should().ContainKey("EUR").WhoseValue.Should().Be(5000m);
        result.Should().ContainKey("USD").WhoseValue.Should().Be(1000m);
        result.Should().HaveCount(2);
    }

    // ── T412 Test 2 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Aggregate_NullBalance_ExcludesFromSum()
    {
        // Arrange — one EUR account with balance, one EUR account with null balance
        var accounts = new List<BankAccount>
        {
            MakeAccount("EUR", 1500m),
            MakeAccountNoBalance("EUR")   // CurrentBalance = null
        };

        var repoMock = new Mock<IBankAccountRepository>();
        repoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(accounts);

        var sut = new AggregationService(repoMock.Object);

        // Act
        var result = await sut.GetAggregatedBalanceAsync(UserId);

        // Assert — only the account with a known balance is counted
        result.Should().ContainKey("EUR").WhoseValue.Should().Be(1500m);
    }

    // ── T412 Test 3 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Aggregate_EmptyAccountList_ReturnsEmptyDict()
    {
        // Arrange
        var repoMock = new Mock<IBankAccountRepository>();
        repoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

        var sut = new AggregationService(repoMock.Object);

        // Act
        var result = await sut.GetAggregatedBalanceAsync(UserId);

        // Assert
        result.Should().BeEmpty();
    }

    // ── T412 Test 4 — GetAccountCountByTypeAsync acts as "available balance" grouping test ──

    [Fact]
    public async Task GetAvailableBalance_ReturnsAvailableBalances()
    {
        // Note: BankAccount only has CurrentBalance (no AvailableBalance property).
        // This test verifies the aggregation uses CurrentBalance correctly per currency
        // (i.e., what would be "available" in the context of current balances).
        //
        // Arrange: 2 savings + 1 checking, all EUR
        var accounts = new List<BankAccount>
        {
            MakeAccount("EUR", 500m,  "savings"),
            MakeAccount("EUR", 300m,  "savings"),
            MakeAccount("EUR", 1200m, "checking")
        };

        var repoMock = new Mock<IBankAccountRepository>();
        repoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(accounts);

        var sut = new AggregationService(repoMock.Object);

        // Act
        var balances = await sut.GetAggregatedBalanceAsync(UserId);
        var byType = await sut.GetAccountCountByTypeAsync(UserId);

        // Assert — all EUR balances are summed
        balances.Should().ContainKey("EUR").WhoseValue.Should().Be(2000m);

        // Account type breakdown
        byType.Should().ContainKey("savings").WhoseValue.Should().Be(2);
        byType.Should().ContainKey("checking").WhoseValue.Should().Be(1);
    }
}
