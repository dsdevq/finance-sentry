namespace FinanceSentry.Modules.Research.Domain.Scoring;

using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain.Opportunity;

/// <summary>
/// Pure, deterministic classification of how structurally stretched a ticker looks at scoring time
/// (FR-005). Thresholds come from <see cref="OpportunityOptions"/> — no magic numbers here. Missing
/// extension/volume data defaults to <see cref="CrowdingClass.Normal"/> (an honest "unknown, assume
/// nothing extreme" rather than guessing Early or Extended).
/// </summary>
public static class CrowdingClassifier
{
    public static CrowdingClass Classify(decimal? extensionFromMa50, decimal? volumeRatio, OpportunityOptions options)
    {
        if (extensionFromMa50 is null)
        {
            return CrowdingClass.Normal;
        }

        if (extensionFromMa50 <= options.EarlyExtensionThreshold)
        {
            return CrowdingClass.Early;
        }

        var isExtendedByExtension = extensionFromMa50 >= options.ExtendedExtensionThreshold;
        var isExtendedByVolume = volumeRatio is { } ratio && ratio >= options.ExtendedVolumeRatioThreshold;

        return isExtendedByExtension && isExtendedByVolume ? CrowdingClass.Extended : CrowdingClass.Normal;
    }
}
