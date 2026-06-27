using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Mcp.Tools;
using FinanceSentry.Modules.Alerts.Domain;
using FinanceSentry.Modules.Alerts.Domain.Repositories;
using FinanceSentry.Modules.Alerts.Infrastructure.Persistence;
using FinanceSentry.Modules.Alerts.Infrastructure.Persistence.Repositories;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Services;
using FinanceSentry.Modules.BrokerageSync.Application.Services;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Persistence;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Persistence.Repositories;
using FinanceSentry.Modules.Budgets.Application.Services;
using FinanceSentry.Modules.Budgets.Domain;
using FinanceSentry.Modules.Budgets.Domain.Repositories;
using FinanceSentry.Modules.Budgets.Infrastructure.Persistence;
using FinanceSentry.Modules.Budgets.Infrastructure.Persistence.Repositories;
using FinanceSentry.Modules.CryptoSync.Application.Services;
using FinanceSentry.Modules.CryptoSync.Domain;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;
using FinanceSentry.Modules.CryptoSync.Infrastructure.Persistence;
using FinanceSentry.Modules.CryptoSync.Infrastructure.Persistence.Repositories;
using FinanceSentry.Modules.Subscriptions.Domain;
using FinanceSentry.Modules.Subscriptions.Domain.Repositories;
using FinanceSentry.Modules.Subscriptions.Infrastructure.Persistence;
using FinanceSentry.Modules.Subscriptions.Infrastructure.Persistence.Repositories;
using FinanceSentry.Modules.Wealth.Domain;
using FinanceSentry.Modules.Wealth.Domain.Repositories;
using FinanceSentry.Modules.Wealth.Infrastructure.Persistence;
using FinanceSentry.Modules.Wealth.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FinanceSentry.Mcp.Tests.IntegrationTests;

/// <summary>
/// Integration tests that invoke each real MCP tool against a seeded in-memory database
/// and assert structural validity of the response payload.
/// One [Fact] per tool; each builds its own isolated ServiceProvider so that seeded data
/// never leaks between tests.
/// </summary>
public sealed class ToolParityTests
{
    // ── DI helper ────────────────────────────────────────────────────────────

