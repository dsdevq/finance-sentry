namespace FinanceSentry.Tests.Unit.BankSync.Infrastructure;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Infrastructure.Jobs;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="CategorySpikeDetectionJob"/> (044/US3).
/// Verifies the 4-month minimum history gate, 6-month baseline averaging, configurable multiplier,
/// and USD-normalised comparison.
/// </summary>
public class CategorySpikeDetectionJobTests
{
    private readonly Mock<IAlertGeneratorService> _alerts = new();

    private static BankSyncDbContext NewDb() => new(
        new DbContextOptionsBuilder<BankSyncDbContext>()
            .UseInMemoryDatabase($"catspike-{Guid.NewGuid():N}").Options);

    private static IConfiguration DefaultConfig() =>
        new ConfigurationBuilder().Build();

    private static IConfiguration ConfigWithMultiplier(decimal multiplier) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HygieneSentinels:CategorySpikeMultiplier"] = multiplier.ToString("F4"),
            })
            .Build();

    private CategorySpikeDetectionJob MakeJob(BankSyncDbContext db, IConfiguration? config = null) =>
        new(db, _alerts.Object, config ?? DefaultConfig(),
            Mock.Of<ILogger<CategorySpikeDetectionJob>>());

    private static BankAccount MakeAccount(Guid userId, string currency = "EUR")
    {
        var account = new BankAccount(userId, Guid.NewGuid().ToString(), "Test Bank",
            "current", "1234", "Owner", currency, Guid.NewGuid(), "monobank");
        return account;
    }

    private static Transaction MakeTx(BankAccount account, decimal amount, string category, DateTime date)
    {
        var tx = new Transaction(account.Id, account.UserId, amount, date, "desc", Guid.NewGuid().ToString());
        tx.MerchantCategory = category;
        tx.IsActive = true;
        return tx;
    }

    /// <summary>
    /// Builds 6 months of historic spend + a current-month spike and expects an alert.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_AlertFired_WhenCurrentMonthExceedsBaselineByMultiplier()
    {
        await using var db = NewDb();
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        db.BankAccounts.Add(account);

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // 6 months of 100 EUR spend each
        for (var i = 1; i <= 6; i++)
        {
            db.Transactions.Add(MakeTx(account, -100m, "FOOD_AND_DRINK",
                currentMonthStart.AddMonths(-i).AddDays(5)));
        }

        // Current month: 300 EUR — 3× the baseline, well above the default 2.0× threshold
        db.Transactions.Add(MakeTx(account, -300m, "FOOD_AND_DRINK", currentMonthStart.AddDays(5)));

        await db.SaveChangesAsync();

        await MakeJob(db).ExecuteAsync();

        _alerts.Verify(a => a.GenerateCategorySpikeAlertAsync(
            userId, "FOOD_AND_DRINK", It.IsAny<decimal>(), It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NoAlert_WhenCurrentMonthBelowThreshold()
    {
        await using var db = NewDb();
        var account = MakeAccount(Guid.NewGuid());
        db.BankAccounts.Add(account);

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 1; i <= 6; i++)
        {
            db.Transactions.Add(MakeTx(account, -100m, "TRANSPORT",
                currentMonthStart.AddMonths(-i).AddDays(5)));
        }
        // Current month: 110 EUR — 10% above baseline, well below the 2.0× default
        db.Transactions.Add(MakeTx(account, -110m, "TRANSPORT", currentMonthStart.AddDays(5)));

        await db.SaveChangesAsync();

        await MakeJob(db).ExecuteAsync();

        _alerts.Verify(a => a.GenerateCategorySpikeAlertAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NoAlert_WhenInsufficientHistory()
    {
        await using var db = NewDb();
        var account = MakeAccount(Guid.NewGuid());
        db.BankAccounts.Add(account);

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Only 3 months of history — below the 4-month minimum requirement
        for (var i = 1; i <= 3; i++)
        {
            db.Transactions.Add(MakeTx(account, -100m, "SHOPPING",
                currentMonthStart.AddMonths(-i).AddDays(5)));
        }
        db.Transactions.Add(MakeTx(account, -500m, "SHOPPING", currentMonthStart.AddDays(5)));

        await db.SaveChangesAsync();

        await MakeJob(db).ExecuteAsync();

        _alerts.Verify(a => a.GenerateCategorySpikeAlertAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AlertFired_WhenExactlyMinimumHistoryMonths()
    {
        await using var db = NewDb();
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        db.BankAccounts.Add(account);

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Exactly 4 months of history — meets the minimum; current-month spike should fire.
        for (var i = 1; i <= 4; i++)
        {
            db.Transactions.Add(MakeTx(account, -100m, "SHOPPING",
                currentMonthStart.AddMonths(-i).AddDays(5)));
        }
        // 400 EUR — 4× the 100 EUR baseline, comfortably above the 2.0× threshold
        db.Transactions.Add(MakeTx(account, -400m, "SHOPPING", currentMonthStart.AddDays(5)));

        await db.SaveChangesAsync();

        await MakeJob(db).ExecuteAsync();

        _alerts.Verify(a => a.GenerateCategorySpikeAlertAsync(
            userId, "SHOPPING", It.IsAny<decimal>(), It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UsesConfigurableMultiplier()
    {
        await using var db = NewDb();
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        db.BankAccounts.Add(account);

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 1; i <= 6; i++)
        {
            db.Transactions.Add(MakeTx(account, -100m, "ENTERTAINMENT",
                currentMonthStart.AddMonths(-i).AddDays(5)));
        }
        // 120 EUR — 20% above baseline: above 1.1× custom threshold, below 2.0× default
        db.Transactions.Add(MakeTx(account, -120m, "ENTERTAINMENT", currentMonthStart.AddDays(5)));

        await db.SaveChangesAsync();

        await MakeJob(db, ConfigWithMultiplier(1.1m)).ExecuteAsync();

        _alerts.Verify(a => a.GenerateCategorySpikeAlertAsync(
            userId, "ENTERTAINMENT", It.IsAny<decimal>(), It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NoAlert_WhenNoCurrentMonthSpend()
    {
        await using var db = NewDb();
        var account = MakeAccount(Guid.NewGuid());
        db.BankAccounts.Add(account);

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // 6 months of history but no current-month spend
        for (var i = 1; i <= 6; i++)
        {
            db.Transactions.Add(MakeTx(account, -100m, "HEALTH",
                currentMonthStart.AddMonths(-i).AddDays(5)));
        }

        await db.SaveChangesAsync();

        await MakeJob(db).ExecuteAsync();

        _alerts.Verify(a => a.GenerateCategorySpikeAlertAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OnlyNegativeAmountsCountAsSpend()
    {
        await using var db = NewDb();
        var account = MakeAccount(Guid.NewGuid());
        db.BankAccounts.Add(account);

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 1; i <= 6; i++)
        {
            db.Transactions.Add(MakeTx(account, -100m, "INCOME",
                currentMonthStart.AddMonths(-i).AddDays(5)));
        }
        // Positive amounts (inflows) should not be treated as spend
        db.Transactions.Add(MakeTx(account, 999m, "INCOME", currentMonthStart.AddDays(5)));

        await db.SaveChangesAsync();

        await MakeJob(db).ExecuteAsync();

        _alerts.Verify(a => a.GenerateCategorySpikeAlertAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
