namespace FinanceSentry.Tests.Unit.BankSync.Application.Subscriptions;

using FinanceSentry.Modules.BankSync.Application.Services;
using FluentAssertions;
using Xunit;

public class MerchantNameNormalizerTests
{
    [Theory]
    [InlineData("NETFLIX.COM", "netflix")]
    [InlineData("netflix.com", "netflix")]
    [InlineData("PAYPAL*SPOTIFY", "spotify")]
    [InlineData("paypal*Netflix", "netflix")]
    [InlineData("  Amzn Mktp*123  ", "amzn mktp")]
    [InlineData("SPOTIFY.NET", "spotify")]
    [InlineData("*ADOBE CC", "adobe cc")]
    [InlineData("#AMAZON", "amazon")]
    [InlineData("Apple iCloud 99", "apple icloud")]
    [InlineData("Con Edison - 4567", "con edison")]
    public void Normalize_KnownInputs_ReturnsExpected(string input, string expected)
    {
        MerchantNameNormalizer.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "unknown")]
    [InlineData("", "unknown")]
    [InlineData("   ", "unknown")]
    [InlineData("*#  ", "unknown")]
    public void Normalize_EmptyOrWhitespace_ReturnsUnknown(string? input, string expected)
    {
        MerchantNameNormalizer.Normalize(input).Should().Be(expected);
    }

    [Fact]
    public void Normalize_CollapseInternalSpaces()
    {
        MerchantNameNormalizer.Normalize("NY   TIMES").Should().Be("ny times");
    }

    [Fact]
    public void Normalize_SameNormalizedKey_ForVariants()
    {
        var a = MerchantNameNormalizer.Normalize("Netflix.com");
        var b = MerchantNameNormalizer.Normalize("NETFLIX.COM");
        a.Should().Be(b);
    }

    [Fact]
    public void GetDisplayName_ReturnsMostFrequentNonNull()
    {
        var names = new[] { "Netflix", "NETFLIX", "Netflix", null, "NETFLIX" };
        MerchantNameNormalizer.GetDisplayName(names).Should().Be("Netflix");
    }

    [Fact]
    public void GetDisplayName_AllNull_ReturnsUnknown()
    {
        var names = new string?[] { null, null };
        MerchantNameNormalizer.GetDisplayName(names).Should().Be("unknown");
    }

    [Fact]
    public void NormalizeDetectionKey_PrefersMerchantNameOverDescription()
    {
        MerchantNameNormalizer.NormalizeDetectionKey("Netflix.com", "CARD PAYMENT 4471")
            .Should().Be("netflix");
    }

    [Fact]
    public void NormalizeDetectionKey_FallsBackToDescription_WhenMerchantNameMissing()
    {
        MerchantNameNormalizer.NormalizeDetectionKey(null, "NY   TIMES")
            .Should().Be("ny times");
    }

    [Fact]
    public void NormalizeDetectionKey_MobileTopUp_CollapsesToPerNumberKey()
    {
        // The phone number would otherwise fragment the key and trip the top-up blocklist.
        MerchantNameNormalizer.NormalizeDetectionKey(null, "*MOBI TOP-UP 0857860057")
            .Should().Be("mobile top-up 0057");
    }

    [Fact]
    public void NormalizeDetectionKey_BothMissing_ReturnsUnknown()
    {
        MerchantNameNormalizer.NormalizeDetectionKey(null, null).Should().Be("unknown");
    }
}
