namespace FinanceSentry.Tests.Unit.BankSync.Application;

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
        var a = new BankAccount(UserId, $"item_{Guid.NewGuid():N}", "Bank", "checking", "1234", "Owner", currency, UserId);
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

    [Fact]
    public async Task GetTopCategories_ExcludesInternalTransferDebit()
    {
        var (_, accountAId) = MakeAccount();
        var (_, accountBId) = MakeAccount();
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

        var txRepoMock = new Mock<ITransactionRepository>();
        txRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(transactions);

        var sut = new MerchantCategoryStatisticsService(txRepoMock.Object, new TransferDetectionService());

        var result = await sut.GetTopCategoriesAsync(UserId, 10);

        result.Should().NotContain(c => c.Category == "Transfer");
        result.Sum(c => c.TotalSpend).Should().Be(200m);
        var food = result.First(c => c.Category == "Food");
        food.TotalSpend.Should().Be(100m);
    }
}
