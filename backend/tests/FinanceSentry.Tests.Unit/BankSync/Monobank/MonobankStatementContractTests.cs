namespace FinanceSentry.Tests.Unit.BankSync.Monobank;

using System.Net;
using FinanceSentry.Modules.BankSync.Infrastructure.Monobank;
using FinanceSentry.Tests.Unit.BankSync.Infrastructure;
using FluentAssertions;
using Xunit;

/// <summary>
/// Contract tests for Monobank GET /personal/statement/{account}/{from}/{to}
/// response mapping (T023): amount ÷ 100, numeric → alphabetic currency,
/// Unix timestamp → UTC DateTime. Mocked HTTP handler — no live Monobank calls.
/// </summary>
public class MonobankStatementContractTests
{
    private const string Token = "uTestToken123";
    private const string AccountId = "kKGVoZuHWzqVoZuH";
    private const long FromUnix = 1554000000L;
    private const long ToUnix = 1554466347L;

    private static readonly DateTimeOffset From = DateTimeOffset.FromUnixTimeSeconds(FromUnix);
    private static readonly DateTimeOffset To = DateTimeOffset.FromUnixTimeSeconds(ToUnix);

    private const string StatementBody = """
        [
          {
            "id": "ZuHWzqkKGVo=",
            "time": 1554466347,
            "description": "Покупка щастя",
            "mcc": 7997,
            "originalMcc": 7997,
            "hold": false,
            "amount": -95000,
            "operationAmount": -95000,
            "currencyCode": 980,
            "operationCurrencyCode": 980,
            "commissionRate": 0,
            "cashbackAmount": 19000,
            "balance": 10050000,
            "comment": "За каву",
            "receiptId": "XXXX-XXXX-XXXX-XXXX",
            "counterName": "ТОВ «ВОРОНА»",
            "counterIban": "UA898999980000355639201001404"
          },
          {
            "id": "creditEntry0001=",
            "time": 1554466400,
            "description": "Зарахування",
            "mcc": 4829,
            "originalMcc": 4829,
            "hold": true,
            "amount": 1000000,
            "operationAmount": 1000000,
            "currencyCode": 980,
            "operationCurrencyCode": 980,
            "commissionRate": 0,
            "cashbackAmount": 0,
            "balance": 11050000
          }
        ]
        """;

    [Fact]
    public async Task GetStatements_RequestsStatementPathWithUnixRange()
    {
        var handler = new MonobankStubHttpHandler().Enqueue(HttpStatusCode.OK, "[]");

        await handler.BuildClient().GetStatementsAsync(Token, AccountId, From, To);

        handler.RequestPaths.Should().ContainSingle()
            .Which.Should().Be($"/personal/statement/{AccountId}/{FromUnix}/{ToUnix}");
        handler.RequestTokens.Should().ContainSingle().Which.Should().Be(Token);
    }

    [Fact]
    public async Task GetStatements_ParsesRawEntryFields()
    {
        var handler = new MonobankStubHttpHandler().Enqueue(HttpStatusCode.OK, StatementBody);

        var result = await handler.BuildClient().GetStatementsAsync(Token, AccountId, From, To);

        result.Should().HaveCount(2);
        var debit = result[0];
        debit.Id.Should().Be("ZuHWzqkKGVo=");
        debit.Time.Should().Be(1554466347L);
        debit.Description.Should().Be("Покупка щастя");
        debit.MCC.Should().Be(7997);
        debit.Hold.Should().BeFalse();
        debit.Amount.Should().Be(-95000L); // raw kopecks at the client layer
        debit.CurrencyCode.Should().Be(980);
        debit.CashbackAmount.Should().Be(19000L);
        debit.Balance.Should().Be(10050000L);
        debit.Comment.Should().Be("За каву");
        debit.CounterName.Should().Be("ТОВ «ВОРОНА»");
        debit.CounterIban.Should().Be("UA898999980000355639201001404");
    }

    [Fact]
    public async Task GetStatements_EmptyBody_ReturnsEmptyList()
    {
        var handler = new MonobankStubHttpHandler().Enqueue(HttpStatusCode.OK, "[]");

        var result = await handler.BuildClient().GetStatementsAsync(Token, AccountId, From, To);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatements_Unauthorized_ThrowsTokenInvalidMappedTo400()
    {
        var handler = new MonobankStubHttpHandler()
            .Enqueue(HttpStatusCode.Unauthorized, """{"errorDescription":"Unknown 'X-Token'"}""");

        var act = () => handler.BuildClient().GetStatementsAsync("bad-token", AccountId, From, To);

        (await act.Should().ThrowAsync<MonobankException>())
            .Which.Should().Match<MonobankException>(e =>
                e.ErrorCode == "MONOBANK_TOKEN_INVALID" && e.StatusCode == 400);
    }

    // ── Statement → candidate conversions (amount ÷ 100, unix time → UTC) ────

    [Fact]
    public async Task GetCandidates_ConvertsKopecksToDecimalAndUnixTimeToUtc()
    {
        var handler = new MonobankStubHttpHandler().Enqueue(HttpStatusCode.OK, StatementBody);
        var adapter = new MonobankAdapter(handler.BuildClient(), StubCategoryResolver.Instance);

        var candidates = await adapter.GetCandidatesAsync(
            Token, AccountId, Guid.NewGuid(), Guid.NewGuid(), From, To);

        candidates.Should().HaveCount(2);

        var debit = candidates[0];
        debit.Amount.Should().Be(950.00m); // -95000 kopecks → 950.00 absolute
        debit.TransactionType.Should().Be("debit");
        debit.TransactionDate.Should().Be(new DateTime(2019, 4, 5, 12, 12, 27, DateTimeKind.Utc));
        debit.TransactionDate.Kind.Should().Be(DateTimeKind.Utc);
        debit.IsPending.Should().BeFalse();
        debit.MerchantName.Should().Be("ТОВ «ВОРОНА»");
        debit.Mcc.Should().Be(7997);

        var credit = candidates[1];
        credit.Amount.Should().Be(10000.00m); // 1000000 kopecks → 10000.00
        credit.TransactionType.Should().Be("credit");
        credit.IsPending.Should().BeTrue(); // hold = pending
    }

    // ── Currency + amount helper contracts ───────────────────────────────────

    [Theory]
    [InlineData(980, "UAH")]
    [InlineData(840, "USD")]
    [InlineData(978, "EUR")]
    [InlineData(826, "GBP")]
    [InlineData(985, "PLN")]
    public void MapCurrency_KnownNumericCodes_MapToAlphabetic(int numeric, string expected)
    {
        MonobankHttpClient.MapCurrency(numeric).Should().Be(expected);
    }

    [Fact]
    public void MapCurrency_UnknownNumericCode_ReturnsUnknownMarker()
    {
        MonobankHttpClient.MapCurrency(999).Should().Be("UNKNOWN_999");
    }

    [Theory]
    [InlineData(12345L, 123.45)]
    [InlineData(-50L, -0.50)]
    [InlineData(0L, 0)]
    [InlineData(1L, 0.01)]
    public void KopecksToDecimal_DividesBy100(long kopecks, decimal expected)
    {
        MonobankHttpClient.KopecksToDecimal(kopecks).Should().Be(expected);
    }
}
