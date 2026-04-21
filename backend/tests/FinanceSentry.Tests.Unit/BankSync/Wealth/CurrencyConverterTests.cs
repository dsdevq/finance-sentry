namespace FinanceSentry.Tests.Unit.BankSync.Wealth;

using FinanceSentry.Modules.BankSync.Application.Services;
using FluentAssertions;
using Xunit;

public class CurrencyConverterTests
{
    [Fact]
    public void ToUsd_UsdInput_ReturnsUnchanged()
    {
        CurrencyConverter.ToUsd(100m, "USD").Should().Be(100m);
    }

    [Fact]
    public void ToUsd_UahInput_ConvertsCorrectly()
    {
        CurrencyConverter.ToUsd(1000m, "UAH").Should().Be(24m);
    }

    [Fact]
    public void ToUsd_EurInput_ConvertsCorrectly()
    {
        CurrencyConverter.ToUsd(100m, "EUR").Should().Be(108m);
    }

    [Fact]
    public void ToUsd_GbpInput_ConvertsCorrectly()
    {
        CurrencyConverter.ToUsd(100m, "GBP").Should().Be(127m);
    }

    [Fact]
    public void ToUsd_UnknownCurrency_PassesThroughAtOneToOne()
    {
        CurrencyConverter.ToUsd(500m, "JPY").Should().Be(500m);
    }

    [Fact]
    public void ToUsd_CurrencyIsCaseInsensitive()
    {
        CurrencyConverter.ToUsd(100m, "usd").Should().Be(100m);
        CurrencyConverter.ToUsd(100m, "eur").Should().Be(108m);
    }
}
