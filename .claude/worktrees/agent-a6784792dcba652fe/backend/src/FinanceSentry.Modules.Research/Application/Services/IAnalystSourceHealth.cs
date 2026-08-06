namespace FinanceSentry.Modules.Research.Application.Services;

using System.Collections.Concurrent;

/// <summary>
/// Tracks consecutive-failure counts for the analyst-actions sources (marketbeat, yahoo) so the
/// ingestion job can alert only after two consecutive failures per source (FR-009). Held in memory
/// on a singleton — counters need only persist between nightly runs while the process is up; a
/// restart forgives prior failures (same posture as the stateless Radar freshness watchdog).
/// </summary>
public interface IAnalystSourceHealth
{
    /// <summary>Increment the source's failure counter and return the new consecutive-failure count.</summary>
    int RecordFailure(string source);

    /// <summary>Reset the source's failure counter after a successful run.</summary>
    void RecordSuccess(string source);
}

public sealed class AnalystSourceHealth : IAnalystSourceHealth
{
    private readonly ConcurrentDictionary<string, int> failures = new(StringComparer.OrdinalIgnoreCase);

    public int RecordFailure(string source)
        => failures.AddOrUpdate(source, 1, (_, current) => current + 1);

    public void RecordSuccess(string source)
        => failures[source] = 0;
}
