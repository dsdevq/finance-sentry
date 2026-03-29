namespace FinanceSentry.Tests.Unit.BankSync.Application;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for MoneyFlowStatisticsService (T413).
/// All repository dependencies are mocked; no database required.
/// </summary>
public class MoneyFlowStatisticsTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a BankAccount and returns both the entity and its auto-generated Id.
    /// </summary>
    private static (BankAccount account, Guid accountId) MakeAccount(string currency)
    {
        var a = new BankAccount(UserId, $"item_{Guid.NewGuid():N}", "Bank", "checking", "1234", "Owner", currency, UserId);
        return (a, a.Id);
    }

    private static Transaction MakeTx(
        Guid accountId, decimal amount, string type, DateTime date, bool isPending = false)
    {
        var hash = Guid.NewGuid().ToString("N");
        var tx = new Transaction(accountId, UserId, amount, date, "desc", hash, isPending)
        {
            TransactionType = type,
            PostedDate = isPending ? null : date,
            IsActive = true
        };
        return tx;
    }

    // ── T413 Test 1: Six months of monthly flow ───────────────────────────────

    [Fact]
    public async Task GetMonthlyFlow_SixMonths_ReturnsCorrectInOutNet()
    {
        // Arrange: one credit and one debit per month for 6 months
        var (account, accountId) = MakeAccount("EUR");
        var now = DateTime.UtcNow;
        var transactions = new List<Transaction>();

        for (int i = 0; i < 6; i++)
        {
            var month = now.AddMonths(-i);
            var monthDate = new DateTime(month.Year, month.Month, 15, 0, 0, 0, DateTimeKind.Utc);
            transactions.Add(MakeTx(accountId, 1000m, "credit", monthDate));
            transactions.Add(MakeTx(accountId, 600m,  "debit",  monthDate));
        }

        var txRepoMock = new Mock<ITransactionRepository>();
        txRepoMock.Setup(r => r.GetByUserIdSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(transactions);

        var accountRepoMock = new Mock<IBankAccountRepository>();
        accountRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<BankAccount> { account });

        var sut = new MoneyFlowStatisticsService(txRepoMock.Object, accountRepoMock.Object);

        // Act
        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        // Assert: 6 months, each with 1000 inflow, 600 outflow, 400 net
        result.Should().HaveCount(6);
        result.Should().AllSatisfy(mf =>
        {
            mf.Inflow.Should().Be(1000m);
            mf.Outflow.Should().Be(600m);
            mf.Net.Should().Be(400m);
            mf.Currency.Should().Be("EUR");
        });

        // Results should be sorted by month DESC
        var months = result.Select(mf => mf.Month).ToList();
        months.Should().BeInDescendingOrder();
    }

    // ── T413 Test 2: Pending transactions excluded ────────────────────────────

    [Fact]
    public async Task GetMonthlyFlow_ExcludesPendingTransactions()
    {
        // Arrange
        var (account, accountId) = MakeAccount("EUR");
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(accountId, 500m, "credit", date, isPending: false),
            MakeTx(accountId, 200m, "credit", date, isPending: true),  // pending — must be excluded
            MakeTx(accountId, 100m, "debit",  date, isPending: false)
        };

        var txRepoMock = new Mock<ITransactionRepository>();
        txRepoMock.Setup(r => r.GetByUserIdSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(transactions);

        var accountRepoMock = new Mock<IBankAccountRepository>();
        accountRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<BankAccount> { account });

        var sut = new MoneyFlowStatisticsService(txRepoMock.Object, accountRepoMock.Object);

        // Act
        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        // Assert: only non-pending transactions counted → inflow=500, outflow=100
        result.Should().HaveCount(1);
        result[0].Inflow.Should().Be(500m);
        result[0].Outflow.Should().Be(100m);
        result[0].Net.Should().Be(400m);
    }

    // ── T413 Test 3: Multi-currency separate stats ────────────────────────────

    [Fact]
    public async Task GetMonthlyFlow_MultiCurrency_SeparateStatsPerCurrency()
    {
        // Arrange
        var (eurAccount, eurAccountId) = MakeAccount("EUR");
        var (usdAccount, usdAccountId) = MakeAccount("USD");
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(eurAccountId, 1000m, "credit", date),
            MakeTx(eurAccountId, 400m,  "debit",  date),
            MakeTx(usdAccountId, 800m,  "credit", date),
            MakeTx(usdAccountId, 300m,  "debit",  date)
        };

        var txRepoMock = new Mock<ITransactionRepository>();
        txRepoMock.Setup(r => r.GetByUserIdSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(transactions);

        var accountRepoMock = new Mock<IBankAccountRepository>();
        accountRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<BankAccount> { eurAccount, usdAccount });

        var sut = new MoneyFlowStatisticsService(txRepoMock.Object, accountRepoMock.Object);

        // Act
        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        // Assert: 2 rows — one for EUR, one for USD (same month)
        result.Should().HaveCount(2);

        var eurFlow = result.First(mf => mf.Currency == "EUR");
        eurFlow.Inflow.Should().Be(1000m);
        eurFlow.Outflow.Should().Be(400m);
        eurFlow.Net.Should().Be(600m);

        var usdFlow = result.First(mf => mf.Currency == "USD");
        usdFlow.Inflow.Should().Be(800m);
        usdFlow.Outflow.Should().Be(300m);
        usdFlow.Net.Should().Be(500m);
    }

    // ── T413 Test 4: Debit/credit classification ──────────────────────────────

    [Fact]
    public async Task GetMonthlyFlow_DebitCreditClassification()
    {
        // Arrange — credit = inflow, debit = outflow
        var (account, accountId) = MakeAccount("EUR");
        var date = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(accountId, 750m, "credit", date),  // → inflow
            MakeTx(accountId, 250m, "debit",  date),  // → outflow
            MakeTx(accountId, 150m, "credit", date),  // → inflow
        };

        var txRepoMock = new Mock<ITransactionRepository>();
        txRepoMock.Setup(r => r.GetByUserIdSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(transactions);

        var accountRepoMock = new Mock<IBankAccountRepository>();
        accountRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<BankAccount> { account });

        var sut = new MoneyFlowStatisticsService(txRepoMock.Object, accountRepoMock.Object);

        // Act
        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        // Assert
        result.Should().HaveCount(1);
        result[0].Inflow.Should().Be(900m);   // 750 + 150
        result[0].Outflow.Should().Be(250m);
        result[0].Net.Should().Be(650m);
    }
}
