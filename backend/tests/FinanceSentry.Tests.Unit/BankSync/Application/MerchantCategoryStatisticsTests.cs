namespace FinanceSentry.Tests.Unit.BankSync.Application;

using FinanceSentry.Core.Domain;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

public class MerchantCategoryStatisticsTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static (BankAccount account, Guid accountId) MakeAccount(string currency = "USD")
    {
        var a = new BankAccount(UserId, $"item_{Guid.NewGuid():N}", "Bank", "checking", "1234", "Owner", currency, UserId, "truelayer");
        return (a, a.Id);
    }

    private static Transaction MakeTx(
        Guid accountId, decimal amount, string type, DateTime date, string? category = null, string description = "desc")
    {
        var hash = Guid.NewGuid().ToString("N");
        var tx = new Transaction(accountId, UserId, amount, date, description, hash, isPending: false)
        {
            TransactionType = type,
            PostedDate = date,
            IsActive = true,
            MerchantCategory = category,
        };
        return tx;
    }

    private static MerchantCategoryStatisticsService BuildSut(
        IEnumerable<Transaction> transactions, IEnumerable<BankAccount> accounts)
    {
        var txRepoMock = new Mock<ITransactionRepository>();
        txRepoMock.Setup(r => r.GetByUserIdSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(transactions.ToList());

        var acctRepoMock = new Mock<IBankAccountRepository>();
        acctRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(accounts.ToList());

        return new MerchantCategoryStatisticsService(txRepoMock.Object, acctRepoMock.Object, new TransferDetectionService());
    }

    [Fact]
    public async Task GetTopCategories_ExcludesInternalTransferDebit()
    {
        var (accountA, accountAId) = MakeAccount();
        var (accountB, accountBId) = MakeAccount();
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        // Internal transfer pair carrying a category. Must NOT appear in spending.
        var transferDebit  = MakeTx(accountAId, 500m, "debit",  date, category: "Transfer", description: "Transfer to savings");
        var transferCredit = MakeTx(accountBId, 500m, "credit", date, category: "Transfer", description: "Transfer to savings");

        var transactions = new List<Transaction>
        {
            transferDebit,
            transferCredit,
            MakeTx(accountAId, 40m,  "debit", date, category: "Food"),
            MakeTx(accountAId, 60m,  "debit", date, category: "Food"),
            MakeTx(accountAId, 100m, "debit", date, category: "Travel"),
        };

        var sut = BuildSut(transactions, [accountA, accountB]);

        var result = await sut.GetTopCategoriesAsync(UserId, 10);

        result.Should().NotContain(c => c.Category == "Transfer");
        result.Sum(c => c.TotalSpend).Should().Be(200m);
        var food = result.First(c => c.Category == "Food");
        food.TotalSpend.Should().Be(100m);
    }

    [Fact]
    public async Task GetTopCategories_ExcludesSingleSidedTransferCategory()
    {
        // A savings-jar top-up categorized TRANSFER_OUT has no synced counterpart, so the
        // pair-matcher can't catch it. It must still be excluded from spend by category name.
        var (account, accountId) = MakeAccount();
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(accountId, 9000m, "debit", date, category: CategoryKeys.TransferOut, description: "Поповнення «Jar»"),
            MakeTx(accountId, 40m,   "debit", date, category: "Food"),
        };

        var sut = BuildSut(transactions, [account]);

        var result = await sut.GetTopCategoriesAsync(UserId, 10);

        result.Should().NotContain(c => c.Category == CategoryKeys.TransferOut);
        result.Sum(c => c.TotalSpend).Should().Be(40m);
    }

    [Fact]
    public async Task GetTopCategories_MixedCurrencies_SumsSpendInUsd()
    {
        // Spend must be converted by account currency before summing. A ₴10,000 Food debit on a
        // UAH account is $240, not $10,000 — otherwise it would dwarf a real $100 USD Food spend.
        var (usd, usdId) = MakeAccount("USD");
        var (uah, uahId) = MakeAccount("UAH");
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(usdId, 100m,   "debit", date, category: "Food"),
            MakeTx(uahId, 10000m, "debit", date, category: "Food"),
        };

        var sut = BuildSut(transactions, [usd, uah]);

        var result = await sut.GetTopCategoriesAsync(UserId, 10);

        // 100 + (10000 × 0.024) = 340, not 10100
        result.Single(c => c.Category == "Food").TotalSpend.Should().Be(340m);
    }

    [Fact]
    public async Task GetTopCategories_QueriesOnlyTheRequestedMonthsWindow()
    {
        // The breakdown must be windowed, not all-time — the repository has to be asked
        // for transactions since N months ago, not for the user's full history. That
        // boundary is the FIRST OF THE MONTH: a mid-month start leaves the oldest bucket
        // holding a few days of transactions, which the charts render as a collapsed bar.
        var (account, accountId) = MakeAccount();
        var transactions = new List<Transaction>
        {
            MakeTx(accountId, 40m, "debit", DateTime.UtcNow.AddDays(-10), category: "Food"),
        };

        var txRepoMock = new Mock<ITransactionRepository>();
        DateTime? capturedSince = null;
        txRepoMock.Setup(r => r.GetByUserIdSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                  .Callback<Guid, DateTime, CancellationToken>((_, since, _) => capturedSince = since)
                  .ReturnsAsync(transactions);

        var acctRepoMock = new Mock<IBankAccountRepository>();
        acctRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync([account]);

        var sut = new MerchantCategoryStatisticsService(
            txRepoMock.Object, acctRepoMock.Object, new TransferDetectionService());

        await sut.GetTopCategoriesAsync(UserId, limit: 10, months: 3);

        capturedSince.Should().NotBeNull();
        capturedSince!.Value.Should().Be(MonthWindow.StartOfMonthsAgo(3));
        capturedSince!.Value.Day.Should().Be(1, "a window must start on a whole month");
    }
}
