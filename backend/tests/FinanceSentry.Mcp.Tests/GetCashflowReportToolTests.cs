using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Tools;
using FinanceSentry.Modules.BankSync.Application.Queries;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FinanceSentry.Mcp.Tests;

public sealed class GetCashflowReportToolTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IQueryHandler<GetAllTransactionsQuery, AllTransactionsResult>> _handler = new();

    private GetCashflowReportTool CreateSut() =>
        new(_handler.Object, new FakeIdentityResolver(), NullLogger<GetCashflowReportTool>.Instance);

    private static GlobalTransactionDto Txn(decimal amount, DateTime date, string transactionType = "debit") =>
        new(Guid.NewGuid(), Guid.NewGuid(), "TestBank", "USD", amount, date, date, "Test", transactionType, null, false, DateTime.UtcNow);

    private static GlobalTransactionDto Credit(decimal amount, DateTime date) => Txn(amount, date, "credit");
    private static GlobalTransactionDto Debit(decimal amount, DateTime date) => Txn(amount, date, "debit");

    private static AllTransactionsResult ResultOf(params GlobalTransactionDto[] txns) =>
        new([.. txns], txns.Length, false, 0, txns.Length);

    [Fact]
    public void ToolName_Returns_get_cashflow_report()
    {
        CreateSut().ToolName.Should().Be("get_cashflow_report");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmpty_WhenHandlerThrows()
    {
        _handler
            .Setup(h => h.Handle(It.IsAny<GetAllTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db unavailable"));

        var result = await CreateSut().ExecuteAsync(UserId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmpty_WhenNoTransactions()
    {
        _handler
            .Setup(h => h.Handle(It.IsAny<GetAllTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultOf());

        var result = await CreateSut().ExecuteAsync(UserId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmpty_WhenFromIsAfterTo()
    {
        var result = await CreateSut().ExecuteAsync(
            UserId,
            fromDate: new DateOnly(2024, 6, 1),
            toDate: new DateOnly(2024, 1, 1));

        result.Should().BeEmpty();
        _handler.Verify(
            h => h.Handle(It.IsAny<GetAllTransactionsQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_GroupsByMonth_AndSplitsInflowOutflow()
    {
        _handler
            .Setup(h => h.Handle(It.IsAny<GetAllTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultOf(
                Credit(2000m, new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
                Debit(500m, new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc)),
                Debit(200m, new DateTime(2024, 1, 20, 0, 0, 0, DateTimeKind.Utc)),
                Credit(3000m, new DateTime(2024, 2, 5, 0, 0, 0, DateTimeKind.Utc)),
                Debit(1000m, new DateTime(2024, 2, 10, 0, 0, 0, DateTimeKind.Utc))));

        var result = await CreateSut().ExecuteAsync(
            UserId,
            fromDate: new DateOnly(2024, 1, 1),
            toDate: new DateOnly(2024, 2, 29));

        result.Should().HaveCount(2);

        var jan = result.Single(e => e.Period == "2024-01");
        jan.Inflow.Should().Be(2000m);
        jan.Outflow.Should().Be(700m);
        jan.Net.Should().Be(1300m);
        jan.TransactionCount.Should().Be(3);

        var feb = result.Single(e => e.Period == "2024-02");
        feb.Inflow.Should().Be(3000m);
        feb.Outflow.Should().Be(1000m);
        feb.Net.Should().Be(2000m);
        feb.TransactionCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_OrdersResults_Chronologically()
    {
        _handler
            .Setup(h => h.Handle(It.IsAny<GetAllTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultOf(
                Txn(100m, new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc)),
                Txn(100m, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                Txn(100m, new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc))));

        var result = await CreateSut().ExecuteAsync(
            UserId,
            fromDate: new DateOnly(2024, 1, 1),
            toDate: new DateOnly(2024, 3, 31));

        result.Select(e => e.Period).Should().ContainInOrder("2024-01", "2024-02", "2024-03");
    }

    [Fact]
    public async Task ExecuteAsync_DefaultsToLastSixMonths_WhenNoDatesProvided()
    {
        GetAllTransactionsQuery? captured = null;
        _handler
            .Setup(h => h.Handle(It.IsAny<GetAllTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetAllTransactionsQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(ResultOf());

        await CreateSut().ExecuteAsync(UserId);

        captured.Should().NotBeNull();
        captured!.From.Should().NotBeNull();
        captured.To.Should().NotBeNull();
        var spanDays = (captured.To!.Value - captured.From!.Value).TotalDays;
        spanDays.Should().BeApproximately(183, 5);
    }
}
