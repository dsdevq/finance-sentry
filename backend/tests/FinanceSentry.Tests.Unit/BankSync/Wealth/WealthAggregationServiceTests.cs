namespace FinanceSentry.Tests.Unit.BankSync.Wealth;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

public class WealthAggregationServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static BankAccount MakeAccount(string provider, string currency, decimal? balance)
    {
        var a = new BankAccount(UserId, $"ext_{Guid.NewGuid():N}", "Test Bank", "checking", "1234", "Owner", currency, UserId, provider);
        if (balance.HasValue)
        {
            a.StartSync();
            a.MarkActive(balance.Value);
        }
        return a;
    }

    private static Transaction MakeTransaction(Guid accountId, decimal amount, string type, DateTime date, bool isPending = false)
    {
        var t = new Transaction(accountId, UserId, amount, date, "Desc", Guid.NewGuid().ToString());
        t.TransactionType = type;
        t.PostedDate = date;
        t.IsPending = isPending;
        return t;
    }

    private WealthAggregationService BuildService(
        IEnumerable<BankAccount> accounts,
        IEnumerable<Transaction>? transactions = null)
    {
        var accountMock = new Mock<IBankAccountRepository>();
        accountMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(accounts);

        var txMock = new Mock<ITransactionRepository>();
        txMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
              .ReturnsAsync(transactions ?? []);

        return new WealthAggregationService(accountMock.Object, txMock.Object);
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

        // 1000 USD + 100000 * 0.024 = 1000 + 2400 = 3400
        result.TotalNetWorth.Should().Be(3400m);
        result.BaseCurrency.Should().Be("USD");
    }

    [Fact]
    public async Task GetWealthSummary_NullBalanceAccount_ExcludedFromTotal_IncludedInList()
    {
        var accounts = new[]
        {
            MakeAccount("plaid", "USD", 500m),
            MakeAccount("plaid", "USD", null),
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
        // provider=monobank overrides even if category=banking would match both
        var result = await svc.GetWealthSummaryAsync(UserId, "banking", "monobank");

        result.TotalNetWorth.Should().Be(500m);
        result.Categories.Single().Accounts.Single().Provider.Should().Be("monobank");
    }

    [Fact]
    public async Task GetWealthSummary_UnknownProvider_ReturnsEmpty()
    {
        var accounts = new[]
        {
            MakeAccount("plaid", "USD", 1000m),
        };

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
        var account = MakeAccount("plaid", "USD", 1000m);
        // override Id via reflection isn't clean; use the real account id from creation
        var date = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);

        var accounts = new[] { account };
        var txs = new[]
        {
            MakeTransaction(account.Id, 100m, "debit", date),
            MakeTransaction(account.Id, 200m, "debit", date),
            MakeTransaction(account.Id, 300m, "credit", date),
        };

        var svc = BuildService(accounts, txs);
        var from = new DateOnly(2026, 4, 1);
        var to = new DateOnly(2026, 4, 30);
        var result = await svc.GetTransactionSummaryAsync(UserId, from, to, null, null);

        result.TotalDebits.Should().Be(300m);
        result.TotalCredits.Should().Be(300m);
        result.NetFlow.Should().Be(0m);
    }

    [Fact]
    public async Task GetTransactionSummary_EmptyWindow_ReturnsZeros()
    {
        var account = MakeAccount("plaid", "USD", 1000m);
        var txDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var tx = MakeTransaction(account.Id, 100m, "debit", txDate);

        var svc = BuildService(new[] { account }, new[] { tx });
        var from = new DateOnly(2026, 4, 1);
        var to = new DateOnly(2026, 4, 30);
        var result = await svc.GetTransactionSummaryAsync(UserId, from, to, null, null);

        result.TotalDebits.Should().Be(0m);
        result.TotalCredits.Should().Be(0m);
        result.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransactionSummary_ProviderFilter_ScopesTransactions()
    {
        var plaidAccount = MakeAccount("plaid", "USD", 1000m);
        var monoAccount = MakeAccount("monobank", "UAH", 50000m);

        var date = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        var txs = new[]
        {
            MakeTransaction(plaidAccount.Id, 100m, "debit", date),
            MakeTransaction(monoAccount.Id, 200m, "debit", date),
        };

        var svc = BuildService(new[] { plaidAccount, monoAccount }, txs);
        var from = new DateOnly(2026, 4, 1);
        var to = new DateOnly(2026, 4, 30);
        var result = await svc.GetTransactionSummaryAsync(UserId, from, to, null, "monobank");

        result.TotalDebits.Should().Be(200m);
    }

    [Fact]
    public async Task GetTransactionSummary_PendingTransactions_Excluded()
    {
        var account = MakeAccount("plaid", "USD", 1000m);
        var date = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        var pending = MakeTransaction(account.Id, 999m, "debit", date, isPending: true);
        var posted = MakeTransaction(account.Id, 50m, "debit", date, isPending: false);

        var svc = BuildService(new[] { account }, new[] { pending, posted });
        var from = new DateOnly(2026, 4, 1);
        var to = new DateOnly(2026, 4, 30);
        var result = await svc.GetTransactionSummaryAsync(UserId, from, to, null, null);

        result.TotalDebits.Should().Be(50m);
    }

    [Fact]
    public async Task GetTransactionSummary_CategoryGrouping_Correct()
    {
        var plaid = MakeAccount("plaid", "USD", 1000m);
        var binance = MakeAccount("binance", "USD", 500m);
        var date = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        var txs = new[]
        {
            MakeTransaction(plaid.Id, 100m, "debit", date),
            MakeTransaction(binance.Id, 50m, "debit", date),
        };

        var svc = BuildService(new[] { plaid, binance }, txs);
        var from = new DateOnly(2026, 4, 1);
        var to = new DateOnly(2026, 4, 30);
        var result = await svc.GetTransactionSummaryAsync(UserId, from, to, null, null);

        result.Categories.Should().HaveCount(2);
        result.Categories.First(c => c.Category == "banking").TotalDebits.Should().Be(100m);
        result.Categories.First(c => c.Category == "crypto").TotalDebits.Should().Be(50m);
    }
}
