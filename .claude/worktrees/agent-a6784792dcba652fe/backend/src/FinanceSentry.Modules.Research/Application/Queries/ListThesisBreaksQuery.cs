namespace FinanceSentry.Modules.Research.Application.Queries;

using System.Globalization;
using System.Text.RegularExpressions;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;

public record ListThesisBreaksQuery(Guid UserId) : IQuery<IReadOnlyList<ThesisBreakView>>;

/// <summary>
/// Read-only projection of every currently-broken thesis for a user (FR-015, SC-005). Reflects
/// the persisted <c>BrokenAt</c>/<c>BrokenReason</c> state — no re-evaluation on read.
/// </summary>
public record ThesisBreakView(
    Guid ThesisId,
    string Ticker,
    string Metric,
    decimal[] ObservedValues,
    string[] Periods,
    decimal Threshold,
    string Direction,
    string Reason);

public class ListThesisBreaksQueryHandler(IThesisRepository repo)
    : IQueryHandler<ListThesisBreaksQuery, IReadOnlyList<ThesisBreakView>>
{
    // Mirrors RunThesisMonitorCommandHandler.ComposeReason: "{metric} {direction} {threshold} —
    // observed [v1, v2] over [p1, p2]". Parsing it back keeps ThesisBreakView explainable without
    // adding structured break columns for a single read-model concern (T021 best-effort derivation).
    private static readonly Regex ObservedPattern = new(@"observed \[(.*?)\]", RegexOptions.Compiled);
    private static readonly Regex PeriodsPattern = new(@"over \[(.*?)\]", RegexOptions.Compiled);

    public Task<IReadOnlyList<ThesisBreakView>> Handle(ListThesisBreaksQuery query, CancellationToken ct)
        => HandleAsync(query, ct);

    private async Task<IReadOnlyList<ThesisBreakView>> HandleAsync(ListThesisBreaksQuery query, CancellationToken ct)
    {
        var theses = await repo.ListAsync(query.UserId, ct);

        return theses
            .Where(t => t.BrokenAt is not null && t.BrokenReason is not null)
            .Select(ToView)
            .ToList();
    }

    private static ThesisBreakView ToView(InvestmentThesis thesis)
    {
        var reason = thesis.BrokenReason!;
        var metric = reason.Split(' ', 2)[0];
        var trigger = thesis.InvalidationTriggers.FirstOrDefault(
            t => string.Equals(t.Metric, metric, StringComparison.Ordinal));

        var (observedValues, periods) = ParseObservedAndPeriods(reason);

        return new ThesisBreakView(
            thesis.Id,
            thesis.Ticker,
            metric,
            observedValues,
            periods,
            trigger?.Threshold ?? 0m,
            trigger?.Direction ?? string.Empty,
            reason);
    }

    private static (decimal[] ObservedValues, string[] Periods) ParseObservedAndPeriods(string reason)
    {
        var observedMatch = ObservedPattern.Match(reason);
        var periodsMatch = PeriodsPattern.Match(reason);

        var observedValues = observedMatch.Success
            ? observedMatch.Groups[1].Value
                .Split(", ", StringSplitOptions.RemoveEmptyEntries)
                .Select(v => decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m)
                .ToArray()
            : [];

        var periods = periodsMatch.Success
            ? periodsMatch.Groups[1].Value.Split(", ", StringSplitOptions.RemoveEmptyEntries)
            : [];

        return (observedValues, periods);
    }
}
