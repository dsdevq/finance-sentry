namespace FinanceSentry.Modules.Research.Domain.Scoring;

using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain.ThesisMonitor;

/// <summary>
/// Pure, deterministic proposal of invalidation triggers from a scorecard, offered at promotion
/// time (US3/T021). Always proposes a price-drawdown trigger; proposes revenue/margin triggers only
/// when the underlying fundamentals were evaluable — never invents a threshold from missing data.
/// </summary>
public static class TriggerPrefill
{
    private const int DrawdownRoundingDecimals = 4;
    private const string PriceDrawdownRationale = "Standard downside guard: exit if price draws down beyond the configured threshold over the configured window.";
    private const string RevenueYoyRationale = "Thesis assumes continued revenue growth; break if YoY growth turns negative.";
    private const string GrossMarginRationale = "Break if gross margin compresses below the latest observed level, minus a buffer.";

    public static IReadOnlyList<ProposedTrigger> Propose(
        FundamentalsScoreDetail fundamentals, OpportunityOptions options)
    {
        var triggers = new List<ProposedTrigger>
        {
            new(
                ThesisMetric.PriceDrawdown,
                "greaterThan",
                Math.Round(options.DefaultDrawdownPrefill, DrawdownRoundingDecimals),
                ProxyTicker: null,
                ConsecutivePeriods: options.DefaultDrawdownConsecutiveDays,
                PriceDrawdownRationale),
        };

        if (fundamentals.RevenueYoy is { } revenueYoy && revenueYoy > 0)
        {
            triggers.Add(new ProposedTrigger(
                ThesisMetric.RevenueYoy,
                "lessThan",
                0m,
                ProxyTicker: null,
                ConsecutivePeriods: 1,
                RevenueYoyRationale));
        }

        if (fundamentals.GrossMarginLatest is { } marginLatest)
        {
            var threshold = Math.Round(marginLatest - options.GrossMarginPrefillBuffer, DrawdownRoundingDecimals);
            triggers.Add(new ProposedTrigger(
                ThesisMetric.GrossMargin,
                "lessThan",
                threshold,
                ProxyTicker: null,
                ConsecutivePeriods: 1,
                GrossMarginRationale));
        }

        return triggers;
    }
}

/// <summary>The subset of a fundamentals score the trigger prefill needs — decoupled from the full scorecard.</summary>
public sealed record FundamentalsScoreDetail(decimal? RevenueYoy, decimal? GrossMarginLatest);
