namespace FinanceSentry.Tests.Unit.BankSync.Infrastructure;

using FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;
using FluentAssertions;
using Xunit;

public class CategoryMappingTests
{
    [Theory]
    [InlineData("FOOD_AND_DRINK", "food_and_drink")]
    [InlineData("GENERAL_MERCHANDISE", "shopping")]
    [InlineData("PERSONAL_CARE", "shopping")]
    [InlineData("ENTERTAINMENT", "entertainment")]
    [InlineData("TRAVEL", "travel")]
    [InlineData("TRANSPORTATION", "transport")]
    [InlineData("RENT_AND_UTILITIES", "utilities")]
    [InlineData("HOME_IMPROVEMENT", "housing")]
    [InlineData("MEDICAL", "health")]
    [InlineData("BANK_FEES", "other")]
    [InlineData("INCOME", "other")]
    public void PlaidCategoryMapper_MapsCommonPrimaryCategories(string rawCategory, string expected)
    {
        new PlaidCategoryMapper().Map(rawCategory).Should().Be(expected);
    }

    [Theory]
    [InlineData("5411", "food_and_drink")]
    [InlineData("5812", "food_and_drink")]
    [InlineData("4111", "transport")]
    [InlineData("5541", "transport")]
    [InlineData("5311", "shopping")]
    [InlineData("5941", "shopping")]
    [InlineData("7911", "entertainment")]
    [InlineData("5912", "health")]
    [InlineData("4812", "utilities")]
    [InlineData("7011", "travel")]
    [InlineData("7623", "housing")]
    public void MonobankCategoryMapper_MapsCommonMccCodes(string rawCategory, string expected)
    {
        new MonobankCategoryMapper().Map(rawCategory).Should().Be(expected);
    }

    [Theory]
    [InlineData("Groceries", "food_and_drink")]
    [InlineData("Restaurants", "food_and_drink")]
    [InlineData("Shopping", "shopping")]
    [InlineData("Personal Care", "shopping")]
    [InlineData("Healthcare", "health")]
    [InlineData("Bills & Utilities", "utilities")]
    [InlineData("Auto & Transport", "transport")]
    [InlineData("Travel", "travel")]
    [InlineData("Home Improvement", "housing")]
    [InlineData("Entertainment", "entertainment")]
    public void TrueLayerCategoryMapper_MapsCommonClassificationLabels(string rawCategory, string expected)
    {
        new TrueLayerCategoryMapper().Map([rawCategory]).Should().Be(expected);
    }
}
