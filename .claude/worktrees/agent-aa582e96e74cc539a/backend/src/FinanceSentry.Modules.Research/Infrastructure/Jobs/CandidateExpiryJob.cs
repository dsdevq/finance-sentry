namespace FinanceSentry.Modules.Research.Infrastructure.Jobs;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.Application.Commands;
using Hangfire;
using Microsoft.Extensions.Logging;

/// <summary>
/// Daily entry point that expires Active opportunity candidates past their TTL (US3/R9). Delegates
/// to <see cref="ExpireCandidatesCommand"/> so the schedule and any on-demand path share one code
/// path. Expired candidates are never deleted — they stay queryable for 020 counterfactuals.
/// </summary>
public sealed class CandidateExpiryJob(
    ICommandHandler<ExpireCandidatesCommand, ExpireCandidatesResult> handler,
    ILogger<CandidateExpiryJob> logger)
{
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var result = await handler.Handle(new ExpireCandidatesCommand(DateTimeOffset.UtcNow), ct);
        logger.LogInformation("Candidate expiry run: {ExpiredCount} candidates expired", result.ExpiredCount);
    }
}
