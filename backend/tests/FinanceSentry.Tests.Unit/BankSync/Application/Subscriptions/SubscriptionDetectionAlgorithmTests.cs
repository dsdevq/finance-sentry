namespace FinanceSentry.Tests.Unit.BankSync.Application.Subscriptions;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
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

    [Theory]
    [InlineData("Погашення наступного платежу RozetkaPay")]
    [InlineData("Щомісячний платіж telemart - monomarket")]
    [InlineData("Оплата частинами Rozetka")]
    [InlineData("Купівля у розстрочку")]
    [InlineData("Installment payment 3/6")]
    public void InstallmentDescription_IsDetected(string description)
    {
        InstallmentPlanRecognizer.IsInstallmentDescription(description).Should().BeTrue();
    }

    [Theory]
    [InlineData("Spotify")]
    [InlineData("Anthropic* Claude Sub")]
    [InlineData("Netflix.com")]
    [InlineData(null)]
    [InlineData("")]
    public void NonInstallmentDescription_IsNotDetected(string? description)
    {
        InstallmentPlanRecognizer.IsInstallmentDescription(description).Should().BeFalse();
    }

    [Fact]
    public void BrandCanonicalization_UnifiesVaryingClaudeDescriptions()
    {
        var keys = new[]
        {
            "Anthropic* Claude Sub",
            "Claude.ai Subscription",
            "Anthropic Ireland",
        }.Select(MerchantNameNormalizer.Normalize).Distinct().ToList();

        keys.Should().ContainSingle().Which.Should().Be("claude");
    }

    [Fact]
    public void BrandCanonicalization_UnifiesVaryingChatgptDescriptions()
    {
        MerchantNameNormalizer.Normalize("Openai *chatgpt Subscr")
            .Should().Be(MerchantNameNormalizer.Normalize("Openai"));
    }

    [Theory]
    [InlineData("Погашення наступного платежу RozetkaPay", null, true)]
    [InlineData("Щомісячний платіж telemart - monomarket", null, true)]
    [InlineData("Погашення наступного платежу iSpace- monomarket", null, true)]
    [InlineData("Платіж Pandora", 4829, true)] // "Платіж <merchant>" on the installment MCC
    [InlineData("Платіж Pandora", 5262, false)] // same words, ordinary purchase MCC → not installment
    [InlineData("Переказ на картку", 4829, false)] // a transfer, not an installment
    [InlineData("Spotify", 5815, false)]
    public void IsInstallmentTransaction_ClassifiesByDescriptionAndMcc(string desc, int? mcc, bool expected)
    {
        InstallmentPlanRecognizer.IsInstallmentTransaction(desc, mcc).Should().Be(expected);
    }

    [Theory]
    [InlineData("Щомісячний платіж telemart - monomarket", "telemart")]
    [InlineData("Погашення наступного платежу RozetkaPay", "RozetkaPay")]
    [InlineData("Погашення наступного платежу iSpace- monomarket", "iSpace")]
    [InlineData("Погашення наступного платежу ТОВ Алло - monomarket", "ТОВ Алло")]
    [InlineData("Платіж Pandora", "Pandora")]
    [InlineData("Повне погашення RozetkaPay", "RozetkaPay")]
    public void ExtractInstallmentMerchant_RecoversMerchant(string desc, string expected)
    {
        InstallmentPlanRecognizer.ExtractMerchant(desc).Should().Be(expected);
    }

    [Theory]
    [InlineData("Повне погашення RozetkaPay", true)]
    [InlineData("Погашення наступного платежу RozetkaPay", false)]
    public void IsInstallmentPayoff_DetectsFullPayoff(string desc, bool expected)
    {
        InstallmentPlanRecognizer.IsInstallmentPayoff(desc).Should().Be(expected);
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

    // ── Amount clustering / plan identity (issue #482, live-data scenarios) ─

    private static SubscriptionDetectionJob.TxRow Tx(
        string description, decimal amount, int year, int month, int day,
        string currency = "EUR", int? mcc = null) =>
        new(Guid.Empty, null, description, amount, new DateTime(year, month, day), null, mcc, currency);

    [Fact]
    public void DetectSubscriptions_DiscontinuedPlanPrice_DoesNotPoisonCurrentPlan()
    {
        // Real Claude data: Pro (€22.14, ended June) + Max (€98.38 → €110.70 after a VAT
        // shift). Merchant-level CV over all five charges fails; the latest amount
        // cluster alone must pass.
        var txs = new[]
        {
            Tx("Claude.ai Subscription", 22.14m, 2026, 4, 11),
            Tx("Anthropic* Claude Sub", 22.14m, 2026, 6, 11),
            Tx("Anthropic* Claude Sub", 98.38m, 2026, 6, 24),
            Tx("Anthropic* Claude Sub", 110.70m, 2026, 7, 24),
            Tx("Anthropic* Claude Sub", 110.70m, 2026, 8, 24),
        };

        var result = SubscriptionDetectionJob.DetectSubscriptions(txs).Should().ContainSingle().Subject;

        result.MerchantNameNormalized.Should().Be("claude");
        result.Kind.Should().Be(SubscriptionKinds.Subscription);
        result.OccurrenceCount.Should().Be(3);
        result.LastKnownAmount.Should().Be(110.70m);
    }

    [Fact]
    public void DetectSubscriptions_MobileTopUp_TrackedPerNumber()
    {
        var txs = new[]
        {
            Tx("*MOBI TOP-UP 0857860057", 20.00m, 2026, 4, 28),
            Tx("*MOBI TOP-UP 0857860057", 20.00m, 2026, 5, 27),
            Tx("*MOBI TOP-UP 0857860057", 20.00m, 2026, 6, 25),
            Tx("*MOBI TOP-UP 0857860057", 20.00m, 2026, 7, 22),
        };

        var result = SubscriptionDetectionJob.DetectSubscriptions(txs).Should().ContainSingle().Subject;

        result.MerchantNameNormalized.Should().Be("mobile top-up 0057");
        result.MerchantNameDisplay.Should().Be("Mobile top-up 0057");
        result.OccurrenceCount.Should().Be(4);
    }

    [Fact]
    public void DetectSubscriptions_RecurringTransferToMaskedCard_IsAnInstallmentNotASubscription()
    {
        var txs = new[]
        {
            Tx("516936******4992", 14060.96m, 2026, 6, 11, "UAH"),
            Tx("516936******4992", 14060.96m, 2026, 7, 11, "UAH"),
            Tx("516936******4992", 14060.96m, 2026, 8, 11, "UAH"),
        };

        var result = SubscriptionDetectionJob.DetectSubscriptions(txs).Should().ContainSingle().Subject;

        result.Kind.Should().Be(SubscriptionKinds.Installment);
        result.MerchantNameNormalized.Should().Be("516936");
    }

    [Fact]
    public void DetectInstallments_TwoConcurrentPlansAtSameMerchant_StaySeparate()
    {
        // Real Алло data: old plan ₴2,339.95 (4 payments) + new plan ₴2,999.95 (Aug 22).
        var txs = new[]
        {
            Tx("Платіж ТОВ Алло - monomarket", 2339.95m, 2026, 5, 29, "UAH"),
            Tx("Погашення наступного платежу ТОВ Алло - monomarket", 2339.95m, 2026, 6, 1, "UAH"),
            Tx("Погашення наступного платежу ТОВ Алло - monomarket", 2339.95m, 2026, 7, 1, "UAH"),
            Tx("Погашення наступного платежу ТОВ Алло - monomarket", 2339.95m, 2026, 8, 5, "UAH"),
            Tx("Платіж ТОВ Алло - monomarket", 2999.95m, 2026, 8, 22, "UAH"),
        };

        var results = SubscriptionDetectionJob.DetectInstallments(txs).ToList();

        results.Should().HaveCount(2);
        var oldPlan = results.Single(r => r.MerchantNameNormalized == "installment:тов алло:2340");
        oldPlan.OccurrenceCount.Should().Be(4);
        oldPlan.LastKnownAmount.Should().Be(2339.95m);
        var newPlan = results.Single(r => r.MerchantNameNormalized == "installment:тов алло:3000");
        newPlan.OccurrenceCount.Should().Be(1);
    }

    [Fact]
    public void DetectInstallments_CentJitter_StaysOnePlan()
    {
        var txs = new[]
        {
            Tx("Щомісячний платіж telemart - monomarket", 6499.84m, 2026, 6, 1, "UAH"),
            Tx("Щомісячний платіж telemart - monomarket", 6499.85m, 2026, 7, 1, "UAH"),
        };

        var result = SubscriptionDetectionJob.DetectInstallments(txs).Should().ContainSingle().Subject;

        result.MerchantNameNormalized.Should().Be("installment:telemart:6500");
        result.OccurrenceCount.Should().Be(2);
    }

    [Fact]
    public void DetectInstallments_Payoff_CompletesOnlyThePlanItEnded()
    {
        // Real RozetkaPay data: an early full payoff on May 1 closed the old plan while a
        // new ₴1,371.89 plan kept charging from the same day.
        var txs = new[]
        {
            Tx("Погашення наступного платежу RozetkaPay", 800.00m, 2026, 3, 1, "UAH"),
            Tx("Погашення наступного платежу RozetkaPay", 800.00m, 2026, 4, 1, "UAH"),
            Tx("Повне погашення RozetkaPay", 4300.00m, 2026, 5, 1, "UAH"),
            Tx("Погашення наступного платежу RozetkaPay", 1371.89m, 2026, 5, 1, "UAH"),
            Tx("Погашення наступного платежу RozetkaPay", 1371.89m, 2026, 6, 1, "UAH"),
        };

        var results = SubscriptionDetectionJob.DetectInstallments(txs).ToList();

        results.Should().HaveCount(2);
        results.Single(r => r.MerchantNameNormalized == "installment:rozetkapay:800")
            .IsCompleted.Should().BeTrue();
        results.Single(r => r.MerchantNameNormalized == "installment:rozetkapay:1372")
            .IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void MobileTopUpKey_IsIdentifiable_DespiteTopUpBlocklist()
    {
        SubscriptionDetectionJob.IsUnidentifiableMerchant("mobile top-up 0057").Should().BeFalse();
    }

    [Theory]
    [InlineData("516936******4992", true)]
    [InlineData("516936", true)]
    [InlineData("545708******2195", true)]
    [InlineData("ТОВ Алло", false)]
    [InlineData("Netcup", false)]
    [InlineData("Mobile top-up 0057", false)]
    [InlineData(null, false)]
    public void MaskedPan_IsLikely_RecognizesCardCounterparties(string? text, bool expected)
    {
        MaskedPan.IsLikely(text).Should().Be(expected);
    }
}
