namespace FinanceSentry.Tests.Unit.BankSync.Application.Subscriptions;

using FinanceSentry.Modules.BankSync.Application.Services;
using FluentAssertions;
using Xunit;

public class SubscriptionDetectionAlgorithmTests
{
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
