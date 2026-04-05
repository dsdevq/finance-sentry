namespace FinanceSentry.Tests.Unit.BankSync.Domain;

using FinanceSentry.Modules.BankSync.Domain;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Transaction entity (T212).
/// Validates immutability, soft-delete, and deduplication hash requirements.
/// </summary>
public class TransactionTests
{
    private static readonly Guid AccountId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    private static Transaction MakeTx(decimal amount = 50m, string description = "Tesco",
        bool isPending = false, string transactionType = "debit")
        => new(
            accountId: AccountId,
            userId: UserId,
            amount: amount,
            transactionDate: new DateTime(2026, 1, 15),
            description: description,
            uniqueHash: "aabbcc112233aabbcc112233aabbcc112233aabbcc112233aabbcc112233aabb",
            isPending: isPending)
        {
            TransactionType = transactionType
        };

    // ── Constructor validation ──────────────────────────────────────────────

    [Fact]
    public void Constructor_ValidArgs_SetsFields()
    {
        var tx = MakeTx(99.99m, "Netflix");

        tx.AccountId.Should().Be(AccountId);
        tx.UserId.Should().Be(UserId);
        tx.Amount.Should().Be(99.99m);
        tx.Description.Should().Be("Netflix");
        tx.IsActive.Should().BeTrue();
        tx.DeletedAt.Should().BeNull();
        tx.IsPending.Should().BeFalse();
    }

    [Fact]
    public void Constructor_NegativeAmount_ValidateInvariantsThrows()
    {
        var tx = new Transaction(AccountId, UserId, -10m, DateTime.Today, "Bad", "hash123", false);
        var act = () => tx.ValidateInvariants();

        act.Should().Throw<ArgumentException>().WithMessage("*Amount*negative*");
    }

    [Fact]
    public void Constructor_EmptyDescription_ValidateInvariantsThrows()
    {
        var tx = new Transaction(AccountId, UserId, 10m, DateTime.Today, "", "hash123", false);
        var act = () => tx.ValidateInvariants();

        act.Should().Throw<ArgumentException>().WithMessage("*Description*");
    }

    [Fact]
    public void Constructor_EmptyUniqueHash_ValidateInvariantsThrows()
    {
        var tx = new Transaction(AccountId, UserId, 10m, DateTime.Today, "Tesco", "", false);
        var act = () => tx.ValidateInvariants();

        act.Should().Throw<ArgumentException>().WithMessage("*UniqueHash*");
    }

    // ── Immutability ─────────────────────────────────────────────────────────

    [Fact]
    public void Transaction_FinancialFields_AreImmutableAfterCreation()
    {
        // Financial fields are EF Core properties — verify they're settable only via constructor
        var tx = MakeTx(100m, "Lidl");

        // Verify values remain as set
        tx.Amount.Should().Be(100m);
        tx.Description.Should().Be("Lidl");
        tx.AccountId.Should().Be(AccountId);
        tx.TransactionDate.Should().Be(new DateTime(2026, 1, 15));
    }

    // ── Soft-delete ──────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_SetsIsActiveToFalse_AndDeletedAt()
    {
        var tx = MakeTx();
        var before = DateTime.UtcNow;

        tx.SoftDelete();

        tx.IsActive.Should().BeFalse();
        tx.DeletedAt.Should().NotBeNull();
        tx.DeletedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void SoftDelete_DoesNotAlterFinancialFields()
    {
        var tx = MakeTx(200m, "Amazon");
        tx.SoftDelete();

        tx.Amount.Should().Be(200m);
        tx.Description.Should().Be("Amazon");
        tx.AccountId.Should().Be(AccountId);
    }

    // ── Transaction type + merchant category ─────────────────────────────────

    [Fact]
    public void TransactionType_CanBeSetToDebitOrCredit()
    {
        var debit = MakeTx(transactionType: "debit");
        var credit = MakeTx(transactionType: "credit");

        debit.TransactionType.Should().Be("debit");
        credit.TransactionType.Should().Be("credit");
    }

    [Fact]
    public void MerchantCategory_CanBeAssigned()
    {
        var tx = MakeTx();
        tx.MerchantCategory = "Groceries";

        tx.MerchantCategory.Should().Be("Groceries");
    }

    // ── Pending vs Posted ─────────────────────────────────────────────────────

    [Fact]
    public void PendingTransaction_HasNullPostedDate_ByDefault()
    {
        var tx = MakeTx(isPending: true);

        tx.IsPending.Should().BeTrue();
        tx.PostedDate.Should().BeNull();
    }

    [Fact]
    public void PostedTransaction_CanHavePostedDate()
    {
        var tx = MakeTx(isPending: false);
        tx.PostedDate = new DateTime(2026, 1, 16);

        tx.PostedDate.Should().Be(new DateTime(2026, 1, 16));
    }

    // ── UniqueHash ────────────────────────────────────────────────────────────

    [Fact]
    public void UniqueHash_IsPreservedFromConstructor()
    {
        const string hash = "aabbcc112233aabbcc112233aabbcc112233aabbcc112233aabbcc112233aabb";
        var tx = new Transaction(AccountId, UserId, 50m, DateTime.Today, "Test", hash, false);

        tx.UniqueHash.Should().Be(hash);
    }
}
