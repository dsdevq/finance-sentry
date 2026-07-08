namespace FinanceSentry.Modules.Research.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using FinanceSentry.Modules.Research.Domain.ThesisMonitor;
using Microsoft.Extensions.Logging;

public record RunThesisMonitorCommand(Guid UserId) : ICommand<ThesisMonitorRunSummary>;

/// <summary>
/// Evaluates every trigger of every thesis owned by a user and marks unbroken theses broken on
/// the first breaching trigger (OR semantics). Also auto-clears a broken thesis when a fresh
/// evaluation holds for every evaluable trigger (US3) — but never on an all-non-evaluable result,
/// since missing data must not un-break a thesis (FR-013).
/// </summary>
public class RunThesisMonitorCommandHandler(
    IThesisRepository thesisRepo,
    ISecEdgarService secEdgar,
    IMarketDataService marketData,
    IAlertGeneratorService alertGenerator,
    ILogger<RunThesisMonitorCommandHandler> logger)
    : ICommandHandler<RunThesisMonitorCommand, ThesisMonitorRunSummary>
{
    private const int MaxFundamentalsPerConcept = 8;

    public async Task<ThesisMonitorRunSummary> Handle(RunThesisMonitorCommand cmd, CancellationToken ct)
    {
        var theses = await thesisRepo.ListAsync(cmd.UserId, ct);

        var thesesEvaluated = 0;
        var triggersEvaluated = 0;
        var breaksRaised = 0;
        var breaksCleared = 0;
        var skipped = 0;
        var errors = 0;

        var fundamentalsCache = new Dictionary<string, IReadOnlyList<FundamentalFact>>(StringComparer.OrdinalIgnoreCase);
        var closesCache = new Dictionary<string, IReadOnlyList<DailyClose>>(StringComparer.OrdinalIgnoreCase);

        foreach (var thesis in theses)
        {
            if (thesis.InvalidationTriggers.Count == 0)
            {
                skipped++;
                continue;
            }

            try
            {
                thesesEvaluated++;

                var verdicts = new List<TriggerVerdict>();
                foreach (var trigger in thesis.InvalidationTriggers)
                {
                    triggersEvaluated++;
                    verdicts.Add(await EvaluateTriggerAsync(
                        trigger, thesis, fundamentalsCache, closesCache, ct));
                }

                var firstBreach = verdicts.OfType<TriggerVerdict.Breached>().FirstOrDefault();
                var allNonEvaluable = verdicts.Count > 0 && verdicts.All(v => v is TriggerVerdict.NonEvaluable);

                if (firstBreach is not null && thesis.BrokenAt is null)
                {
                    thesis.BrokenAt = DateTimeOffset.UtcNow;
                    thesis.BrokenReason = ComposeReason(firstBreach);
                    await thesisRepo.UpsertAsync(thesis, ct);
                    await alertGenerator.GenerateThesisBreakAlertAsync(
                        thesis.UserId, thesis.Id, thesis.Ticker, thesis.BrokenReason, ct);
                    breaksRaised++;
                }
                else if (firstBreach is null && allNonEvaluable)
                {
                    skipped++;
                }
                else if (firstBreach is null && thesis.BrokenAt is not null)
                {
                    // Broken→cleared: at least one trigger evaluated (not all-non-evaluable) and
                    // none breached. Missing data alone (allNonEvaluable, handled above) must never
                    // clear a broken thesis (FR-013).
                    thesis.BrokenAt = null;
                    thesis.BrokenReason = null;
                    await thesisRepo.UpsertAsync(thesis, ct);
                    await alertGenerator.ResolveThesisBreakAlertAsync(thesis.UserId, thesis.Id, ct);
                    breaksCleared++;
                }

                // firstBreach is not null && thesis already broken: still-broken, no-op (dedup lives
                // in the alert generator too). firstBreach is null && not all non-evaluable && not
                // previously broken: held, no-op.
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex, "Thesis monitor failed for thesis {ThesisId} ({Ticker})", thesis.Id, thesis.Ticker);
                errors++;
            }
        }

        return new ThesisMonitorRunSummary(
            thesesEvaluated, triggersEvaluated, breaksRaised, breaksCleared, skipped, errors);
    }

    private async Task<TriggerVerdict> EvaluateTriggerAsync(
        ThesisInvalidationTrigger trigger,
        InvestmentThesis thesis,
        Dictionary<string, IReadOnlyList<FundamentalFact>> fundamentalsCache,
        Dictionary<string, IReadOnlyList<DailyClose>> closesCache,
        CancellationToken ct)
    {
        var targetTicker = trigger.ProxyTicker ?? thesis.Ticker;

        if (ThesisMetric.IsPriceMetric(trigger.Metric))
        {
            var closes = await GetClosesAsync(targetTicker, thesis.CreatedAt, closesCache, ct);
            return ThesisBreakEvaluator.Evaluate(trigger, thesis.CreatedAt, [], closes);
        }

        var facts = await GetFundamentalsAsync(targetTicker, fundamentalsCache, ct);
        return ThesisBreakEvaluator.Evaluate(trigger, thesis.CreatedAt, facts, []);
    }

    private async Task<IReadOnlyList<FundamentalFact>> GetFundamentalsAsync(
        string ticker,
        Dictionary<string, IReadOnlyList<FundamentalFact>> cache,
        CancellationToken ct)
    {
        if (cache.TryGetValue(ticker, out var cached))
        {
            return cached;
        }

        var facts = await secEdgar.GetFundamentalsAsync(ticker, MaxFundamentalsPerConcept, ct);
        cache[ticker] = facts;
        return facts;
    }

    private async Task<IReadOnlyList<DailyClose>> GetClosesAsync(
        string ticker,
        DateTimeOffset since,
        Dictionary<string, IReadOnlyList<DailyClose>> cache,
        CancellationToken ct)
    {
        if (cache.TryGetValue(ticker, out var cached))
        {
            return cached;
        }

        var closes = await marketData.GetDailyClosesAsync(
            ticker, DateOnly.FromDateTime(since.UtcDateTime), ct);
        cache[ticker] = closes;
        return closes;
    }

    private static string ComposeReason(TriggerVerdict.Breached breach)
    {
        var observed = string.Join(", ", breach.ObservedValues.Select(v => v.ToString("0.####")));
        var periods = string.Join(", ", breach.Periods);
        return $"{breach.Metric} {breach.Direction} {breach.Threshold:0.####} " +
               $"— observed [{observed}] over [{periods}]";
    }
}
