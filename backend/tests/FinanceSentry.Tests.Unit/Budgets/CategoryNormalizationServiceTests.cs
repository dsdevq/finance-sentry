namespace FinanceSentry.Tests.Unit.Budgets;

using FinanceSentry.Modules.Budgets.Application.Services;
using FluentAssertions;
using Xunit;

public class CategoryNormalizationServiceTests
{
    private readonly CategoryNormalizationService _sut = new();

    [Theory]
    [InlineData("Food and Drink", "food_and_drink")]
    [InlineData("Restaurants", "food_and_drink")]
    [InlineData("Coffee Shop", "food_and_drink")]
    [InlineData("Groceries", "food_and_drink")]
    [InlineData("Rent", "housing")]
    [InlineData("Mortgage & Rent", "housing")]
    [InlineData("Home Improvement", "housing")]
    [InlineData("Travel", "transport")]
    [InlineData("Gas Stations", "transport")]
    [InlineData("Taxi", "transport")]
    [InlineData("Public Transportation", "transport")]
    [InlineData("Shops", "shopping")]
    [InlineData("Clothing", "shopping")]
    [InlineData("Recreation", "entertainment")]
    [InlineData("Movies and DVDs", "entertainment")]
    [InlineData("Healthcare", "health")]
    [InlineData("Pharmacies", "health")]
    [InlineData("Utilities", "utilities")]
    [InlineData("Electric", "utilities")]
    [InlineData("Airlines and Aviation Services", "travel")]
    [InlineData("Hotels and Motels", "travel")]
    public void Normalize_KnownRawCategory_ReturnsCorrectKey(string raw, string expectedKey)
    {
        _sut.Normalize(raw).Should().Be(expectedKey);
    }

    [Theory]
    [InlineData("food_and_drink", "food_and_drink")]
    [InlineData("housing", "housing")]
    [InlineData("transport", "transport")]
    [InlineData("shopping", "shopping")]
    [InlineData("entertainment", "entertainment")]
    [InlineData("health", "health")]
    [InlineData("utilities", "utilities")]
    [InlineData("travel", "travel")]
    [InlineData("other", "other")]
    public void Normalize_InternalKey_ReturnsSameKey(string key, string expectedKey)
    {
        _sut.Normalize(key).Should().Be(expectedKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unrecognised_category_xyz")]
    [InlineData("PayPal")]
    public void Normalize_NullOrUnknown_ReturnsOther(string? raw)
    {
        _sut.Normalize(raw).Should().Be("other");
    }

    [Theory]
    [InlineData("food_and_drink", "Food & Drink")]
    [InlineData("housing", "Housing")]
    [InlineData("transport", "Transport")]
    [InlineData("shopping", "Shopping")]
    [InlineData("entertainment", "Entertainment")]
    [InlineData("health", "Health & Fitness")]
    [InlineData("utilities", "Utilities")]
    [InlineData("travel", "Travel")]
    [InlineData("other", "Other")]
    public void GetLabel_ValidKey_ReturnsDisplayName(string key, string expectedLabel)
    {
        _sut.GetLabel(key).Should().Be(expectedLabel);
    }

    [Fact]
    public void GetLabel_UnknownKey_ReturnsOtherFallback()
    {
        _sut.GetLabel("nonexistent").Should().Be("Other");
    }
}
