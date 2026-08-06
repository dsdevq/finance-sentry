namespace FinanceSentry.Modules.Research.Infrastructure.Jobs;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Research.Application.Commands;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain.Opportunity;
using FinanceSentry.Modules.Research.Domain.Scoring;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Scheduled FR-008 machine scan (019 US2): nominates candidates from 018 market structure by the
/// deterministic <see cref="ScanNominationRules"/> and scores each through the same pipeline as
/// user nominations, so 020 can compare hit rates across the two sources. Runs after Radar's
/// nightly compute. One ticker failing never aborts the run (FR-013); an empty 018 read aborts
/// with a recorded error; zero nominations is a valid, silent outcome.
/// </summary>
public sealed class OpportunityScanJob(
    IMarketStructureReader structureReader,
    Domain.Repositories.IIpsRepository ipsRepo,
    ICommandHandler<ScoreCandidateCommand, ScoreCandidateResult> scorer,
    IOptions<OpportunityOptions> options,
    ILogger<OpportunityScanJob> logger)
{
    private readonly OpportunityOptions _options = options.Value;

    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var universe = await structureReader.GetUniverseStructuresAsync(ct);
        if (universe.Count == 0)
        {
            logger.LogError("Opportunity scan aborted: no 018 structure data (empty universe read — ingestion outage?)");
            return;
        }

        var nominations = ScanNominationRules.Evaluate(universe, _options);
        var capped = nominations.Take(_options.ScanMaxNominationsPerRun).ToList();
        if (capped.Count < nominations.Count)
        {
            logger.LogWarning(
                "Opportunity scan capped nominations at {Cap}; dropped {Dropped}: {DroppedTickers}",
                _options.ScanMaxNominationsPerRun,
                nominations.Count - capped.Count,
                string.Join(", ", nominations.Skip(capped.Count).Select(n => n.Ticker)));
        }

        var userIds = await ipsRepo.GetUserIdsWithCurrentIpsAsync(ct);
        var scored = 0;
        var newCandidates = 0;
        var errors = 0;

        foreach (var userId in userIds)
        {
            foreach (var nomination in capped)
            {
                try
                {
                    var result = await scorer.Handle(
                        new ScoreCandidateCommand(
                            userId,
                            nomination.Ticker,
                            DecisionNote: null,
                            Source: CandidateSource.Scan,
                            NominationReasons: nomination.Reasons),
                        ct);
                    scored++;
                    if (result.IsNewCandidate)
                    {
                        newCandidates++;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    errors++;
                    logger.LogError(ex,
                        "Opportunity scan failed to score {Ticker} for user {UserId}", nomination.Ticker, userId);
                }
            }
        }

        logger.LogInformation(
            "Opportunity scan run: universe {Universe}, nominated {Nominated} (capped {Capped}), " +
            "users {Users}, scored {Scored}, new candidates {New}, errors {Errors}",
            universe.Count, nominations.Count, capped.Count, userIds.Count, scored, newCandidates, errors);
    }
}