    // dbKey must be unique per test so that each test gets its own in-memory DB.
    private static ServiceProvider BuildProvider(string dbKey)
    {
        var services = new ServiceCollection();

        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        // Each DbContext gets an isolated in-memory store keyed by dbKey.
        services.AddDbContext<BankSyncDbContext>(o =>
            o.UseInMemoryDatabase($"banksync-{dbKey}"));
        services.AddDbContext<CryptoSyncDbContext>(o =>
            o.UseInMemoryDatabase($"crypto-{dbKey}"));
        services.AddDbContext<BrokerageSyncDbContext>(o =>
            o.UseInMemoryDatabase($"brokerage-{dbKey}"));
        services.AddDbContext<AlertsDbContext>(o =>
            o.UseInMemoryDatabase($"alerts-{dbKey}"));
        services.AddDbContext<BudgetsDbContext>(o =>
            o.UseInMemoryDatabase($"budgets-{dbKey}"));
        services.AddDbContext<SubscriptionsDbContext>(o =>
            o.UseInMemoryDatabase($"subscriptions-{dbKey}"));
        services.AddDbContext<WealthDbContext>(o =>
            o.UseInMemoryDatabase($"wealth-{dbKey}"));

        // Repositories — real implementations backed by the in-memory contexts above.
        services.AddScoped<IBankAccountRepository, BankAccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICryptoHoldingRepository, CryptoHoldingRepository>();
        services.AddScoped<IBrokerageHoldingRepository, BrokerageHoldingRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IDetectedSubscriptionRepository, DetectedSubscriptionRepository>();
        services.AddScoped<INetWorthSnapshotRepository, NetWorthSnapshotRepository>();

        // Cross-provider readers used by several tools.
        services.AddScoped<IBankingAccountsReader, BankingAccountsReader>();
        services.AddScoped<ICryptoHoldingsReader, CryptoHoldingsReader>();
        services.AddScoped<IBrokerageHoldingsReader, BrokerageHoldingsReader>();

        // Domain service needed by GetBudgetSummaryQueryHandler.
        services.AddScoped<ICategoryNormalizationService, CategoryNormalizationService>();

        // CQRS: registers all IQueryHandler / ICommandHandler / IEventHandler
        // implementations from each module assembly, wrapped in validation and logging
        // decorators.  Assemblies are resolved via the public module marker types so
        // that the correct DLL is guaranteed to be loaded.
        services.AddCqrs(
            typeof(FinanceSentry.Modules.BankSync.BankSyncModule).Assembly,
            typeof(FinanceSentry.Modules.Budgets.BudgetsModule).Assembly,
            typeof(FinanceSentry.Modules.Alerts.AlertsModule).Assembly,
            typeof(FinanceSentry.Modules.BrokerageSync.BrokerageSyncModule).Assembly,
            typeof(FinanceSentry.Modules.CryptoSync.CryptoSyncModule).Assembly,
            typeof(FinanceSentry.Modules.Subscriptions.SubscriptionsModule).Assembly,
            typeof(FinanceSentry.Modules.Wealth.WealthModule).Assembly);

        services.AddSingleton<FinanceSentry.Mcp.Abstractions.IIdentityResolver>(new FakeIdentityResolver());
        services.AddScoped<GetAccountSummaryTool>();
        services.AddScoped<ListTransactionsTool>();
        services.AddScoped<GetBudgetStatusTool>();
        services.AddScoped<ListActiveAlertsTool>();
        services.AddScoped<GetPortfolioSnapshotTool>();
        services.AddScoped<ListSubscriptionsTool>();
        services.AddScoped<GetSyncHealthTool>();
        services.AddScoped<GetNetWorthHistoryTool>();
        services.AddScoped<GetCashflowReportTool>();
        services.AddScoped<GetCryptoPnlDetailTool>();
        services.AddScoped<GetTaxLotsTool>();

        return services.BuildServiceProvider();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAccountSummary_ReturnsNonEmpty_WhenBankAndCryptoSeeded()
    {
        var userId = Guid.NewGuid();
        await using var sp = BuildProvider(Guid.NewGuid().ToString("N"));
        await using var scope = sp.CreateAsyncScope();
        var svc = scope.ServiceProvider;

        // Seed one active Plaid bank account.
        var bankDb = svc.GetRequiredService<BankSyncDbContext>();
        var account = new BankAccount(userId, "ext-acc-001", "Chase", "checking", "1234", "Test User", "USD", userId);
        account.BeginSync();
        account.MarkActive(5_000m);
        bankDb.BankAccounts.Add(account);
        await bankDb.SaveChangesAsync();

        // Seed one crypto holding.
        var cryptoDb = svc.GetRequiredService<CryptoSyncDbContext>();
        cryptoDb.CryptoHoldings.Add(CryptoHolding.Create(userId, "BTC", 0.1m, 0m, 6_500m));
        await cryptoDb.SaveChangesAsync();

        var tool = svc.GetRequiredService<GetAccountSummaryTool>();
        var result = await tool.ExecuteAsync(userId);

        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Should().AllSatisfy(e =>
        {
            e.AccountId.Should().NotBeNullOrEmpty();
            e.Provider.Should().NotBeNullOrEmpty();
            e.Currency.Should().NotBeNullOrEmpty();
        });
        result.Should().ContainSingle(e => e.Provider == "plaid");
        result.Should().ContainSingle(e => e.Provider == "binance");
    }

    [Fact]
    public async Task ListTransactions_ReturnsNonEmpty_WhenTransactionSeeded()
    {
        var userId = Guid.NewGuid();
        await using var sp = BuildProvider(Guid.NewGuid().ToString("N"));
        await using var scope = sp.CreateAsyncScope();
        var svc = scope.ServiceProvider;

        var bankDb = svc.GetRequiredService<BankSyncDbContext>();

        var account = new BankAccount(userId, "ext-txn-001", "BofA", "savings", "5678", "Test User", "USD", userId);
        account.BeginSync();
        account.MarkActive(2_000m);
        bankDb.BankAccounts.Add(account);

        var tx = new Transaction(account.Id, userId, 42.50m, DateTime.UtcNow, "Netflix subscription", "hash-txn-001");
        tx.MerchantCategory = "ENTERTAINMENT";
        bankDb.Transactions.Add(tx);

        await bankDb.SaveChangesAsync();

        var tool = svc.GetRequiredService<ListTransactionsTool>();
        var result = await tool.ExecuteAsync(userId);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(e =>
        {
            e.TransactionId.Should().NotBeNullOrEmpty();
            e.AccountId.Should().NotBeNullOrEmpty();
            e.Amount.Should().BeGreaterThan(0);
            e.Description.Should().NotBeNullOrEmpty();
            e.Currency.Should().NotBeNullOrEmpty();
            e.Provider.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetBudgetStatus_ReturnsNonEmpty_WhenBudgetSeeded()
    {
        var userId = Guid.NewGuid();
        await using var sp = BuildProvider(Guid.NewGuid().ToString("N"));
        await using var scope = sp.CreateAsyncScope();
        var svc = scope.ServiceProvider;

        // BankSyncDbContext must be initialised so that GetMerchantSpendingQueryHandler
        // can query transactions (returns empty → spending = 0, which is fine).
        _ = svc.GetRequiredService<BankSyncDbContext>();

        var budgetDb = svc.GetRequiredService<BudgetsDbContext>();
        budgetDb.Budgets.Add(Budget.Create(userId, "food_and_drink", 500m, "USD"));
        await budgetDb.SaveChangesAsync();

        var tool = svc.GetRequiredService<GetBudgetStatusTool>();
        var result = await tool.ExecuteAsync(userId);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(e =>
        {
            e.BudgetId.Should().NotBeNullOrEmpty();
            e.Name.Should().NotBeNullOrEmpty();
            e.Period.Should().MatchRegex(@"^\d{4}-\d{2}$");
            e.LimitAmount.Should().BeGreaterThan(0);
            e.Currency.Should().NotBeNullOrEmpty();
            e.SpentAmount.Should().BeGreaterThanOrEqualTo(0);
            e.UtilizationPercent.Should().BeGreaterThanOrEqualTo(0);
        });
    }

    [Fact]
    public async Task ListActiveAlerts_ReturnsNonEmpty_WhenUnreadAlertSeeded()
    {
        var userId = Guid.NewGuid();
        await using var sp = BuildProvider(Guid.NewGuid().ToString("N"));
        await using var scope = sp.CreateAsyncScope();
        var svc = scope.ServiceProvider;

        var alertsDb = svc.GetRequiredService<AlertsDbContext>();
        alertsDb.Alerts.Add(new Alert
        {
            UserId = userId,
            Type = AlertType.LowBalance,
            Severity = AlertSeverity.Warning,
            Title = "Low balance alert",
            Message = "Your checking account balance is below $100.",
            IsRead = false,
            IsResolved = false,
            IsDismissed = false,
        });
        await alertsDb.SaveChangesAsync();

        var tool = svc.GetRequiredService<ListActiveAlertsTool>();
        var result = await tool.ExecuteAsync(userId);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(e =>
        {
            e.AlertId.Should().NotBeNullOrEmpty();
            e.Type.Should().NotBeNullOrEmpty();
            e.Severity.Should().NotBeNullOrEmpty();
            e.Title.Should().NotBeNullOrEmpty();
            e.Message.Should().NotBeNullOrEmpty();
            e.Status.Should().Be("Fired");
        });
    }

    [Fact]
    public async Task GetPortfolioSnapshot_ReturnsNonEmpty_WhenBrokerageAndCryptoHoldingsSeeded()
    {
        var userId = Guid.NewGuid();
        await using var sp = BuildProvider(Guid.NewGuid().ToString("N"));
        await using var scope = sp.CreateAsyncScope();
        var svc = scope.ServiceProvider;

        var cryptoDb = svc.GetRequiredService<CryptoSyncDbContext>();
        cryptoDb.CryptoHoldings.Add(CryptoHolding.Create(userId, "ETH", 2.5m, 0m, 9_000m));
        await cryptoDb.SaveChangesAsync();

        var brokerageDb = svc.GetRequiredService<BrokerageSyncDbContext>();
        brokerageDb.BrokerageHoldings.Add(new BrokerageHolding(userId, "AAPL", "STK", 10m, 1_900m, "ibkr"));
        await brokerageDb.SaveChangesAsync();

        var tool = svc.GetRequiredService<GetPortfolioSnapshotTool>();
        var result = await tool.ExecuteAsync(userId);

        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Should().AllSatisfy(e =>
        {
            e.Symbol.Should().NotBeNullOrEmpty();
            e.AssetClass.Should().NotBeNullOrEmpty();
            e.Provider.Should().NotBeNullOrEmpty();
            e.CurrentValue.Should().BeGreaterThan(0);
            e.Quantity.Should().BeGreaterThan(0);
        });
        result.Should().ContainSingle(e => e.Provider == "ibkr");
        result.Should().ContainSingle(e => e.Provider == "binance");
    }

    [Fact]
    public async Task ListSubscriptions_ReturnsNonEmpty_WhenActiveSubscriptionSeeded()
    {
        var userId = Guid.NewGuid();
        await using var sp = BuildProvider(Guid.NewGuid().ToString("N"));
        await using var scope = sp.CreateAsyncScope();
        var svc = scope.ServiceProvider;

        var subsDb = svc.GetRequiredService<SubscriptionsDbContext>();
        subsDb.DetectedSubscriptions.Add(DetectedSubscription.Create(
            userId.ToString(),
            "netflix",
            "Netflix",
            "monthly",
            15.99m,
            15.99m,
            "USD",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(23)),
            occurrenceCount: 3,
            confidenceScore: 85,
            category: "ENTERTAINMENT"));
        await subsDb.SaveChangesAsync();

        var tool = svc.GetRequiredService<ListSubscriptionsTool>();
        var result = await tool.ExecuteAsync(userId);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(e =>
        {
            e.SubscriptionId.Should().NotBeNullOrEmpty();
            e.Merchant.Should().NotBeNullOrEmpty();
            e.EstimatedMonthlyAmount.Should().BeGreaterThan(0);
            e.Currency.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetSyncHealth_ReturnsFourProviders_WithCorrectStatusWhenSeeded()
    {
        var userId = Guid.NewGuid();
        await using var sp = BuildProvider(Guid.NewGuid().ToString("N"));
        await using var scope = sp.CreateAsyncScope();
        var svc = scope.ServiceProvider;

        // Seed an active Plaid account so the tool reports "ok" for Plaid.
        var bankDb = svc.GetRequiredService<BankSyncDbContext>();
        var plaidAccount = new BankAccount(userId, "ext-plaid-001", "Chase", "checking", "9999", "Test", "USD", userId);
        plaidAccount.BeginSync();
        plaidAccount.MarkActive(10_000m);
        bankDb.BankAccounts.Add(plaidAccount);
        await bankDb.SaveChangesAsync();

        // Seed a Binance credential with a completed sync so the tool reports "ok" for Binance.
        var cryptoDb = svc.GetRequiredService<CryptoSyncDbContext>();
        var binanceCred = BinanceCredential.Create(
            userId,
            encryptedApiKey: [1, 2, 3],
            apiKeyIv: [4, 5, 6],
            apiKeyAuthTag: [7, 8, 9],
            encryptedApiSecret: [1, 2, 3],
            apiSecretIv: [4, 5, 6],
            apiSecretAuthTag: [7, 8, 9],
            keyVersion: 1);
        binanceCred.MarkSynced(DateTime.UtcNow.AddMinutes(-10));
        cryptoDb.BinanceCredentials.Add(binanceCred);
        await cryptoDb.SaveChangesAsync();

        var tool = svc.GetRequiredService<GetSyncHealthTool>();
        var result = await tool.ExecuteAsync(userId);

        // All four providers are always returned.
        result.Should().HaveCount(4);
        result.Select(e => e.Provider).Should()
            .BeEquivalentTo(["plaid", "monobank", "binance", "ibkr"]);

        // Every entry has required fields populated.
        result.Should().AllSatisfy(e =>
        {
            e.Provider.Should().NotBeNullOrEmpty();
            e.Status.Should().NotBeNullOrEmpty();
        });

        // Seeded providers report expected statuses.
        result.Single(e => e.Provider == "plaid").Status.Should().Be("ok");
        result.Single(e => e.Provider == "binance").Status.Should().Be("ok");

        // Providers with no credentials are honest about it.
        result.Single(e => e.Provider == "monobank").Status.Should().Be("never_synced");
        result.Single(e => e.Provider == "ibkr").Status.Should().Be("never_synced");
    }

    [Fact]
    public async Task GetTaxLots_ReturnsCostBasisAndPnl_WhenBrokerageHoldingSeededWithAvgCost()
    {
        var userId = Guid.NewGuid();
        await using var sp = BuildProvider(Guid.NewGuid().ToString("N"));
        await using var scope = sp.CreateAsyncScope();
        var svc = scope.ServiceProvider;

        var brokerageDb = svc.GetRequiredService<BrokerageSyncDbContext>();
        brokerageDb.BrokerageHoldings.Add(new BrokerageHolding(
            userId,
            "AAPL",
            "STK",
            10m,
            1_900m,
            "ibkr",
            averageCostUsd: 150m,
            acquiredAt: DateTime.UtcNow.AddYears(-2)));
        brokerageDb.BrokerageHoldings.Add(new BrokerageHolding(
            userId,
            "SPY",
            "STK",
            5m,
            2_500m,
            "ibkr"));
        await brokerageDb.SaveChangesAsync();

        var tool = svc.GetRequiredService<GetTaxLotsTool>();
        var result = await tool.ExecuteAsync(userId);

        result.Should().HaveCount(2);

        var aapl = result.Single(e => e.Symbol == "AAPL");
        aapl.AverageCostUsd.Should().Be(150m);
        aapl.CostBasisUsd.Should().Be(1_500m);
        aapl.UnrealizedPnlUsd.Should().Be(400m);
        aapl.IsLongTerm.Should().BeTrue();
        aapl.Provider.Should().Be("ibkr");

        var spy = result.Single(e => e.Symbol == "SPY");
        spy.AverageCostUsd.Should().BeNull();
        spy.CostBasisUsd.Should().BeNull();
        spy.UnrealizedPnlUsd.Should().BeNull();
        spy.IsLongTerm.Should().BeFalse();
    }

    [Fact]
    public async Task GetCryptoPnlDetail_ReturnsCostBasisAndUnrealizedPnl_WhenHoldingSeededWithCostBasis()
    {
        var userId = Guid.NewGuid();
        await using var sp = BuildProvider(Guid.NewGuid().ToString("N"));
        await using var scope = sp.CreateAsyncScope();
        var svc = scope.ServiceProvider;

        var cryptoDb = svc.GetRequiredService<CryptoSyncDbContext>();
        var holding = CryptoHolding.Create(userId, "BTC", freeQuantity: 0.5m, lockedQuantity: 0m, usdValue: 25_000m);
        holding.SetCostBasis(
            costBasisUsd: 18_000m,
            averageBuyPriceUsd: 36_000m,
            realizedPnlUsd: 500m,
            lastTradeAt: new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            lastTradeId: 42,
            tradeCount: 3);
        cryptoDb.CryptoHoldings.Add(holding);

        var nullCostHolding = CryptoHolding.Create(userId, "DOGE", freeQuantity: 1_000m, lockedQuantity: 0m, usdValue: 50m);
        cryptoDb.CryptoHoldings.Add(nullCostHolding);

        await cryptoDb.SaveChangesAsync();

        var tool = svc.GetRequiredService<GetCryptoPnlDetailTool>();
        var result = await tool.ExecuteAsync(userId);

        result.Should().HaveCount(2);

        var btc = result.Single(e => e.Asset == "BTC");
        btc.CostBasisUsd.Should().Be(18_000m);
        btc.UnrealizedPnlUsd.Should().Be(7_000m);
        btc.RealizedPnlUsd.Should().Be(500m);
        btc.TradeCount.Should().Be(3);
        btc.Provider.Should().Be("binance");

        var doge = result.Single(e => e.Asset == "DOGE");
        doge.CostBasisUsd.Should().BeNull();
        doge.UnrealizedPnlUsd.Should().BeNull();
        doge.TradeCount.Should().Be(0);
    }

    [Fact]
    public async Task GetNetWorthHistory_ReturnsSeededSnapshots()
    {
        var userId = Guid.NewGuid();
        await using var sp = BuildProvider(Guid.NewGuid().ToString("N"));
        await using var scope = sp.CreateAsyncScope();
        var svc = scope.ServiceProvider;

        var wealthDb = svc.GetRequiredService<WealthDbContext>();
        wealthDb.NetWorthSnapshots.AddRange(
            new NetWorthSnapshot
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SnapshotDate = new DateOnly(2024, 1, 31),
                BankingTotal = 1_000m,
                BrokerageTotal = 500m,
                CryptoTotal = 200m,
                TotalNetWorth = 1_700m,
                Currency = "USD",
                TakenAt = DateTimeOffset.UtcNow,
            },
            new NetWorthSnapshot
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SnapshotDate = new DateOnly(2024, 2, 29),
                BankingTotal = 1_100m,
                BrokerageTotal = 600m,
                CryptoTotal = 250m,
                TotalNetWorth = 1_950m,
                Currency = "USD",
                TakenAt = DateTimeOffset.UtcNow,
            });
        await wealthDb.SaveChangesAsync();

        var tool = svc.GetRequiredService<GetNetWorthHistoryTool>();
        var result = await tool.ExecuteAsync(userId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e =>
        {
            e.Currency.Should().Be("USD");
            e.TotalNetWorth.Should().BeGreaterThan(0);
        });
        result.Select(e => e.SnapshotDate).Should()
            .ContainInOrder(new DateOnly(2024, 1, 31), new DateOnly(2024, 2, 29));
    }

    [Fact]
    public async Task GetCashflowReport_AggregatesTransactionsByMonth()
    {
        var userId = Guid.NewGuid();
        await using var sp = BuildProvider(Guid.NewGuid().ToString("N"));
        await using var scope = sp.CreateAsyncScope();
        var svc = scope.ServiceProvider;

        var bankDb = svc.GetRequiredService<BankSyncDbContext>();
        var account = new BankAccount(userId, "ext-cf-001", "Chase", "checking", "1111", "Test User", "USD", userId);
        account.BeginSync();
        account.MarkActive(0m);
        bankDb.BankAccounts.Add(account);

        var jan = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var feb = new DateTime(2024, 2, 10, 12, 0, 0, DateTimeKind.Utc);
        // Plaid/Monobank store Amount as a positive magnitude; direction lives in
        // TransactionType ("credit" = inflow, "debit" = outflow).
        var salary1 = new Transaction(account.Id, userId, 2_000m, jan, "Salary", "hash-cf-001") { TransactionType = "credit" };
        var groceries = new Transaction(account.Id, userId, 300m, jan, "Groceries", "hash-cf-002") { TransactionType = "debit" };
        var salary2 = new Transaction(account.Id, userId, 2_500m, feb, "Salary", "hash-cf-003") { TransactionType = "credit" };
        var rent = new Transaction(account.Id, userId, 800m, feb, "Rent", "hash-cf-004") { TransactionType = "debit" };
        bankDb.Transactions.AddRange(salary1, groceries, salary2, rent);
        await bankDb.SaveChangesAsync();

        var tool = svc.GetRequiredService<GetCashflowReportTool>();
        var result = await tool.ExecuteAsync(
            userId,
            fromDate: new DateOnly(2024, 1, 1),
            toDate: new DateOnly(2024, 2, 29));

        result.Should().HaveCount(2);

        var janEntry = result.Single(e => e.Period == "2024-01");
        janEntry.Inflow.Should().Be(2_000m);
        janEntry.Outflow.Should().Be(300m);
        janEntry.Net.Should().Be(1_700m);

        var febEntry = result.Single(e => e.Period == "2024-02");
        febEntry.Inflow.Should().Be(2_500m);
        febEntry.Outflow.Should().Be(800m);
        febEntry.Net.Should().Be(1_700m);
    }
}
