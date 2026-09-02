namespace FinanceSentry.Tests.Unit.BankSync.Application;

using FinanceSentry.Core.Domain;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for MoneyFlowStatisticsService (T413).
/// All repository dependencies are mocked; no database required.
/// </summary>
public class MoneyFlowStatisticsTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a BankAccount and returns both the entity and its auto-generated Id.
    /// </summary>
    private static (BankAccount account, Guid accountId) MakeAccount(string currency)
    {
        var a = new BankAccount(UserId, $"item_{Guid.NewGuid():N}", "Bank", "checking", "1234", "Owner", currency, UserId, "truelayer");
        return (a, a.Id);
    }

    private static Transaction MakeTx(
        Guid accountId, decimal amount, string type, DateTime date, bool isPending = false,
        string? merchantName = null, string description = "desc", int? mcc = null)
    {
        var hash = Guid.NewGuid().ToString("N");
        var tx = new Transaction(accountId, UserId, amount, date, description, hash, isPending)
        {
            TransactionType = type,
            PostedDate = isPending ? null : date,
            IsActive = true,
            MerchantName = merchantName,
            Mcc = mcc
        };
        return tx;
    }

    private static Mock<ITransactionRepository> TxRepo(IReadOnlyList<Transaction> transactions)
    {
        var mock = new Mock<ITransactionRepository>();
        mock.Setup(r => r.GetByUserIdSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);
        return mock;
    }

    private static Mock<IBankAccountRepository> AccountRepo(params BankAccount[] accounts)
    {
        var mock = new Mock<IBankAccountRepository>();
        mock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);
        return mock;
    }

    private static IActiveSubscriptionsReader CommitmentsReader(params string[] merchantKeys)
    {
        var mock = new Mock<IActiveSubscriptionsReader>();
        mock.Setup(r => r.GetActiveCommitmentMerchantKeysAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchantKeys.ToHashSet(StringComparer.Ordinal));
        return mock.Object;
    }

    // ── T413 Test 1: Six months of monthly flow ───────────────────────────────

    [Fact]
    public async Task GetMonthlyFlow_SixMonths_ReturnsCorrectInOutNet()
    {
        // Arrange: one credit and one debit per month for 6 months
        var (account, accountId) = MakeAccount("EUR");
        var now = DateTime.UtcNow;
        var transactions = new List<Transaction>();

        for (int i = 0; i < 6; i++)
        {
            var month = now.AddMonths(-i);
            var monthDate = new DateTime(month.Year, month.Month, 15, 0, 0, 0, DateTimeKind.Utc);
            transactions.Add(MakeTx(accountId, 1000m, "credit", monthDate));
            transactions.Add(MakeTx(accountId, 600m, "debit", monthDate));
        }

        var txRepoMock = new Mock<ITransactionRepository>();
        txRepoMock.Setup(r => r.GetByUserIdSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(transactions);

        var accountRepoMock = new Mock<IBankAccountRepository>();
        accountRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync([account]);

        var sut = new MoneyFlowStatisticsService(
            txRepoMock.Object, accountRepoMock.Object, new TransferDetectionService(), CommitmentsReader());

        // Act
        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        // Assert: 6 months, each with 1000 inflow, 600 outflow, 400 net
        result.Should().HaveCount(6);
        result.Should().AllSatisfy(mf =>
        {
            mf.Inflow.Should().Be(1000m);
            mf.Outflow.Should().Be(600m);
            mf.Net.Should().Be(400m);
            mf.Currency.Should().Be("EUR");
        });

        // Results should be sorted by month DESC
        var months = result.Select(mf => mf.Month).ToList();
        months.Should().BeInDescendingOrder();
    }

    // ── T413 Test 2: Pending transactions counted ─────────────────────────────

    [Fact]
    public async Task GetMonthlyFlow_IncludesPendingTransactions()
    {
        // A card hold is committed money — excluding pending debits made the current
        // month's outflow a fraction of real spending (the "$688 outflow" bug).
        var (account, accountId) = MakeAccount("EUR");
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(accountId, 500m, "credit", date, isPending: false),
            MakeTx(accountId, 100m, "debit",  date, isPending: false),
            MakeTx(accountId, 40m,  "debit",  date, isPending: true)
        };

        var txRepoMock = new Mock<ITransactionRepository>();
        txRepoMock.Setup(r => r.GetByUserIdSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(transactions);

        var accountRepoMock = new Mock<IBankAccountRepository>();
        accountRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync([account]);

        var sut = new MoneyFlowStatisticsService(
            txRepoMock.Object, accountRepoMock.Object, new TransferDetectionService(), CommitmentsReader());

        // Act
        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        // Assert: the pending debit counts → outflow = 100 + 40
        result.Should().HaveCount(1);
        result[0].Inflow.Should().Be(500m);
        result[0].Outflow.Should().Be(140m);
        result[0].Net.Should().Be(360m);
    }

    // ── T413 Test 3: Multi-currency separate stats ────────────────────────────

    [Fact]
    public async Task GetMonthlyFlow_MultiCurrency_SeparateStatsPerCurrency()
    {
        // Arrange
        var (eurAccount, eurAccountId) = MakeAccount("EUR");
        var (usdAccount, usdAccountId) = MakeAccount("USD");
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(eurAccountId, 1000m, "credit", date),
            MakeTx(eurAccountId, 400m,  "debit",  date),
            MakeTx(usdAccountId, 800m,  "credit", date),
            MakeTx(usdAccountId, 300m,  "debit",  date)
        };

        var txRepoMock = new Mock<ITransactionRepository>();
        txRepoMock.Setup(r => r.GetByUserIdSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(transactions);

        var accountRepoMock = new Mock<IBankAccountRepository>();
        accountRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync([eurAccount, usdAccount]);

        var sut = new MoneyFlowStatisticsService(
            txRepoMock.Object, accountRepoMock.Object, new TransferDetectionService(), CommitmentsReader());

        // Act
        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        // Assert: 2 rows — one for EUR, one for USD (same month)
        result.Should().HaveCount(2);

        var eurFlow = result.First(mf => mf.Currency == "EUR");
        eurFlow.Inflow.Should().Be(1000m);
        eurFlow.Outflow.Should().Be(400m);
        eurFlow.Net.Should().Be(600m);

        var usdFlow = result.First(mf => mf.Currency == "USD");
        usdFlow.Inflow.Should().Be(800m);
        usdFlow.Outflow.Should().Be(300m);
        usdFlow.Net.Should().Be(500m);
    }

    // ── Transfer exclusion: internal transfer pair must not inflate inflow/outflow ─

    [Fact]
    public async Task GetMonthlyFlow_ExcludesInternalTransferPair()
    {
        var (accountA, accountAId) = MakeAccount("USD");
        var (accountB, accountBId) = MakeAccount("USD");
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var transferDebit  = MakeTx(accountAId, 500m, "debit",  date);
        var transferCredit = MakeTx(accountBId, 500m, "credit", date);
        transferDebit.Description  = "Transfer to savings";
        transferCredit.Description = "Transfer to savings";

        var transactions = new List<Transaction>
        {
            transferDebit,
            transferCredit,
            MakeTx(accountAId, 100m, "debit",  date),  // real spending — kept
            MakeTx(accountAId, 300m, "credit", date),  // real income   — kept
        };

        var txRepoMock = new Mock<ITransactionRepository>();
        txRepoMock.Setup(r => r.GetByUserIdSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(transactions);

        var accountRepoMock = new Mock<IBankAccountRepository>();
        accountRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync([accountA, accountB]);

        var sut = new MoneyFlowStatisticsService(
            txRepoMock.Object, accountRepoMock.Object, new TransferDetectionService(), CommitmentsReader());

        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        result.Should().HaveCount(1);
        result[0].Inflow.Should().Be(300m);   // transfer credit excluded
        result[0].Outflow.Should().Be(100m);  // transfer debit  excluded
        result[0].Net.Should().Be(200m);
    }

    // ── T413 Test 4: Debit/credit classification ──────────────────────────────

    [Fact]
    public async Task GetMonthlyFlow_DebitCreditClassification()
    {
        // Arrange — credit = inflow, debit = outflow
        var (account, accountId) = MakeAccount("EUR");
        var date = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(accountId, 750m, "credit", date),  // → inflow
            MakeTx(accountId, 250m, "debit",  date),  // → outflow
            MakeTx(accountId, 150m, "credit", date),  // → inflow
        };

        var txRepoMock = new Mock<ITransactionRepository>();
        txRepoMock.Setup(r => r.GetByUserIdSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(transactions);

        var accountRepoMock = new Mock<IBankAccountRepository>();
        accountRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync([account]);

        var sut = new MoneyFlowStatisticsService(
            txRepoMock.Object, accountRepoMock.Object, new TransferDetectionService(), CommitmentsReader());

        // Act
        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        // Assert
        result.Should().HaveCount(1);
        result[0].Inflow.Should().Be(900m);   // 750 + 150
        result[0].Outflow.Should().Be(250m);
        result[0].Net.Should().Be(650m);
    }

    // ── #538 Committed vs discretionary split ─────────────────────────────────

    [Fact]
    public async Task GetMonthlyFlow_SplitsOutflowByActiveCommitmentMerchantKey()
    {
        var (account, accountId) = MakeAccount("USD");
        var date = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(accountId, 15m,  "debit",  date, merchantName: "Netflix"),
            MakeTx(accountId, 85m,  "debit",  date, merchantName: "Silpo"),
            MakeTx(accountId, 500m, "credit", date, merchantName: "Employer"),
        };

        var sut = new MoneyFlowStatisticsService(
            TxRepo(transactions).Object, AccountRepo(account).Object, new TransferDetectionService(),
            CommitmentsReader("netflix"));

        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        result.Should().HaveCount(1);
        result[0].OutflowUsd.Should().Be(100m);
        result[0].CommittedOutflowUsd.Should().Be(15m);
        result[0].DiscretionaryOutflowUsd.Should().Be(85m);
    }

    [Fact]
    public async Task GetMonthlyFlow_SplitAlwaysPartitionsOutflow()
    {
        // The split may never invent or drop spend: whatever the match rule decides,
        // the two buckets must add back to OutflowUsd.
        var (account, accountId) = MakeAccount("UAH");
        var date = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(accountId, 333.33m, "debit", date, merchantName: "Netflix"),
            MakeTx(accountId, 777.77m, "debit", date, merchantName: "Silpo"),
        };

        var sut = new MoneyFlowStatisticsService(
            TxRepo(transactions).Object, AccountRepo(account).Object, new TransferDetectionService(),
            CommitmentsReader("netflix"));

        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        result[0].CommittedOutflowUsd.Should().Be(CurrencyConverter.ToUsd(333.33m, "UAH"));
        result[0].DiscretionaryOutflowUsd.Should().BeGreaterThan(0m);
        (result[0].CommittedOutflowUsd + result[0].DiscretionaryOutflowUsd)
            .Should().Be(result[0].OutflowUsd);
    }

    [Fact]
    public async Task GetMonthlyFlow_CommittedOutflow_IsConvertedPerCurrencyAtTheReaderBoundary()
    {
        // Two commitments billed in different currencies in the same month. Adding the native
        // amounts (14,000 ₴ + 100 €) produces a number that is neither hryvnia nor euro; only
        // the USD figures may be summed across rows.
        var (uahAccount, uahAccountId) = MakeAccount("UAH");
        var (eurAccount, eurAccountId) = MakeAccount("EUR");
        var date = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(uahAccountId, 14000m, "debit", date, merchantName: "Kredobank"),
            MakeTx(eurAccountId, 100m,   "debit", date, merchantName: "Netflix"),
        };

        var sut = new MoneyFlowStatisticsService(
            TxRepo(transactions).Object, AccountRepo(uahAccount, eurAccount).Object,
            new TransferDetectionService(),
            CommitmentsReader("kredobank", "netflix"));

        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        var expectedUah = CurrencyConverter.ToUsd(14000m, "UAH");
        var expectedEur = CurrencyConverter.ToUsd(100m, "EUR");

        result.Should().HaveCount(2);
        result.First(r => r.Currency == "UAH").CommittedOutflowUsd.Should().Be(expectedUah);
        result.First(r => r.Currency == "EUR").CommittedOutflowUsd.Should().Be(expectedEur);
        result.Sum(r => r.CommittedOutflowUsd).Should().Be(expectedUah + expectedEur);

        // The figure a native `.Sum(x => x.Amount)` would have produced.
        result.Sum(r => r.CommittedOutflowUsd).Should().NotBe(14100m);
    }

    [Fact]
    public async Task GetMonthlyFlow_CommittedMatch_UsesTheDetectorsMerchantKey()
    {
        // The detector stores "claude" for every Anthropic spelling. Matching on the raw
        // merchant name would miss the charge the detector itself grouped under that key.
        var (account, accountId) = MakeAccount("USD");
        var date = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(accountId, 22m, "debit", date, merchantName: "Anthropic* Claude Sub 4471"),
            // Merchant name absent — the key falls back to the description, and mobile
            // top-ups collapse to a per-number key.
            MakeTx(accountId, 10m, "debit", date, description: "*MOBI TOP-UP 0857860057"),
        };

        var sut = new MoneyFlowStatisticsService(
            TxRepo(transactions).Object, AccountRepo(account).Object, new TransferDetectionService(),
            CommitmentsReader("claude", "mobile top-up 0057"));

        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        result[0].CommittedOutflowUsd.Should().Be(32m);
        result[0].DiscretionaryOutflowUsd.Should().Be(0m);
    }

    [Fact]
    public async Task GetMonthlyFlow_TransferToACommittedMerchant_CountsInNeitherBucket()
    {
        // Transfers are already out of Outflow; the split must inherit that exclusion rather
        // than re-deriving it, or committed spend would exceed the outflow it partitions.
        var (account, accountId) = MakeAccount("USD");
        var date = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);

        var transfer = MakeTx(accountId, 700m, "debit", date, merchantName: "Netflix");
        transfer.MerchantCategory = CategoryKeys.TransferOut;

        var transactions = new List<Transaction>
        {
            transfer,
            MakeTx(accountId, 15m, "debit", date, merchantName: "Netflix"),
        };

        var sut = new MoneyFlowStatisticsService(
            TxRepo(transactions).Object, AccountRepo(account).Object, new TransferDetectionService(),
            CommitmentsReader("netflix"));

        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        result[0].OutflowUsd.Should().Be(15m);
        result[0].CommittedOutflowUsd.Should().Be(15m);
        result[0].DiscretionaryOutflowUsd.Should().Be(0m);
    }

    [Fact]
    public async Task GetMonthlyFlow_NoActiveCommitments_TreatsAllOutflowAsDiscretionary()
    {
        // Before the detector has found anything (or after everything is dismissed) the split
        // must not guess — it reports the whole outflow as discretionary.
        var (account, accountId) = MakeAccount("USD");
        var date = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(accountId, 15m, "debit", date, merchantName: "Netflix"),
        };

        var sut = new MoneyFlowStatisticsService(
            TxRepo(transactions).Object, AccountRepo(account).Object, new TransferDetectionService(),
            CommitmentsReader());

        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        result[0].CommittedOutflowUsd.Should().Be(0m);
        result[0].DiscretionaryOutflowUsd.Should().Be(15m);
    }

    [Fact]
    public async Task GetMonthlyFlow_InstallmentRepayment_CountsAsCommitted()
    {
        // A розстрочка repayment is the most committed spend there is, but the detector keys
        // its plans as installment:{merchant}:{amount} — a form no merchant key ever takes, so
        // matching on the merchant key alone booked every repayment as discretionary.
        var (account, accountId) = MakeAccount("UAH");
        var date = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(accountId, 6499.84m, "debit", date, description: "Щомісячний платіж telemart - monomarket"),
            MakeTx(accountId, 900m, "debit", date, merchantName: "Silpo"),
        };

        var sut = new MoneyFlowStatisticsService(
            TxRepo(transactions).Object, AccountRepo(account).Object, new TransferDetectionService(),
            CommitmentsReader("installment:telemart:6500"));

        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        result[0].CommittedOutflowUsd.Should().Be(CurrencyConverter.ToUsd(6499.84m, "UAH"));
        result[0].DiscretionaryOutflowUsd.Should().Be(CurrencyConverter.ToUsd(900m, "UAH"));
        (result[0].CommittedOutflowUsd + result[0].DiscretionaryOutflowUsd)
            .Should().Be(result[0].OutflowUsd);
    }

    [Fact]
    public async Task GetMonthlyFlow_InstallmentAtASecondPlanAmount_IsNotClaimedByTheFirstPlan()
    {
        // The same shop can carry concurrent розстрочки; only the plan the user actually has
        // an active row for is committed. Merchant-level matching would claim both.
        var (account, accountId) = MakeAccount("UAH");
        var date = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(accountId, 2339.95m, "debit", date, description: "Погашення наступного платежу ТОВ Алло - monomarket"),
            MakeTx(accountId, 2999.95m, "debit", date, description: "Погашення наступного платежу ТОВ Алло - monomarket"),
        };

        var sut = new MoneyFlowStatisticsService(
            TxRepo(transactions).Object, AccountRepo(account).Object, new TransferDetectionService(),
            CommitmentsReader("installment:тов алло:2340"));

        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        result[0].CommittedOutflowUsd.Should().Be(CurrencyConverter.ToUsd(2339.95m, "UAH"));
        result[0].DiscretionaryOutflowUsd.Should().Be(CurrencyConverter.ToUsd(2999.95m, "UAH"));
    }

    [Fact]
    public async Task GetMonthlyFlow_InstallmentsInTwoCurrencies_AreConvertedPerBucket()
    {
        // Installment plans are billed in the account's currency; summing ₴6,499.84 with
        // €120 natively would produce a number that is neither.
        var (uahAccount, uahAccountId) = MakeAccount("UAH");
        var (eurAccount, eurAccountId) = MakeAccount("EUR");
        var date = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(uahAccountId, 6499.84m, "debit", date, description: "Щомісячний платіж telemart - monomarket"),
            MakeTx(eurAccountId, 120m, "debit", date, description: "Платіж Pandora", mcc: 4829),
        };

        var sut = new MoneyFlowStatisticsService(
            TxRepo(transactions).Object, AccountRepo(uahAccount, eurAccount).Object,
            new TransferDetectionService(),
            CommitmentsReader("installment:telemart:6500", "installment:pandora:120"));

        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        var expectedUah = CurrencyConverter.ToUsd(6499.84m, "UAH");
        var expectedEur = CurrencyConverter.ToUsd(120m, "EUR");

        result.Should().HaveCount(2);
        result.Sum(r => r.CommittedOutflowUsd).Should().Be(expectedUah + expectedEur);
        result.Sum(r => r.DiscretionaryOutflowUsd).Should().Be(0m);

        // The figure a native `.Sum(x => x.Amount)` would have produced.
        result.Sum(r => r.CommittedOutflowUsd).Should().NotBe(6619.84m);
    }

    [Fact]
    public async Task GetMonthlyFlow_CompletedInstallmentPlan_IsDiscretionary()
    {
        // A plan the detector marked completed leaves the active key set, so its repayments
        // stop counting as committed — the same "active only" rule subscriptions follow.
        var (account, accountId) = MakeAccount("UAH");
        var date = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);

        var transactions = new List<Transaction>
        {
            MakeTx(accountId, 6499.84m, "debit", date, description: "Щомісячний платіж telemart - monomarket"),
        };

        var sut = new MoneyFlowStatisticsService(
            TxRepo(transactions).Object, AccountRepo(account).Object, new TransferDetectionService(),
            CommitmentsReader("installment:rozetkapay:2340"));

        var result = await sut.GetMonthlyFlowAsync(UserId, 6);

        result[0].CommittedOutflowUsd.Should().Be(0m);
        result[0].DiscretionaryOutflowUsd.Should().Be(result[0].OutflowUsd);
    }
}
