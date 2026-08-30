namespace FinanceSentry.Tests.Unit.BankSync.Infrastructure;

using FinanceSentry.Core.Domain;
using FinanceSentry.Modules.BankSync.Infrastructure.Categorization;
using FluentAssertions;
using Xunit;

/// <summary>
/// Exercises the real curated merchant-keyword seed list through the same matcher the
/// resolver uses, over actual transaction descriptions observed from TrueLayer. Guards both
/// the seed data quality and the longest-first matching rule without needing a database.
/// </summary>
public class MerchantKeywordSeedDataTests
{
    private static readonly IReadOnlyList<(string Keyword, string CategoryKey)> Prepared =
        MerchantKeywordMatcher.Prepare(MerchantKeywordSeedData.Keywords);

    [Theory]
    [InlineData("Lidl Ireland Ltd", CategoryKeys.FoodAndDrink)]
    [InlineData("Tesco Stores 6913", CategoryKeys.FoodAndDrink)]
    [InlineData("Camile Thai Dublin", CategoryKeys.FoodAndDrink)]
    [InlineData("amazon.ie", CategoryKeys.GeneralMerchandise)]
    [InlineData("Openai *chatgpt Subscr", CategoryKeys.GeneralServices)]
    [InlineData("Github, Inc.", CategoryKeys.GeneralServices)]
    [InlineData("Netcup", CategoryKeys.GeneralServices)]
    [InlineData("Leap Card App", CategoryKeys.Transportation)]
    [InlineData("Fitzmaurice Chemists", CategoryKeys.Medical)]
    [InlineData("East Wall Medical Cent", CategoryKeys.Medical)]
    [InlineData("apple.com/bill", CategoryKeys.GeneralServices)]
    [InlineData("Hetzner Online Gmbh", CategoryKeys.GeneralServices)]
    [InlineData("Feel Fit Gym", CategoryKeys.Medical)]
    [InlineData("Dublinbikes Internet", CategoryKeys.Transportation)]
    [InlineData("Sq *olives Room Cafe 1", CategoryKeys.FoodAndDrink)]
    [InlineData("*MOBI TOP-UP 0857860057", CategoryKeys.RentAndUtilities)]
    [InlineData("Klarna*mstore.ie", CategoryKeys.GeneralMerchandise)]
    // TrueLayer no-MCC merchants (observed uncategorized in prod)
    [InlineData("Popeyes", CategoryKeys.FoodAndDrink)]
    [InlineData("Cineworld", CategoryKeys.Entertainment)]
    [InlineData("Frn* Hold.free-Now.com", CategoryKeys.Transportation)]
    [InlineData("veoliatransport.com", CategoryKeys.Transportation)]
    [InlineData("Dt Dublin Express", CategoryKeys.Transportation)]
    [InlineData("Lego Blanchardstown", CategoryKeys.GeneralMerchandise)]
    [InlineData("Makeup", CategoryKeys.PersonalCare)]
    [InlineData("Clubwise Software Ltd", CategoryKeys.Medical)]
    public void Resolve_KnownMerchantDescriptions_MapToExpectedCategory(string description, string expected)
    {
        MerchantKeywordMatcher.Resolve(description, Prepared).Should().Be(expected);
    }

    [Fact]
    public void Resolve_LongerKeywordWins_UberEatsIsFoodNotTransport()
    {
        MerchantKeywordMatcher.Resolve("UBER EATS Amsterdam", Prepared).Should().Be(CategoryKeys.FoodAndDrink);
        MerchantKeywordMatcher.Resolve("Uber trip Dublin", Prepared).Should().Be(CategoryKeys.Transportation);
    }

    [Fact]
    public void Resolve_LongerKeywordWins_GymbeamIsMerchandiseNotGym()
    {
        MerchantKeywordMatcher.Resolve("Gymbeam Italy S.r.l.", Prepared).Should().Be(CategoryKeys.GeneralMerchandise);
        MerchantKeywordMatcher.Resolve("Feel Fit Gym", Prepared).Should().Be(CategoryKeys.Medical);
    }

    [Fact]
    public void Resolve_LongerKeywordWins_TicketmasterViaKlarnaIsEntertainment()
    {
        MerchantKeywordMatcher.Resolve("Klarna* Ticketmaster.", Prepared).Should().Be(CategoryKeys.Entertainment);
    }

    [Theory]
    [InlineData("To Denys Sychov")]
    [InlineData("")]
    [InlineData(null)]
    public void Resolve_UnknownOrEmpty_IsUncategorized(string? description)
    {
        MerchantKeywordMatcher.Resolve(description, Prepared).Should().Be(CategoryKeys.Uncategorized);
    }

    [Fact]
    public void SeedData_AllCategoryKeysAreCanonical()
    {
        var canonical = new HashSet<string>
        {
            CategoryKeys.Income, CategoryKeys.TransferIn, CategoryKeys.TransferOut,
            CategoryKeys.LoanPayments, CategoryKeys.BankFees, CategoryKeys.Entertainment,
            CategoryKeys.FoodAndDrink, CategoryKeys.GeneralMerchandise, CategoryKeys.HomeImprovement,
            CategoryKeys.Medical, CategoryKeys.PersonalCare, CategoryKeys.GeneralServices,
            CategoryKeys.GovernmentAndNonProfit, CategoryKeys.Transportation, CategoryKeys.Travel,
            CategoryKeys.RentAndUtilities, CategoryKeys.Uncategorized,
        };

        MerchantKeywordSeedData.Keywords.Select(k => k.CategoryKey)
            .Should().OnlyContain(key => canonical.Contains(key));
    }

    [Fact]
    public void SeedData_HasNoDuplicateKeywords()
    {
        MerchantKeywordSeedData.Keywords.Select(k => k.Keyword.ToLowerInvariant())
            .Should().OnlyHaveUniqueItems();
    }
}
