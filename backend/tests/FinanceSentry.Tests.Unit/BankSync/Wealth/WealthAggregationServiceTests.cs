namespace FinanceSentry.Tests.Unit.BankSync.Wealth;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.Wealth.Application.Services;
using FluentAssertions;
using Moq;
using Xunit;

public class WealthAggregationServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static BankingAccountSummary MakeAccount(string provider, string currency, decimal? balance)
        => new(Guid.NewGuid(), "Test Bank", "checking", "1234", provider, currency,
               balance, balance.HasValue ? CurrencyConverter.ToUsd(balance.Value, currency) : null, "synced", null);

    private static BankingTransactionSummary MakeTx(Guid accountId, string provider, decimal amount, string type, DateTime date, bool isPending = false)
        => new(accountId, provider, type, amount, date, isPending);

    private WealthAggregationService BuildService(
        IEnumerable<BankingAccountSummary> accounts,
        IEnumerable<BankingTransactionSummary>? transactions = null)
    {
        var accountsMock = new Mock<IBankingAccountsReader>();
        accountsMock.Setup(r => r.GetAccountSummariesAsync(UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(accounts.ToList());

        var txMock = new Mock<IBankingTransactionReader>();
        txMock.Setup(r => r.GetTransactionsAsync(UserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((transactions ?? []).ToList());

        return new WealthAggregationService(accountsMock.Object, txMock.Object);
    }

    // ── GetWealthSummaryAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetWealthSummary_MixedCurrencies_CorrectUsdTotal()
    {
        var accounts = new[]
        {
            MakeAccount("plaid", "USD", 1000m),
            MakeAccount("monobank", "UAH", 100000m),
        };

        var svc = BuildService(accounts);
        var result = await svc.GetWealthSummaryAsync(UserId, null, null);

        result.TotalNetWorth.Should().Be(3400m);
        result.BaseCurrency.Should().Be("USD");
    }

    [Fact]
    public async Task GetWealthSummary_NullBalanceAccount_ExcludedFromTotal_IncludedInList()
    {
        var accounts = new[]
        {
            MakeAccount("plaid", "USD", 500m),
            new BankingAccountSummary(Guid.NewGuid(), "Test Bank", "checking", "1234", "plaid", "USD", null, null, "synced", null),
        };

        var svc = BuildService(accounts);
        var result = await svc.GetWealthSummaryAsync(UserId, null, null);

        result.TotalNetWorth.Should().Be(500m);
        result.Categories.Single().Accounts.Should().HaveCount(2);
        result.Categories.Single().Accounts.First(a => a.CurrentBalance is null).BalanceInBaseCurrency.Should().BeNull();
    }

    [Fact]
    public async Task GetWealthSummary_EmptyAccountList_ReturnsZeroTotal()
    {
        var svc = BuildService([]);
        var result = await svc.GetWealthSummaryAsync(UserId, null, null);

        result.TotalNetWorth.Should().Be(0m);
        result.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWealthSummary_CategoryFilter_ExcludesNonMatching()
    {
        var accounts = new[]
        {
            MakeAccount("plaid", "USD", 1000m),
            MakeAccount("binance", "USD", 500m),
        };

        var svc = BuildService(accounts);
        var result = await svc.GetWealthSummaryAsync(UserId, "banking", null);

        result.Categories.Should().HaveCount(1);
        result.Categories.Single().Category.Should().Be("banking");
        result.TotalNetWorth.Should().Be(1000m);
    }

    [Fact]
    public async Task GetWealthSummary_ProviderFilter_TakesPrecedenceOverCategory()
    {
        var accounts = new[]
        {
            MakeAccount("plaid", "USD", 1000m),
            MakeAccount("monobank", "USD", 500m),
        };

        var svc = BuildService(accounts);
        var result = await svc.GetWealthSummaryAsync(UserId, "banking", "monobank");

        result.TotalNetWorth.Should().Be(500m);
        result.Categories.Single().Accounts.Single().Provider.Should().Be("monobank");
    }

    [Fact]
    public async Task GetWealthSummary_UnknownProvider_ReturnsEmpty()
    {
        var accounts = new[] { MakeAccount("plaid", "USD", 1000m) };

        var svc = BuildService(accounts);
        var result = await svc.GetWealthSummaryAsync(UserId, null, "nonexistent_bank");

        result.TotalNetWorth.Should().Be(0m);
        result.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWealthSummary_InvalidCategory_Throws()
    {
        var svc = BuildService([]);
        await svc.Invoking(s => s.GetWealthSummaryAsync(UserId, "invalid_cat", null))
                 .Should().ThrowAsync<ArgumentException>();
    }

    // ── GetTransactionSummaryAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetTransactionSummary_DebitCreditSplit_Correct()
    {
        var accountId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        var txs = new[]
        {
            MakeTx(accountId, "plaid", 100m, "debit", date),
            MakeTx(accountId, "plaid", 200m, "debit", date),
            MakeTx(accountId, "plaid", 300m, "credit", date),
        };

        var svc = BuildService([MakeAccount("plaid", "USD", 1000m)], txs);
        var result = await svc.GetTransactionSummaryAsync(UserId, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), null, null);

        result.TotalDebits.Should().Be(300m);
        result.TotalCredits.Should().Be(300m);
        result.NetFlow.Should().Be(0m);
    }

    [Fact]
    public async Task GetTransactionSummary_EmptyWindow_ReturnsZeros()
    {
        var svc = BuildService([MakeAccount("plaid", "USD", 1000m)], []);
        var result = await svc.GetTransactionSummaryAsync(UserId, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), null, null);

        result.TotalDebits.Should().Be(0m);
        result.TotalCredits.Should().Be(0m);
        result.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransactionSummary_ProviderFilter_ScopesTransactions()
    {
        var date = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        var txs = new[]
        {
            MakeTx(Guid.NewGuid(), "plaid", 100m, "debit", date),
            MakeTx(Guid.NewGuid(), "monobank", 200m, "debit", date),
        };

        var svc = BuildService([MakeAccount("plaid", "USD", 1000m), MakeAccount("monobank", "UAH", 50000m)], txs);
        var result = await svc.GetTransactionSummaryAsync(UserId, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), null, "monobank");

        result.TotalDebits.Should().Be(200m);
    }

    [Fact]
    public async Task GetTransactionSummary_PendingTransactions_Excluded()
    {
        var accountId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        var txs = new[]
        {
            MakeTx(accountId, "plaid", 999m, "debit", date, isPending: true),
            MakeTx(accountId, "plaid", 50m, "debit", date, isPending: false),
        };

        var svc = BuildService([MakeAccount("plaid", "USD", 1000m)], txs);
        var result = await svc.GetTransactionSummaryAsync(UserId, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), null, null);

        result.TotalDebits.Should().Be(50m);
    }

    [Fact]
    public async Task GetTransactionSummary_CategoryGrouping_Correct()
    {
        var date = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        var txs = new[]
        {
            MakeTx(Guid.NewGuid(), "plaid", 100m, "debit", date),
            MakeTx(Guid.NewGuid(), "binance", 50m, "debit", date),
        };

        var svc = BuildService([MakeAccount("plaid", "USD", 1000m), MakeAccount("binance", "USD", 500m)], txs);
        var result = await svc.GetTransactionSummaryAsync(UserId, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), null, null);

        result.Categories.Should().HaveCount(2);
        result.Categories.First(c => c.Category == "banking").TotalDebits.Should().Be(100m);
        result.Categories.First(c => c.Category == "crypto").TotalDebits.Should().Be(50m);
    }
}
