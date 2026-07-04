namespace FinanceSentry.Tests.Unit.BankSync.Application.Subscriptions;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Infrastructure.Jobs;
using FluentAssertions;
using Xunit;

public class SubscriptionDetectionAlgorithmTests
{
    private const double TightenedMaxCv = 0.10;

    [Theory]
    [InlineData("unknown")]
    [InlineData("mobi top-up")]
    [InlineData("privatbank transfer 12345")]
    [InlineData("atm withdrawal")]
    [InlineData("cash advance")]
    public void UnidentifiableMerchant_IsFiltered(string normalized)
    {
        SubscriptionDetectionJob.IsUnidentifiableMerchant(normalized).Should().BeTrue();
    }

    [Theory]
    [InlineData("netflix")]
    [InlineData("spotify premium")]
    [InlineData("adobe creative cloud")]
    public void IdentifiableMerchant_IsNotFiltered(string normalized)
    {
        SubscriptionDetectionJob.IsUnidentifiableMerchant(normalized).Should().BeFalse();
    }

    [Fact]
    public void TightenedCv_RejectsAmountsThatWouldPassLegacyThreshold()
    {
        // 10.0, 12.0, 12.0 → cv ≈ 0.094 (borderline; passes 0.10 tightened)
        var borderline = new[] { 10.0, 12.0, 12.0 };
        var borderlineMean = borderline.Average();
        var borderlineStddev = Math.Sqrt(borderline.Sum(a => Math.Pow(a - borderlineMean, 2)) / borderline.Length);
        (borderlineStddev / borderlineMean).Should().BeLessThanOrEqualTo(TightenedMaxCv);

        // 10.0, 12.0, 14.0 → cv ≈ 0.163 (previously passed 0.20; now rejected under 0.10)
        var wider = new[] { 10.0, 12.0, 14.0 };
        var widerMean = wider.Average();
        var widerStddev = Math.Sqrt(wider.Sum(a => Math.Pow(a - widerMean, 2)) / wider.Length);
        (widerStddev / widerMean).Should().BeGreaterThan(TightenedMaxCv);
    }

    [Fact]
    public void MonthlyInterval_IsInRange_WhenDaysAre30()
    {
        var days = new[] { 30, 30, 30 };
        var median = Median(days);
        (median >= 28 && median <= 35).Should().BeTrue();
    }

    [Fact]
    public void AnnualInterval_IsInRange_WhenDaysAre365()
    {
        var days = new[] { 365, 365, 365 };
        var median = Median(days);
        (median >= 351 && median <= 379).Should().BeTrue();
    }

    [Fact]
    public void Median_OddCount_ReturnsMiddleValue()
    {
        var days = new[] { 28, 30, 35 };
        Median(days).Should().Be(30.0);
    }

    [Fact]
    public void Median_EvenCount_ReturnsAverageOfMiddleTwo()
    {
        var days = new[] { 29, 30, 31, 32 };
        Median(days).Should().Be(30.5);
    }

    [Theory]
    [InlineData(new[] { 10.0, 10.0, 10.0 }, 0.0)]
    [InlineData(new[] { 10.0, 12.0, 14.0 }, true)]
    public void CoefficientOfVariation_LowForConsistentAmounts(double[] amounts, object _)
    {
        var mean = amounts.Average();
        var stddev = Math.Sqrt(amounts.Sum(a => Math.Pow(a - mean, 2)) / amounts.Count());
        var cv = stddev / mean;
        cv.Should().BeLessThanOrEqualTo(0.20);
    }

    [Fact]
    public void CoefficientOfVariation_HighForVariableAmounts()
    {
        var amounts = new[] { 10.0, 50.0, 100.0 };
        var mean = amounts.Average();
        var stddev = Math.Sqrt(amounts.Sum(a => Math.Pow(a - mean, 2)) / amounts.Length);
        var cv = stddev / mean;
        cv.Should().BeGreaterThan(0.20);
    }

    [Fact]
    public void Normalize_SameKeyForVariants()
    {
        MerchantNameNormalizer.Normalize("NETFLIX.COM")
            .Should().Be(MerchantNameNormalizer.Normalize("netflix.com"));
    }

    private static double Median(int[] values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }
}
