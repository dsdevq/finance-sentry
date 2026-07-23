namespace FinanceSentry.Tests.Unit.BankSync.Application;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain;
using FluentAssertions;
using Xunit;

public class PendingReconcilerTests
{
    private static readonly Guid Account = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid User = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    private static Transaction Tx(decimal amount, string description, bool pending, string hash) =>
        new(Account, User, amount, new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc), description, hash, pending);

    [Fact]
    public void RetiresPending_WhenPostedTwinExistsAmongExisting()
    {
        var pending = Tx(15.38m, "Lidl Ireland Ltd", pending: true, "h-pending");
        var posted = Tx(15.38m, "Lidl Ireland Ltd", pending: false, "h-posted");

        var stale = PendingReconciler.SelectStalePending([pending, posted], []);

        stale.Should().ContainSingle().Which.Should().BeSameAs(pending);
    }

    [Fact]
    public void RetiresPending_WhenPostedTwinArrivesInNewBatch()
    {
        var pending = Tx(15.38m, "Lidl Ireland Ltd", pending: true, "h-pending");
        var postedNew = Tx(15.38m, "Lidl Ireland Ltd", pending: false, "h-posted-new");

        var stale = PendingReconciler.SelectStalePending([pending], [postedNew]);

        stale.Should().ContainSingle().Which.Should().BeSameAs(pending);
    }

    [Fact]
    public void KeepsPending_WhenNoPostedTwin()
    {
        var pending = Tx(15.38m, "Lidl Ireland Ltd", pending: true, "h-pending");
        var otherPosted = Tx(9.99m, "Tesco Stores", pending: false, "h-other");

        var stale = PendingReconciler.SelectStalePending([pending, otherPosted], []);

        stale.Should().BeEmpty();
    }

    [Fact]
    public void KeepsPending_WhenOnlyTwinIsAlsoPending()
    {
        var pendingA = Tx(15.38m, "Lidl Ireland Ltd", pending: true, "h-a");
        var pendingB = Tx(15.38m, "Lidl Ireland Ltd", pending: true, "h-b");

        var stale = PendingReconciler.SelectStalePending([pendingA, pendingB], []);

        stale.Should().BeEmpty();
    }

    [Fact]
    public void MatchIsCaseInsensitiveOnDescription()
    {
        var pending = Tx(20m, "AMAZON.IE", pending: true, "h-pending");
        var posted = Tx(20m, "amazon.ie", pending: false, "h-posted");

        var stale = PendingReconciler.SelectStalePending([pending, posted], []);

        stale.Should().ContainSingle();
    }

    [Fact]
    public void DoesNotRetireFreshlyInsertedPending()
    {
        // A pending row only in the new batch is current, never retired here.
        var newPending = Tx(15.38m, "Lidl Ireland Ltd", pending: true, "h-new-pending");
        var posted = Tx(15.38m, "Lidl Ireland Ltd", pending: false, "h-posted");

        var stale = PendingReconciler.SelectStalePending([], [newPending, posted]);

        stale.Should().BeEmpty();
    }
}
