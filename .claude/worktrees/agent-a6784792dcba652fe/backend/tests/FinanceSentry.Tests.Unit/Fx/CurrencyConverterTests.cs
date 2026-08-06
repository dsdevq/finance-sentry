namespace FinanceSentry.Tests.Unit.Fx;

using FinanceSentry.Core.Utils;
using FluentAssertions;
using Xunit;

/// <summary>
/// CurrencyConverter holds process-wide state, so each test restores the
/// hardcoded fallback table on dispose to avoid leaking rates into other tests.
/// </summary>
public sealed class CurrencyConverterTests : IDisposable
{
    public void Dispose() => CurrencyConverter.UpdateRates(CurrencyConverter.FallbackRates);

    [Fact]
    public void ToUsd_UsesFallbackRates_BeforeAnyRefresh()
    {
        CurrencyConverter.ToUsd(100m, "UAH").Should().Be(2.4m);
        CurrencyConverter.ToUsd(10m, "EUR").Should().Be(10.8m);
        CurrencyConverter.ToUsd(5m, "USD").Should().Be(5m);
    }

    [Fact]
    public void ToUsd_UsesLiveRates_AfterUpdate()
    {
        CurrencyConverter.UpdateRates(new Dictionary<string, decimal>
        {
            ["UAH"] = 0.025m,
            ["EUR"] = 1.09m,
        });

        CurrencyConverter.ToUsd(100m, "UAH").Should().Be(2.5m);
        CurrencyConverter.ToUsd(10m, "EUR").Should().Be(10.9m);
    }

    [Fact]
    public void ToUsd_IsCaseInsensitiveOnCurrency()
    {
        CurrencyConverter.ToUsd(100m, "uah").Should().Be(2.4m);
    }

    [Fact]
    public void ToUsd_FallsBackTo1To1_ForUnknownCurrency()
    {
        CurrencyConverter.ToUsd(42m, "XYZ").Should().Be(42m);
    }

    [Fact]
    public void ToUsd_FallsBackTo1To1_ForBlankCurrency()
    {
        CurrencyConverter.ToUsd(42m, "").Should().Be(42m);
    }

    [Fact]
    public void UpdateRates_AlwaysForcesUsdTo1()
    {
        CurrencyConverter.UpdateRates(new Dictionary<string, decimal> {["USD"] = 0.5m});
        CurrencyConverter.ToUsd(10m, "USD").Should().Be(10m);
    }

    [Fact]
    public void UpdateRates_KeepsFallbackForCurrenciesMissingFromFeed()
    {
        // Feed carries EUR but not UAH — UAH must still resolve via the fallback.
        CurrencyConverter.UpdateRates(new Dictionary<string, decimal> {["EUR"] = 1.05m});
        CurrencyConverter.ToUsd(100m, "UAH").Should().Be(2.4m);
    }

    [Fact]
    public void UpdateRates_IgnoresNullOrEmpty()
    {
        CurrencyConverter.UpdateRates(new Dictionary<string, decimal> {["EUR"] = 1.05m});
        CurrencyConverter.UpdateRates(null);
        CurrencyConverter.UpdateRates(new Dictionary<string, decimal>());

        CurrencyConverter.ToUsd(10m, "EUR").Should().Be(10.5m);
    }

    [Fact]
    public void UpdateRates_SkipsNonPositiveRates()
    {
        CurrencyConverter.UpdateRates(new Dictionary<string, decimal>
        {
            ["EUR"] = 0m,
            ["GBP"] = -1m,
        });

        // both invalid -> fallback values remain
        CurrencyConverter.ToUsd(10m, "EUR").Should().Be(10.8m);
        CurrencyConverter.ToUsd(10m, "GBP").Should().Be(12.7m);
    }
}
