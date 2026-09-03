namespace FinanceSentry.Tests.Unit.BankSync.Application;

using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for CounterpartyClassificationService (spec 044, US1 + US2).
/// </summary>
public class CounterpartyClassificationTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid AccountId = Guid.NewGuid();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Transaction MakeTx(
        decimal amount,
        string type,
        string description,
        string? merchantName = null,
        DateTime? date = null,
        bool isPending = false,
        Guid? accountId = null)
    {
        var d = date ?? new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc);
        var tx = new Transaction(accountId ?? AccountId, UserId, amount, d, description, Guid.NewGuid().ToString("N"), isPending)
        {
            TransactionType = type,
            MerchantName = merchantName,
            PostedDate = isPending ? null : d,
            IsActive = true
        };
        return tx;
    }

    private static Counterparty MakeCounterparty(string name, params (string matchType, string pattern)[] rules)
        => MakeCounterparty(name, FlowRoles.FamilySupport, rules);

    private static Counterparty MakeCounterparty(
        string name, string flowRole, params (string matchType, string pattern)[] rules)
    {
        var cp = new Counterparty { UserId = Guid.Empty, Name = name, FlowRole = flowRole };
        foreach (var (mt, pat) in rules)
            cp.Rules.Add(new CounterpartyRule { CounterpartyId = cp.Id, MatchType = mt, Pattern = pat });
        return cp;
    }

    private static BankAccount MakeAccount(string currency)
        => new(UserId, $"item_{Guid.NewGuid():N}", "Bank", "checking", "1234", "Owner", currency, UserId, "truelayer");

    private static ICounterpartyClassificationService BuildSut(params Counterparty[] counterparties)
        => BuildSutForWindow([], MakeAccount("USD"), counterparties);

    /// <summary>SUT whose repositories also serve the window path (<c>ClassifyForWindowAsync</c>).</summary>
    private static ICounterpartyClassificationService BuildSutForWindow(
        IReadOnlyList<Transaction> windowTransactions, BankAccount account, params Counterparty[] counterparties)
    {
        var repoMock = new Mock<ICounterpartyRepository>();
        repoMock.Setup(r => r.GetForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(counterparties.ToList());

        var txMock = new Mock<ITransactionRepository>();
        txMock.Setup(r => r.GetByUserIdSinceAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(windowTransactions.ToList());

        var acctMock = new Mock<IBankAccountRepository>();
        acctMock.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([account]);

        return new CounterpartyClassificationService(repoMock.Object, txMock.Object, acctMock.Object);
    }

    private static readonly Dictionary<Guid, string> UahAccount = new() { [AccountId] = "UAH" };
    private static readonly Dictionary<Guid, string> UsdAccount = new() { [AccountId] = "USD" };

    // ── T044-01: Match by description_contains ────────────────────────────────

    [Fact]
    public async Task Classify_DescriptionContains_MatchesCreditTransaction()
    {
        var cp = MakeCounterparty("Людмила Сичова", ("description_contains", "Людмила Сичова"));
        var sut = BuildSut(cp);

        var credit = MakeTx(18000m, "credit", "Від: Людмила Сичова");
        var result = await sut.ClassifyAsync(UserId, [credit], UahAccount);

        result.MatchedTransactionIds.Should().Contain(credit.Id);
        result.MonthlyFlows.Should().HaveCount(1);
        result.MonthlyFlows[0].NetIncomeUsd.Should().BeApproximately(
            CurrencyConverter.ToUsd(18000m, "UAH"), 0.01m);
        result.MonthlyFlows[0].NetExpenseUsd.Should().Be(0m);
    }

    // ── T044-02: Match by merchant_name_contains ──────────────────────────────

    [Fact]
    public async Task Classify_MerchantNameContains_MatchesDebitTransaction()
    {
        var cp = MakeCounterparty("Ліза", ("merchant_name_contains", "Єлизавета"));
        var sut = BuildSut(cp);

        var debit = MakeTx(5000m, "debit", "card transfer", merchantName: "Єлизавета Морозова");
        var result = await sut.ClassifyAsync(UserId, [debit], UsdAccount);

        result.MatchedTransactionIds.Should().Contain(debit.Id);
        result.MonthlyFlows[0].NetExpenseUsd.Should().Be(5000m);
        result.MonthlyFlows[0].NetIncomeUsd.Should().Be(0m);
    }

    // ── T044-03: No match returns empty result ────────────────────────────────

    [Fact]
    public async Task Classify_NoMatch_ReturnsEmptyResult()
    {
        var cp = MakeCounterparty("Мама", ("description_contains", "мама"));
        var sut = BuildSut(cp);

        var unrelated = MakeTx(100m, "debit", "Grocery store");
        var result = await sut.ClassifyAsync(UserId, [unrelated], UsdAccount);

        result.MatchedTransactionIds.Should().BeEmpty();
        result.MonthlyFlows.Should().BeEmpty();
    }

    // ── T044-04: Netting — credits dominate → income, zero expense ───────────

    [Fact]
    public async Task Classify_CreditsDominateMonth_NetIncomePositiveExpenseZero()
    {
        // ₴18k credit rent - ₴13k debit support = ₴5k net income
        var cp = MakeCounterparty("Мама", ("description_contains", "мама"));
        var sut = BuildSut(cp);

        var month = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var credit = MakeTx(18000m, "credit", "від мама", date: month.AddDays(1));
        var debit1 = MakeTx(8000m, "debit", "мама допомога", date: month.AddDays(10));
        var debit2 = MakeTx(5000m, "debit", "мама переказ", date: month.AddDays(20));

        var result = await sut.ClassifyAsync(UserId, [credit, debit1, debit2], UahAccount);

        result.MatchedTransactionIds.Should().BeEquivalentTo([credit.Id, debit1.Id, debit2.Id]);
        var flow = result.MonthlyFlows.Should().ContainSingle().Subject;
        var expectedIncomeUsd = CurrencyConverter.ToUsd(18000m - 13000m, "UAH");
        flow.NetIncomeUsd.Should().BeApproximately(expectedIncomeUsd, 0.01m);
        flow.NetExpenseUsd.Should().Be(0m);
    }

    // ── T044-05: Netting — debits dominate → expense, zero income ────────────

    [Fact]
    public async Task Classify_DebitsDominateMonth_NetExpensePositiveIncomeZero()
    {
        // ₴2k credit, ₴12k debits = ₴10k net expense
        var cp = MakeCounterparty("Мама", ("description_contains", "мама"));
        var sut = BuildSut(cp);

        var month = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var credit = MakeTx(2000m, "credit", "від мама", date: month.AddDays(1));
        var debit = MakeTx(12000m, "debit", "мама підтримка", date: month.AddDays(10));

        var result = await sut.ClassifyAsync(UserId, [credit, debit], UahAccount);

        var flow = result.MonthlyFlows.Should().ContainSingle().Subject;
        flow.NetExpenseUsd.Should().BeApproximately(CurrencyConverter.ToUsd(10000m, "UAH"), 0.01m);
        flow.NetIncomeUsd.Should().Be(0m);
    }

    // ── T044-06: Equal credits and debits → both zero ─────────────────────────

    [Fact]
    public async Task Classify_EqualCreditsAndDebits_BothNetToZero()
    {
        var cp = MakeCounterparty("Мама", ("description_contains", "мама"));
        var sut = BuildSut(cp);

        var month = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var credit = MakeTx(10000m, "credit", "від мама", date: month.AddDays(1));
        var debit = MakeTx(10000m, "debit", "мама повернення", date: month.AddDays(5));

        var result = await sut.ClassifyAsync(UserId, [credit, debit], UahAccount);

        var flow = result.MonthlyFlows.Should().ContainSingle().Subject;
        flow.NetIncomeUsd.Should().Be(0m);
        flow.NetExpenseUsd.Should().Be(0m);
    }

    // ── T044-07: Multi-counterparty in the same month ─────────────────────────

    [Fact]
    public async Task Classify_MultipleCounterpartiesSameMonth_EachNetsSeparately()
    {
        var mama = MakeCounterparty("Мама", ("description_contains", "мама"));
        var liza = MakeCounterparty("Ліза", ("description_contains", "Ліза"));
        var sut = BuildSut(mama, liza);

        var month = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var mamaCredit = MakeTx(5000m, "credit", "від мама", date: month.AddDays(1));
        var lizaDebit = MakeTx(3000m, "debit", "Ліза допомога", date: month.AddDays(2));

        var result = await sut.ClassifyAsync(UserId, [mamaCredit, lizaDebit], UsdAccount);

        result.MatchedTransactionIds.Should().BeEquivalentTo([mamaCredit.Id, lizaDebit.Id]);
        result.MonthlyFlows.Should().HaveCount(2);
        result.MonthlyFlows.Should().ContainSingle(f => f.CounterpartyName == "Мама" && f.NetIncomeUsd == 5000m);
        result.MonthlyFlows.Should().ContainSingle(f => f.CounterpartyName == "Ліза" && f.NetExpenseUsd == 3000m);
    }

    // ── T044-08: Multi-month window produces per-month results ────────────────

    [Fact]
    public async Task Classify_MultiMonthWindow_ProducesPerMonthRows()
    {
        var cp = MakeCounterparty("Мама", ("description_contains", "мама"));
        var sut = BuildSut(cp);

        // July: pure debit (expense). August: pure credit (income).
        var julyDebit = MakeTx(8000m, "debit", "мама липень",
            date: new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc));
        var augustCredit = MakeTx(18000m, "credit", "від мама серпень",
            date: new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc));

        var result = await sut.ClassifyAsync(UserId, [julyDebit, augustCredit], UsdAccount);

        result.MonthlyFlows.Should().HaveCount(2);
        var july = result.MonthlyFlows.Single(f => f.Month == "2026-07");
        july.NetExpenseUsd.Should().Be(8000m);
        july.NetIncomeUsd.Should().Be(0m);

        var august = result.MonthlyFlows.Single(f => f.Month == "2026-08");
        august.NetIncomeUsd.Should().Be(18000m);
        august.NetExpenseUsd.Should().Be(0m);
    }

    // ── T044-09: First-match wins when multiple counterparties could match ─────

    [Fact]
    public async Task Classify_FirstMatchWins_TransactionNotDoubleMatched()
    {
        // Both counterparties have a rule that matches "мама"
        var cp1 = MakeCounterparty("Counterparty1", ("description_contains", "мама"));
        var cp2 = MakeCounterparty("Counterparty2", ("description_contains", "мама"));
        var sut = BuildSut(cp1, cp2);

        var tx = MakeTx(1000m, "debit", "мама test");
        var result = await sut.ClassifyAsync(UserId, [tx], UsdAccount);

        // Matched exactly once
        result.MatchedTransactionIds.Should().Contain(tx.Id);
        result.MonthlyFlows.Should().HaveCount(1);
        result.MonthlyFlows[0].CounterpartyName.Should().Be("Counterparty1"); // first match
    }

    // ── T044-10: Case-insensitive matching ────────────────────────────────────

    [Fact]
    public async Task Classify_PatternMatchIsCaseInsensitive()
    {
        var cp = MakeCounterparty("Мама", ("description_contains", "МАМА"));
        var sut = BuildSut(cp);

        var tx = MakeTx(1000m, "credit", "від мама сичова");
        var result = await sut.ClassifyAsync(UserId, [tx], UsdAccount);

        result.MatchedTransactionIds.Should().Contain(tx.Id);
    }

    // ── T044-11: Empty inputs return empty result ─────────────────────────────

    [Fact]
    public async Task Classify_EmptyTransactionList_ReturnsEmpty()
    {
        var cp = MakeCounterparty("Мама", ("description_contains", "мама"));
        var sut = BuildSut(cp);

        var result = await sut.ClassifyAsync(UserId, [], UsdAccount);

        result.MatchedTransactionIds.Should().BeEmpty();
        result.MonthlyFlows.Should().BeEmpty();
    }

    // ── T044-12: Inactive transactions are ignored ────────────────────────────

    [Fact]
    public async Task Classify_InactiveTransaction_IsIgnored()
    {
        var cp = MakeCounterparty("Мама", ("description_contains", "мама"));
        var sut = BuildSut(cp);

        var tx = MakeTx(1000m, "credit", "від мама");
        tx.IsActive = false;

        var result = await sut.ClassifyAsync(UserId, [tx], UsdAccount);

        result.MatchedTransactionIds.Should().BeEmpty();
        result.MonthlyFlows.Should().BeEmpty();
    }

    // ── T044-13: Flow role rides through to the monthly flow ──────────────────

    [Fact]
    public async Task Classify_InvestmentCounterparty_CarriesInvestmentFlowRole()
    {
        var cp = MakeCounterparty("Investment routing", FlowRoles.Investment, ("merchant_name_contains", "Binance"));
        var sut = BuildSut(cp);

        var tx = MakeTx(500m, "debit", "Card payment", merchantName: "BINANCE");

        var result = await sut.ClassifyAsync(UserId, [tx], UsdAccount);

        result.MonthlyFlows.Should().ContainSingle()
              .Which.Should().BeEquivalentTo(new
              {
                  FlowRole = FlowRoles.Investment,
                  NetExpenseUsd = 500m,
                  NetIncomeUsd = 0m,
              });
    }

    // ── T044-14: Window path loads its own transactions and currencies ────────

    [Fact]
    public async Task ClassifyForWindow_LoadsTransactionsAndAccountCurrencies()
    {
        // The single entry point every consumer shares: it must resolve the account currency
        // itself, or a UAH statement would be netted as if it were dollars.
        var account = MakeAccount("UAH");
        var cp = MakeCounterparty("Мама", ("description_contains", "мама"));

        var rent = MakeTx(18000m, "credit", "від мама", accountId: account.Id);
        var sut = BuildSutForWindow([rent], account, cp);

        var result = await sut.ClassifyForWindowAsync(UserId, months: 6);

        result.MatchedTransactionIds.Should().ContainSingle();
        result.MonthlyFlows.Should().ContainSingle()
              .Which.NetIncomeUsd.Should().Be(CurrencyConverter.ToUsd(18000m, "UAH"));
    }
}
