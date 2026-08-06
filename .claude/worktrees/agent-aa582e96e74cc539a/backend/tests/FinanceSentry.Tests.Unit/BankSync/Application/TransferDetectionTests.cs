namespace FinanceSentry.Tests.Unit.BankSync.Application;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain;
using FluentAssertions;
using Xunit;

public class TransferDetectionTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static Transaction MakeTx(
        Guid accountId, decimal amount, string type, DateTime date,
        string description = "Transfer to savings", bool isPending = false, bool isActive = true)
    {
        var hash = Guid.NewGuid().ToString("N");
        var tx = new Transaction(accountId, UserId, amount, date, description, hash, isPending)
        {
            TransactionType = type,
            PostedDate = isPending ? null : date,
            IsActive = isActive,
        };
        return tx;
    }

    [Fact]
    public void DetectTransferTransactionIds_EmptyInput_ReturnsEmptySet()
    {
        var sut = new TransferDetectionService();

        var result = sut.DetectTransferTransactionIds([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void DetectTransferTransactionIds_PairMatchedByAmountAndDescription_ReturnsBothIds()
    {
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var debit  = MakeTx(accountA, 500m, "debit",  date);
        var credit = MakeTx(accountB, 500m, "credit", date);

        var sut = new TransferDetectionService();

        var result = sut.DetectTransferTransactionIds([debit, credit]);

        result.Should().BeEquivalentTo(new[] { debit.Id, credit.Id });
    }

    [Fact]
    public void DetectTransferTransactionIds_UnrelatedDebitAndCredit_NotMatched()
    {
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        // Different amounts + different descriptions → not a transfer
        var debit  = MakeTx(accountA, 500m, "debit",  date, description: "Grocery Store");
        var credit = MakeTx(accountB, 500m, "credit", date, description: "Employer Payroll");

        var sut = new TransferDetectionService();

        var result = sut.DetectTransferTransactionIds([debit, credit]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void DetectTransferTransactionIds_AmbiguousCredit_ConsumedByFirstDebitOnly()
    {
        // Two debits both match the same credit. The credit must be consumed once, so
        // the second debit stays unmatched (and is counted as real spending).
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var debit1 = MakeTx(accountA, 500m, "debit",  date);
        var debit2 = MakeTx(accountA, 500m, "debit",  date);
        var credit = MakeTx(accountB, 500m, "credit", date);

        var sut = new TransferDetectionService();

        var result = sut.DetectTransferTransactionIds([debit1, debit2, credit]);

        result.Should().HaveCount(2);
        result.Should().Contain(credit.Id);
        (result.Contains(debit1.Id) ^ result.Contains(debit2.Id)).Should().BeTrue();
    }

    [Fact]
    public void DetectTransferTransactionIds_PendingAndInactive_Ignored()
    {
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var debit  = MakeTx(accountA, 500m, "debit",  date, isPending: true);
        var credit = MakeTx(accountB, 500m, "credit", date, isActive: false);

        var sut = new TransferDetectionService();

        var result = sut.DetectTransferTransactionIds([debit, credit]);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Debit",  "Credit")]
    [InlineData("DEBIT",  "CREDIT")]
    [InlineData("debit",  "CREDIT")]
    [InlineData("DEBIT",  "credit")]
    [InlineData(" debit", " credit")]   // leading whitespace should be trimmed
    [InlineData("debit ", "credit ")]   // trailing whitespace
    public void DetectTransferTransactionIds_MixedCaseAndWhitespaceTypes_AreMatchedCorrectly(
        string debitType, string creditType)
    {
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var debit  = MakeTx(accountA, 500m, debitType,  date);
        var credit = MakeTx(accountB, 500m, creditType, date);

        var sut = new TransferDetectionService();

        var result = sut.DetectTransferTransactionIds([debit, credit]);

        result.Should().BeEquivalentTo(new[] { debit.Id, credit.Id },
            because: $"debit type '{debitType}' and credit type '{creditType}' should be bucketed case-insensitively");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unknown")]
    [InlineData("transfer")]
    public void DetectTransferTransactionIds_NonDebitCreditType_NotBucketed(string? txType)
    {
        // Rows with a type that is neither "debit" nor "credit" (regardless of case)
        // must not end up in either bucket and therefore never produce a matched pair.
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        // Two rows with the same non-debit/credit type — no valid debit+credit pair exists.
        var tx1 = MakeTx(accountA, 500m, txType!, date);
        var tx2 = MakeTx(accountB, 500m, txType!, date);

        var sut = new TransferDetectionService();

        var result = sut.DetectTransferTransactionIds([tx1, tx2]);

        result.Should().BeEmpty(because: $"type '{txType}' is not a debit or credit bucket");
    }
}
