namespace FinanceSentry.Tests.Unit.Budgets;

using FinanceSentry.Modules.Budgets.Domain;
using FinanceSentry.Modules.Budgets.Domain.Exceptions;
using FluentAssertions;
using Xunit;

public class BudgetTests
{
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    // ── Budget.Create — happy path ──────────────────────────────────────────

    [Fact]
    public void Create_ValidArgs_ReturnsBudgetWithCorrectProperties()
    {
        var before = DateTimeOffset.UtcNow;
        var budget = Budget.Create(UserId, "FOOD_AND_DRINK", 500m, "USD");
        var after = DateTimeOffset.UtcNow;

        budget.UserId.Should().Be(UserId);
        budget.Category.Should().Be("FOOD_AND_DRINK");
        budget.MonthlyLimit.Should().Be(500m);
        budget.Currency.Should().Be("USD");
        budget.Id.Should().NotBeEmpty();
        budget.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        budget.UpdatedAt.Should().BeCloseTo(budget.CreatedAt, TimeSpan.FromMilliseconds(100));
    }

    // ── Budget.Create — category guard ─────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyCategory_ThrowsArgumentException(string category)
    {
        var act = () => Budget.Create(UserId, category, 100m, "USD");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*category*");
    }

    // ── Budget.Create — currency guard ─────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyCurrency_ThrowsArgumentException(string currency)
    {
        var act = () => Budget.Create(UserId, "FOOD_AND_DRINK", 100m, currency);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*currency*");
    }

    // ── Budget.Create — limit guard ────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-1000)]
    public void Create_NonPositiveLimit_ThrowsBudgetInvalidLimitException(decimal limit)
    {
        var act = () => Budget.Create(UserId, "FOOD_AND_DRINK", limit, "USD");

        act.Should().Throw<BudgetInvalidLimitException>();
    }

    [Fact]
    public void Create_SmallestPositiveLimit_Succeeds()
    {
        var act = () => Budget.Create(UserId, "FOOD_AND_DRINK", 0.01m, "USD");

        act.Should().NotThrow();
    }

    // ── Budget.UpdateLimit — happy path ────────────────────────────────────

    [Fact]
    public void UpdateLimit_ValidPositiveLimit_UpdatesMonthlyLimitAndUpdatedAt()
    {
        var budget = Budget.Create(UserId, "FOOD_AND_DRINK", 100m, "USD");
        var createdAt = budget.UpdatedAt;

        budget.UpdateLimit(250m);

        budget.MonthlyLimit.Should().Be(250m);
        budget.UpdatedAt.Should().BeOnOrAfter(createdAt);
    }

    // ── Budget.UpdateLimit — limit guard ───────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-500)]
    public void UpdateLimit_NonPositiveLimit_ThrowsBudgetInvalidLimitException(decimal newLimit)
    {
        var budget = Budget.Create(UserId, "FOOD_AND_DRINK", 100m, "USD");

        var act = () => budget.UpdateLimit(newLimit);

        act.Should().Throw<BudgetInvalidLimitException>();
    }

    [Fact]
    public void UpdateLimit_NonPositiveLimit_DoesNotMutateMonthlyLimit()
    {
        var budget = Budget.Create(UserId, "FOOD_AND_DRINK", 100m, "USD");

        try { budget.UpdateLimit(0); } catch (BudgetInvalidLimitException) { }

        budget.MonthlyLimit.Should().Be(100m);
    }
}
