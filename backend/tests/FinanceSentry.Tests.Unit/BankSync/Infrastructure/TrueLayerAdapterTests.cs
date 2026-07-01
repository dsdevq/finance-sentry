namespace FinanceSentry.Tests.Unit.BankSync.Infrastructure;

using FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;
using FinanceSentry.Modules.BankSync.Infrastructure.TrueLayer;
using FluentAssertions;
using Moq;
using Xunit;

public class TrueLayerAdapterTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid AccountId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private const string AccessToken = "test-access-token";
    private const string ExternalAccountId = "tl-acct-1234abcd";

    private readonly Mock<ITrueLayerClient> _clientMock = new(MockBehavior.Strict);
    private TrueLayerAdapter CreateSut() => new(_clientMock.Object, new TrueLayerCategoryMapper());

    [Fact]
    public void ProviderName_IsTrueLayer()
    {
        CreateSut().ProviderName.Should().Be("truelayer");
    }

    [Fact]
    public async Task GetAccountsAsync_FetchesAccountsAndBalances()
    {
        _clientMock
            .Setup(c => c.ListAccountsAsync(AccessToken, default))
            .ReturnsAsync(new List<TrueLayerAccountInfo>
            {
                new(
                    AccountId: ExternalAccountId,
                    DisplayName: "AIB Current Account",
                    Currency: "EUR",
                    ProviderName: "AIB",
                    AccountType: "checking",
                    Iban: "IE29 AIBK 9311 5212 3456 78",
                    AccountNumberLast4: "5678")
            });

        _clientMock
            .Setup(c => c.GetBalanceAsync(AccessToken, ExternalAccountId, default))
            .ReturnsAsync(new TrueLayerAccountBalance(Current: 1500.25m, Available: 1500.25m, Currency: "EUR"));

        var result = await CreateSut().GetAccountsAsync(AccessToken);

        result.Should().HaveCount(1);
        result[0].Currency.Should().Be("EUR");
        result[0].CurrentBalance.Should().Be(1500.25m);
        result[0].AccountNumberLast4.Should().Be("5678");
    }

    [Fact]
    public async Task GetAccountsAsync_BalanceFailureDoesNotFailWholeCall()
    {
        _clientMock
            .Setup(c => c.ListAccountsAsync(AccessToken, default))
            .ReturnsAsync(new List<TrueLayerAccountInfo>
            {
                new(ExternalAccountId, "Revolut", "GBP", "Revolut", "checking", null, "0000")
            });

        _clientMock
            .Setup(c => c.GetBalanceAsync(AccessToken, ExternalAccountId, default))
            .ThrowsAsync(new TrueLayerException("TRUELAYER_NOT_FOUND", "balance missing", 404));

        var result = await CreateSut().GetAccountsAsync(AccessToken);

        result.Should().HaveCount(1);
        result[0].CurrentBalance.Should().BeNull();
    }

    [Fact]
    public async Task SyncTransactionsAsync_NoCursor_RequestsLast90Days()
    {
        DateOnly? capturedFrom = null;
        DateOnly? capturedTo = null;
        _clientMock
            .Setup(c => c.GetTransactionsAsync(AccessToken, ExternalAccountId,
                It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), default))
            .Callback<string, string, DateOnly?, DateOnly?, CancellationToken>((_, _, from, to, _) =>
            {
                capturedFrom = from;
                capturedTo = to;
            })
            .ReturnsAsync([]);
        _clientMock
            .Setup(c => c.GetPendingTransactionsAsync(AccessToken, ExternalAccountId, default))
            .ReturnsAsync([]);

        await CreateSut().SyncTransactionsAsync(
            credential: AccessToken,
            externalAccountId: ExternalAccountId,
            accountId: AccountId,
            userId: UserId,
            since: null);

        capturedFrom.Should().NotBeNull();
        capturedTo.Should().NotBeNull();
        (capturedTo!.Value.DayNumber - capturedFrom!.Value.DayNumber).Should().Be(90);
    }

    [Fact]
    public async Task SyncTransactionsAsync_MergesBookedAndPendingWithSignedAmounts()
    {
        var booked = new TrueLayerTransaction(
            TransactionId: "tx-1",
            Timestamp: new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            Amount: -42.50m,
            Currency: "EUR",
            Description: "Tesco Galway",
            MerchantName: "Tesco",
            TransactionType: "debit",
            IsPending: false,
            Classification: ["Groceries"]);

        var pending = new TrueLayerTransaction(
            TransactionId: "tx-2",
            Timestamp: new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            Amount: 1000m,
            Currency: "EUR",
            Description: "Salary",
            MerchantName: null,
            TransactionType: "credit",
            IsPending: true,
            Classification: []);

        _clientMock
            .Setup(c => c.GetTransactionsAsync(AccessToken, ExternalAccountId, It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), default))
            .ReturnsAsync([booked]);
        _clientMock
            .Setup(c => c.GetPendingTransactionsAsync(AccessToken, ExternalAccountId, default))
            .ReturnsAsync([pending]);

        var (candidates, _) = await CreateSut().SyncTransactionsAsync(
            credential: AccessToken,
            externalAccountId: ExternalAccountId,
            accountId: AccountId,
            userId: UserId,
            since: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));

        candidates.Should().HaveCount(2);
        var debit = candidates.Single(c => c.Description == "Tesco Galway");
        debit.Amount.Should().Be(42.50m);
        debit.TransactionType.Should().Be("debit");
        debit.IsPending.Should().BeFalse();
        debit.PostedDate.Should().NotBeNull();
        debit.MerchantCategory.Should().Be("food_and_drink");

        var credit = candidates.Single(c => c.Description == "Salary");
        credit.Amount.Should().Be(1000m);
        credit.TransactionType.Should().Be("credit");
        credit.IsPending.Should().BeTrue();
        credit.PostedDate.Should().BeNull();
    }

    [Fact]
    public async Task DisconnectAsync_IsNoOp()
    {
        await CreateSut().DisconnectAsync(AccessToken);
        _clientMock.VerifyNoOtherCalls();
    }
}
