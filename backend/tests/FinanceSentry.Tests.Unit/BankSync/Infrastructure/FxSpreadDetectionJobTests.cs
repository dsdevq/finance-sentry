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
/// Unit tests for <see cref="FxSpreadDetectionJob"/> (044/US4).
/// Verifies that the sentinel fires when the implied EUR→UAH conversion rate is sufficiently below
/// the market rate, and stays silent when the rate is within tolerance or when no matching
/// cross-currency flows exist.
/// </summary>
public class FxSpreadDetectionJobTests
{
    private readonly Mock<IAlertGeneratorService> _alerts = new();

    // EUR→UAH market rate from CurrencyConverter fallback rates: 1.08 / 0.024 = 45
    private const decimal EurUahMarketRate = 45m;

    private static BankSyncDbContext NewDb() => new(
        new DbContextOptionsBuilder<BankSyncDbContext>()
            .UseInMemoryDatabase($"fxspread-{Guid.NewGuid():N}").Options);

    private static IConfiguration DefaultConfig() =>
        new ConfigurationBuilder().Build();

    private static IConfiguration ConfigWith(int lookbackDays, decimal threshold) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HygieneSentinels:FxSpreadLookbackDays"] = lookbackDays.ToString(),
                ["HygieneSentinels:FxSpreadThreshold"] = threshold.ToString("F4"),
            })
            .Build();

    private FxSpreadDetectionJob MakeJob(BankSyncDbContext db, IConfiguration? config = null) =>
        new(db, _alerts.Object, config ?? DefaultConfig(),
            Mock.Of<ILogger<FxSpreadDetectionJob>>());

    private static BankAccount MakeAccount(Guid userId, string currency)
    {
        var account = new BankAccount(userId, Guid.NewGuid().ToString(), "Test Bank",
            "current", "1234", "Owner", currency, Guid.NewGuid(), "monobank");
        return account;
    }

    private static Transaction MakeTx(BankAccount account, decimal amount, DateTime? date = null)
    {
        var tx = new Transaction(account.Id, account.UserId, amount,
            date ?? DateTime.UtcNow, "conversion", Guid.NewGuid().ToString());
        tx.IsActive = true;
        return tx;
    }

    [Fact]
    public async Task ExecuteAsync_AlertFired_WhenImpliedRateIsBelowMarketByMoreThanThreshold()
    {
        await using var db = NewDb();
        var userId = Guid.NewGuid();
        var eurAccount = MakeAccount(userId, "EUR");
        var uahAccount = MakeAccount(userId, "UAH");
        db.BankAccounts.AddRange(eurAccount, uahAccount);

        var today = DateTime.UtcNow;
        // EUR outflow: -100 EUR; UAH inflow: 4050 UAH (implied rate = 40.5, market = 45)
        // Spread = (45 - 40.5) / 45 = 10% > default 3% threshold
        db.Transactions.AddRange(
            MakeTx(eurAccount, -100m, today),
            MakeTx(uahAccount, 4050m, today));
        await db.SaveChangesAsync();

        await MakeJob(db).ExecuteAsync();

        _alerts.Verify(a => a.GenerateFxSpreadAlertAsync(
            userId, "EUR", "UAH", It.IsAny<decimal>(), It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NoAlert_WhenImpliedRateIsWithinThreshold()
    {
        await using var db = NewDb();
        var userId = Guid.NewGuid();
        var eurAccount = MakeAccount(userId, "EUR");
        var uahAccount = MakeAccount(userId, "UAH");
        db.BankAccounts.AddRange(eurAccount, uahAccount);

        var today = DateTime.UtcNow;
        // EUR outflow: -100 EUR; UAH inflow: 4450 UAH (implied = 44.5, market = 45)
        // Spread = (45 - 44.5) / 45 ≈ 1.1% < 3% threshold
        db.Transactions.AddRange(
            MakeTx(eurAccount, -100m, today),
            MakeTx(uahAccount, 4450m, today));
        await db.SaveChangesAsync();

        await MakeJob(db).ExecuteAsync();

        _alerts.Verify(a => a.GenerateFxSpreadAlertAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NoAlert_WhenUserHasOnlyOneCurrency()
    {
        await using var db = NewDb();
        var userId = Guid.NewGuid();
        var eur1 = MakeAccount(userId, "EUR");
        var eur2 = MakeAccount(userId, "EUR");
        db.BankAccounts.AddRange(eur1, eur2);
        db.Transactions.Add(MakeTx(eur1, -100m));
        await db.SaveChangesAsync();

        await MakeJob(db).ExecuteAsync();

        _alerts.Verify(a => a.GenerateFxSpreadAlertAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NoAlert_WhenNoDayMatchExists()
    {
        await using var db = NewDb();
        var userId = Guid.NewGuid();
        var eurAccount = MakeAccount(userId, "EUR");
        var uahAccount = MakeAccount(userId, "UAH");
        db.BankAccounts.AddRange(eurAccount, uahAccount);

        // EUR outflow on day 1, UAH inflow 10 days later — no ±1-day match
        db.Transactions.AddRange(
            MakeTx(eurAccount, -100m, DateTime.UtcNow.AddDays(-10)),
            MakeTx(uahAccount, 4050m, DateTime.UtcNow));
        await db.SaveChangesAsync();

        await MakeJob(db).ExecuteAsync();

        _alerts.Verify(a => a.GenerateFxSpreadAlertAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UsesConfigurableThreshold()
    {
        await using var db = NewDb();
        var userId = Guid.NewGuid();
        var eurAccount = MakeAccount(userId, "EUR");
        var uahAccount = MakeAccount(userId, "UAH");
        db.BankAccounts.AddRange(eurAccount, uahAccount);

        var today = DateTime.UtcNow;
        // Implied 44.0 (spread ≈ 2.2%): below 3% default but above 2% custom threshold
        db.Transactions.AddRange(
            MakeTx(eurAccount, -100m, today),
            MakeTx(uahAccount, 4400m, today));
        await db.SaveChangesAsync();

        await MakeJob(db, ConfigWith(30, 0.02m)).ExecuteAsync();

        _alerts.Verify(a => a.GenerateFxSpreadAlertAsync(
            userId, "EUR", "UAH", It.IsAny<decimal>(), It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NoAlert_WhenTransactionOutsideLookbackWindow()
    {
        await using var db = NewDb();
        var userId = Guid.NewGuid();
        var eurAccount = MakeAccount(userId, "EUR");
        var uahAccount = MakeAccount(userId, "UAH");
        db.BankAccounts.AddRange(eurAccount, uahAccount);

        // Both transactions are 40 days old — outside a 30-day lookback window
        var old = DateTime.UtcNow.AddDays(-40);
        db.Transactions.AddRange(
            MakeTx(eurAccount, -100m, old),
            MakeTx(uahAccount, 4050m, old));
        await db.SaveChangesAsync();

        await MakeJob(db, ConfigWith(30, 0.03m)).ExecuteAsync();

        _alerts.Verify(a => a.GenerateFxSpreadAlertAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ImpliedRateIsVolumeWeightedAverage()
    {
        await using var db = NewDb();
        var userId = Guid.NewGuid();
        var eurAccount = MakeAccount(userId, "EUR");
        var uahAccount = MakeAccount(userId, "UAH");
        db.BankAccounts.AddRange(eurAccount, uahAccount);

        var day1 = DateTime.UtcNow.AddDays(-2);
        var day2 = DateTime.UtcNow.AddDays(-1);

        // Day 1: -100 EUR → 4500 UAH (exactly market — within threshold, no spike)
        // Day 2: -100 EUR → 3600 UAH (implied 36, 20% spread)
        // Combined: -200 EUR → 8100 UAH, implied = 40.5, spread = 10% > 3%
        db.Transactions.AddRange(
            MakeTx(eurAccount, -100m, day1),
            MakeTx(uahAccount, 4500m, day1),
            MakeTx(eurAccount, -100m, day2),
            MakeTx(uahAccount, 3600m, day2));
        await db.SaveChangesAsync();

        await MakeJob(db).ExecuteAsync();

        _alerts.Verify(a => a.GenerateFxSpreadAlertAsync(
            userId, "EUR", "UAH", It.IsAny<decimal>(), It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
