namespace FinanceSentry.Tests.Unit.BankSync.Infrastructure;

using FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;
using FluentAssertions;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for PlaidAdapter (T213).
/// IPlaidClient is mocked — no real HTTP calls.
/// </summary>
public class PlaidAdapterTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private readonly Mock<IPlaidClient> _clientMock = new(MockBehavior.Strict);
    private PlaidAdapter CreateSut() => new(_clientMock.Object, new PlaidCategoryMapper());

    // ── CreateLinkToken ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateLinkToken_ReturnsLinkToken()
    {
        _clientMock
            .Setup(c => c.CreateLinkTokenAsync(UserId.ToString(), default))
            .ReturnsAsync(new PlaidLinkTokenResponse(
                LinkToken: "link-sandbox-abc123",
                RequestId: "req_001",
                Expiration: DateTime.UtcNow.AddMinutes(30)));

        var sut = CreateSut();
        var result = await sut.CreateLinkTokenAsync(UserId);

        result.LinkToken.Should().Be("link-sandbox-abc123");
        result.ExpiresIn.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task CreateLinkToken_PlaidReturns4xx_ThrowsPlaidException()
    {
        _clientMock
            .Setup(c => c.CreateLinkTokenAsync(UserId.ToString(), default))
            .ThrowsAsync(new PlaidException(400, "INVALID_INPUT", "User ID is invalid"));

        var sut = CreateSut();
        var act = async () => await sut.CreateLinkTokenAsync(UserId);

        await act.Should().ThrowAsync<PlaidException>()
            .Where(e => e.StatusCode == 400);
    }

    // ── ExchangePublicToken ──────────────────────────────────────────────────

    [Fact]
    public async Task ExchangePublicToken_ReturnsAccessTokenAndItemId()
    {
        const string publicToken = "public-sandbox-xyz";
        _clientMock
            .Setup(c => c.ExchangePublicTokenAsync(publicToken, default))
            .ReturnsAsync(new PlaidExchangeTokenResponse(
                AccessToken: "access-sandbox-secret",
                ItemId: "item-prod-111",
                RequestId: "req_002"));

        var sut = CreateSut();
        var result = await sut.ExchangePublicTokenAsync(publicToken);

        result.AccessToken.Should().Be("access-sandbox-secret");
        result.ItemId.Should().Be("item-prod-111");
    }

    [Fact]
    public async Task ExchangePublicToken_PlaidReturns401_ThrowsPlaidException()
    {
        _clientMock
            .Setup(c => c.ExchangePublicTokenAsync(It.IsAny<string>(), default))
            .ThrowsAsync(new PlaidException(401, "INVALID_ACCESS_TOKEN", "Token revoked"));

        var sut = CreateSut();
        var act = async () => await sut.ExchangePublicTokenAsync("expired-token");

        await act.Should().ThrowAsync<PlaidException>()
            .Where(e => e.StatusCode == 401);
    }

    // ── GetAccountsWithBalance ───────────────────────────────────────────────

    [Fact]
    public async Task GetAccountsWithBalance_MapsToDomainModels()
    {
        const string accessToken = "access-sandbox-abc";
        _clientMock
            .Setup(c => c.GetAccountsAsync(accessToken, default))
            .ReturnsAsync(new PlaidAccountsResponse(
                Accounts:
                [
                    new PlaidAccount("acct_001", "My Checking", "AIB Checking",
                        "depository", "checking", "1234",
                        CurrentBalance: 1500.00m, AvailableBalance: 1400.00m,
                        CurrencyCode: "EUR")
                ],
                RequestId: "req_003"));

        var sut = CreateSut();
        var accounts = await sut.GetAccountsWithBalanceAsync(accessToken);

        accounts.Should().HaveCount(1);
        var acct = accounts[0];
        acct.PlaidAccountId.Should().Be("acct_001");
        acct.Name.Should().Be("My Checking");
        acct.AccountType.Should().Be("checking");
        acct.AccountNumberLast4.Should().Be("1234");
        acct.CurrentBalance.Should().Be(1500.00m);
        acct.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task GetAccountsWithBalance_PlaidReturns5xx_ThrowsPlaidException()
    {
        _clientMock
            .Setup(c => c.GetAccountsAsync(It.IsAny<string>(), default))
            .ThrowsAsync(new PlaidException(503, "SERVICE_UNAVAILABLE", "Plaid is down"));

        var sut = CreateSut();
        var act = async () => await sut.GetAccountsWithBalanceAsync("access-token");

        await act.Should().ThrowAsync<PlaidException>()
            .Where(e => e.StatusCode == 503 && e.IsTransient);
    }

    // ── GetTransactions ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransactions_MapsToTransactionCandidates()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string accessToken = "access-sandbox-abc";
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 1, 31);

        _clientMock
            .Setup(c => c.SyncTransactionsAsync(accessToken, null, 500, default))
            .ReturnsAsync(new PlaidSyncResponse(
                Added:
                [
                    new PlaidTransaction(
                        TransactionId: "txn_001",
                        AccountId: "plaid-acct-001",
                        Amount: 45.99m,
                        IsoCurrencyCode: "EUR",
                        Name: "Tesco Metro Dublin",
                        MerchantName: "Tesco",
                        PersonalFinanceCategory: "FOOD_AND_DRINK",
                        Date: new DateTime(2026, 1, 15),
                        AuthorizedDate: new DateTime(2026, 1, 14),
                        Pending: false)
                ],
                Modified: [],
                Removed: [],
                NextCursor: "cursor_001",
                HasMore: false,
                RequestId: "req_004"));

        var sut = CreateSut();
        var (result, _) = await sut.SyncTransactionsAsync(accessToken, accountId, userId, null);

        result.Should().HaveCount(1);
        var tx = result[0];
        tx.AccountId.Should().Be(accountId);
        tx.UserId.Should().Be(userId);
        tx.Amount.Should().Be(45.99m);
        tx.Description.Should().Be("Tesco Metro Dublin");
        tx.MerchantCategory.Should().Be("food_and_drink");
        tx.IsPending.Should().BeFalse();
    }

    // ── Error classification ─────────────────────────────────────────────────

    [Theory]
    [InlineData(400, false)]
    [InlineData(401, false)]
    [InlineData(403, false)]
    [InlineData(429, true)]
    [InlineData(500, true)]
    [InlineData(503, true)]
    public void PlaidException_IsTransient_ClassifiedCorrectly(int statusCode, bool expectedTransient)
    {
        var ex = new PlaidException(statusCode, "ERR", "msg");
        ex.IsTransient.Should().Be(expectedTransient,
            $"HTTP {statusCode} should {(expectedTransient ? "" : "not ")}be retryable");
    }
}
