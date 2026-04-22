namespace FinanceSentry.Tests.Unit.BankSync.Wealth;

using FinanceSentry.Modules.BankSync.Application.Services;
using FluentAssertions;
using Xunit;

public class ProviderCategoryMapperTests
{
    [Theory]
    [InlineData("plaid", "banking")]
    [InlineData("PLAID", "banking")]
    [InlineData("monobank", "banking")]
    [InlineData("MONOBANK", "banking")]
    [InlineData("binance", "crypto")]
    [InlineData("ibkr", "brokerage")]
    public void Map_KnownProvider_ReturnsCorrectCategory(string provider, string expected)
    {
        ProviderCategoryMapper.Map(provider).Should().Be(expected);
    }

    [Fact]
    public void Map_NullProvider_ReturnsOther()
    {
        ProviderCategoryMapper.Map(null).Should().Be("other");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Map_EmptyOrWhitespace_ReturnsOther(string provider)
    {
        ProviderCategoryMapper.Map(provider).Should().Be("other");
    }

    [Fact]
    public void Map_UnknownProvider_ReturnsOther()
    {
        ProviderCategoryMapper.Map("somefutureprovider").Should().Be("other");
    }
}
