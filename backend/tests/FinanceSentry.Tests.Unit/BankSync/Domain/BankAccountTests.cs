namespace FinanceSentry.Tests.Unit.BankSync.Domain;

using FinanceSentry.Modules.BankSync.Domain;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for BankAccount entity (T211).
/// Validates FR-001 (connect), FR-009 (user-scoped), and state machine transitions.
/// </summary>
public class BankAccountTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid CreatorId = UserId;

    // ── Constructor validation ──────────────────────────────────────────────

    [Fact]
    public void Constructor_ValidArgs_CreatesPendingAccount()
    {
        var account = MakeAccount();

        account.UserId.Should().Be(UserId);
        account.SyncStatus.Should().Be("pending");
        account.IsActive.Should().BeTrue();
        account.BankName.Should().Be("AIB Ireland");
        account.AccountType.Should().Be("checking");
        account.AccountNumberLast4.Should().Be("1234");
        account.Currency.Should().Be("EUR");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyPlaidItemId_ThrowsArgumentException(string plaidItemId)
    {
        var act = () => new BankAccount(UserId, plaidItemId, "Bank", "checking", "1234", "John", "EUR", CreatorId);
        act.Should().Throw<ArgumentException>().WithMessage("*ExternalAccountId*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyBankName_ThrowsArgumentException(string bankName)
    {
        var act = () => new BankAccount(UserId, "item_123", bankName, "checking", "1234", "John", "EUR", CreatorId);
        act.Should().Throw<ArgumentException>().WithMessage("*BankName*");
    }

    [Theory]
    [InlineData("123")]   // too short
    [InlineData("12345")] // too long
    [InlineData("12AB")]  // non-digit
    public void Constructor_InvalidAccountNumberLast4_ThrowsArgumentException(string last4)
    {
        var act = () => new BankAccount(UserId, "item_123", "AIB", "checking", last4, "John", "EUR", CreatorId);
        act.Should().Throw<ArgumentException>().WithMessage("*AccountNumberLast4*");
    }

    // ── State machine transitions ───────────────────────────────────────────

    [Fact]
    public void BeginSync_FromPending_TransitionsToSyncing()
    {
        var account = MakeAccount();
        account.BeginSync();

        account.SyncStatus.Should().Be("syncing");
    }

    [Fact]
    public void MarkActive_FromSyncing_TransitionsToActive()
    {
        var account = MakeAccount();
        account.BeginSync();
        account.MarkActive(balance: 1500.00m);

        account.SyncStatus.Should().Be("active");
        account.CurrentBalance.Should().Be(1500.00m);
    }

    [Fact]
    public void MarkFailed_FromSyncing_TransitionsToFailed()
    {
        var account = MakeAccount();
        account.BeginSync();
        account.MarkFailed("INSTITUTION_NOT_RESPONDING");

        account.SyncStatus.Should().Be("failed");
    }

    [Fact]
    public void MarkReauthRequired_FromAnyStatus_TransitionsToReauthRequired()
    {
        var account = MakeAccount();
        account.BeginSync();
        account.MarkActive(1000m);
        account.MarkReauthRequired();

        account.SyncStatus.Should().Be("reauth_required");
    }

    [Fact]
    public void BeginSync_WhileAlreadySyncing_ThrowsInvalidOperationException()
    {
        var account = MakeAccount();
        account.BeginSync();

        var act = () => account.BeginSync();

        act.Should().Throw<InvalidOperationException>("cannot sync an account that is already syncing");
    }

    [Fact]
    public void BeginSync_FromFailed_TransitionsToSyncing()
    {
        var account = MakeAccount();
        account.BeginSync();
        account.MarkFailed("TIMEOUT");

        account.BeginSync();

        account.SyncStatus.Should().Be("syncing");
    }

    [Fact]
    public void MarkActive_FromPending_ThrowsInvalidOperationException()
    {
        var account = MakeAccount();

        var act = () => account.MarkActive(500m);

        act.Should().Throw<InvalidOperationException>(
            "account must be syncing before it can be marked active");
    }

    // ── Equality ────────────────────────────────────────────────────────────

    [Fact]
    public void SameAccount_EqualsItself()
    {
        var account = MakeAccount();
        account.Equals(account).Should().BeTrue();
    }

    [Fact]
    public void TwoDistinctAccounts_AreNotEqual()
    {
        var a1 = MakeAccount();
        var a2 = MakeAccount();

        a1.Should().NotBe(a2, "distinct accounts have distinct auto-generated IDs");
    }

    // ── User scoping (FR-009) ───────────────────────────────────────────────

    [Fact]
    public void UserId_IsPreserved_AfterCreation()
    {
        var specificUserId = Guid.NewGuid();
        var account = new BankAccount(specificUserId, "item_xyz", "Revolut", "checking",
            "5678", "Jane Doe", "GBP", specificUserId);

        account.UserId.Should().Be(specificUserId);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static BankAccount MakeAccount()
        => new(UserId, "item_abc123", "AIB Ireland", "checking",
            "1234", "John Doe", "EUR", CreatorId);
}
