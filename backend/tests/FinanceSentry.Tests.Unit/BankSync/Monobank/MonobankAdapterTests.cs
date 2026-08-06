namespace FinanceSentry.Tests.Unit.BankSync.Monobank;

using System.Net;
using FinanceSentry.Modules.BankSync.Domain.Interfaces;
using FinanceSentry.Modules.BankSync.Infrastructure.Monobank;
using FinanceSentry.Tests.Unit.BankSync.Infrastructure;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for MonobankAdapter (T034): connect happy path, invalid token,
/// and SyncTransactions amount mapping. HTTP is stubbed — no live Monobank calls.
/// (The duplicate-connect 409 is owned by ConnectMonobankAccountCommandHandler
/// and is covered in ConnectMonobankContractTests.)
/// </summary>
public class MonobankAdapterTests
{
    private const string Token = "uTestToken123";
    private const string ExternalAccountId = "kKGVoZuHWzqVoZuH";
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid AccountId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    private const string ClientInfoBody = """
        {
          "clientId": "3MSaMMtczs",
          "name": "Мазепа Іван",
          "accounts": [
            {
              "id": "kKGVoZuHWzqVoZuH",
              "balance": 1234567,
              "creditLimit": 0,
              "type": "black",
              "currencyCode": 980,
              "maskedPan": ["537541******1234"]
            }
          ]
        }
        """;

    private const string StatementBody = """
        [
          {
            "id": "tx-debit-1",
            "time": 1554466347,
            "description": "Кава",
            "mcc": 5814,
            "hold": false,
            "amount": -4250,
            "currencyCode": 980,
            "operationAmount": -4250,
            "operationCurrencyCode": 980,
            "commissionRate": 0,
            "cashbackAmount": 0,
            "balance": 1230317
          },
          {
            "id": "tx-credit-1",
            "time": 1554466400,
            "description": "Поповнення",
            "mcc": 4829,
            "hold": false,
            "amount": 500000,
            "currencyCode": 980,
            "operationAmount": 500000,
            "operationCurrencyCode": 980,
            "commissionRate": 0,
            "cashbackAmount": 0,
            "balance": 1730317
          }
        ]
        """;

    private static MonobankAdapter CreateSut(MonobankStubHttpHandler handler)
        => new(handler.BuildClient(), StubCategoryResolver.Instance);

    [Fact]
    public void ProviderName_IsMonobank()
    {
        CreateSut(new MonobankStubHttpHandler()).ProviderName.Should().Be("monobank");
    }

    // ── Connect ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConnectAsync_HappyPath_ReturnsAccounts()
    {
        var handler = new MonobankStubHttpHandler().Enqueue(HttpStatusCode.OK, ClientInfoBody);

        var accounts = await CreateSut(handler).ConnectAsync(Token);

        accounts.Should().ContainSingle();
        accounts[0].Id.Should().Be(ExternalAccountId);
        accounts[0].Type.Should().Be("checking");
        accounts[0].CurrencyCode.Should().Be(980);
        handler.RequestPaths.Should().ContainSingle().Which.Should().Be("/personal/client-info");
    }

    [Fact]
    public async Task ConnectAsync_InvalidToken_ThrowsTokenInvalid()
    {
        var handler = new MonobankStubHttpHandler()
            .Enqueue(HttpStatusCode.Unauthorized, """{"errorDescription":"Unknown 'X-Token'"}""");

        var act = () => CreateSut(handler).ConnectAsync("bad-token");

        (await act.Should().ThrowAsync<MonobankException>())
            .Which.Should().Match<MonobankException>(e =>
                e.ErrorCode == "MONOBANK_TOKEN_INVALID" && e.StatusCode == 400);
    }

    // ── IBankProvider.GetAccountsAsync ───────────────────────────────────────

    [Fact]
    public async Task GetAccountsAsync_AsBankProvider_MapsBalanceCurrencyAndLast4()
    {
        var handler = new MonobankStubHttpHandler().Enqueue(HttpStatusCode.OK, ClientInfoBody);
        IBankProvider provider = CreateSut(handler);

        var accounts = await provider.GetAccountsAsync(Token, default);

        accounts.Should().ContainSingle();
        accounts[0].ExternalAccountId.Should().Be(ExternalAccountId);
        accounts[0].CurrentBalance.Should().Be(12345.67m); // 1234567 kopecks ÷ 100
        accounts[0].Currency.Should().Be("UAH");
        accounts[0].AccountNumberLast4.Should().Be("1234");
        accounts[0].OwnerName.Should().Be("Мазепа Іван");
    }

    // ── SyncTransactionsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task SyncTransactionsAsync_WithCursor_MapsAmountsSignsAndDates()
    {
        var handler = new MonobankStubHttpHandler().Enqueue(HttpStatusCode.OK, StatementBody);
        var since = DateTime.UtcNow.AddDays(-7);

        var (candidates, nextSyncFrom) = await CreateSut(handler).SyncTransactionsAsync(
            Token, ExternalAccountId, AccountId, UserId, since, default);

        nextSyncFrom.Should().NotBeNull();
        handler.RequestPaths.Should().ContainSingle(); // one incremental window
        candidates.Should().HaveCount(2);

        var debit = candidates.Single(c => c.Description == "Кава");
        debit.Amount.Should().Be(42.50m); // -4250 kopecks → 42.50 absolute
        debit.TransactionType.Should().Be("debit");
        debit.TransactionDate.Should().Be(new DateTime(2019, 4, 5, 12, 12, 27, DateTimeKind.Utc));
        debit.AccountId.Should().Be(AccountId);
        debit.UserId.Should().Be(UserId);
        debit.Mcc.Should().Be(5814);

        var credit = candidates.Single(c => c.Description == "Поповнення");
        credit.Amount.Should().Be(5000.00m); // 500000 kopecks → 5000.00
        credit.TransactionType.Should().Be("credit");
    }

    [Fact]
    public async Task SyncTransactionsAsync_NoCursor_Fetches90DaysInThreeWindows()
    {
        var handler = new MonobankStubHttpHandler().Enqueue(HttpStatusCode.OK, "[]");

        var (candidates, _) = await CreateSut(handler).SyncTransactionsAsync(
            Token, ExternalAccountId, AccountId, UserId, since: null, default);

        candidates.Should().BeEmpty();
        handler.RequestPaths.Should().HaveCount(3); // 3 × 31-day windows for the initial import
        handler.RequestPaths.Should().OnlyContain(p =>
            p.StartsWith($"/personal/statement/{ExternalAccountId}/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SyncTransactionsAsync_HoldTransaction_IsPending()
    {
        const string holdBody = """
            [
              {
                "id": "tx-hold-1",
                "time": 1554466500,
                "description": "Блокування",
                "mcc": 5411,
                "hold": true,
                "amount": -1000,
                "currencyCode": 980,
                "operationAmount": -1000,
                "operationCurrencyCode": 980,
                "commissionRate": 0,
                "cashbackAmount": 0,
                "balance": 1729317
              }
            ]
            """;
        var handler = new MonobankStubHttpHandler().Enqueue(HttpStatusCode.OK, holdBody);

        var (candidates, _) = await CreateSut(handler).SyncTransactionsAsync(
            Token, ExternalAccountId, AccountId, UserId, DateTime.UtcNow.AddDays(-1), default);

        candidates.Should().ContainSingle().Which.IsPending.Should().BeTrue();
    }
}
