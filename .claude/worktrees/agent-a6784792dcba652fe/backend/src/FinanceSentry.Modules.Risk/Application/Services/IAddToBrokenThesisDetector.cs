namespace FinanceSentry.Modules.Risk.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Risk.Domain;

/// <summary>
/// FR-006: flags a quantity increase on a position whose thesis is marked broken (017), but only
/// if the increase happens after the break, not before.
/// </summary>
public interface IAddToBrokenThesisDetector
{
    IReadOnlyList<BrokenThesisFlag> Detect(
        IReadOnlyList<HoldingSnapshot> history, IReadOnlyList<BrokenThesisSummary> brokenTheses);
}

public sealed record BrokenThesisFlag(string Ticker, DateTimeOffset IncreasedAt, decimal FromQuantity, decimal ToQuantity);
