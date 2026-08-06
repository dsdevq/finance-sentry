namespace FinanceSentry.Modules.Research.Tests.Opportunity;

using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain.Opportunity;
using FinanceSentry.Modules.Research.Domain.Scoring;
using FluentAssertions;
using Xunit;

public sealed class CrowdingClassifierTests
{
    private static readonly OpportunityOptions Options = new();

    [Fact]
    public void Classify_ReturnsEarly_WhenExtensionAtOrBelowEarlyThreshold()
    {
        var result = CrowdingClassifier.Classify(0.02m, 1m, Options);

        result.Should().Be(CrowdingClass.Early);
    }

    [Fact]
    public void Classify_ReturnsExtended_WhenExtensionAndVolumeBothBreachThreshold()
    {
        var result = CrowdingClassifier.Classify(0.25m, 2m, Options);

        result.Should().Be(CrowdingClass.Extended);
    }

    [Fact]
    public void Classify_ReturnsNormal_WhenExtensionHighButVolumeNotConfirming()
    {
        var result = CrowdingClassifier.Classify(0.25m, 1m, Options);

        result.Should().Be(CrowdingClass.Normal);
    }

    [Fact]
    public void Classify_ReturnsNormal_WhenExtensionDataMissing()
    {
        var result = CrowdingClassifier.Classify(null, 2m, Options);

        result.Should().Be(CrowdingClass.Normal);
    }

    [Fact]
    public void Classify_IsDeterministic()
    {
        var result1 = CrowdingClassifier.Classify(0.10m, 1.2m, Options);
        var result2 = CrowdingClassifier.Classify(0.10m, 1.2m, Options);

        result1.Should().Be(result2);
    }
}
