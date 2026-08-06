namespace FinanceSentry.Modules.Retention.Domain;

/// <summary>
/// One execution of the generic purge or downsample job (feature 024, US1). Recorded so the
/// operator can review what a run examined/removed and how long it took (US1-AS2). Duration is
/// derived as <see cref="CompletedAt"/> − <see cref="StartedAt"/>.
/// </summary>
public sealed class RetentionRun
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public RetentionRunType RunType { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }

    public RetentionOutcome Outcome { get; set; } = RetentionOutcome.Failed;

    /// <summary>JSON array of <see cref="TableResult"/> (jsonb column). Defaults to an empty array.</summary>
    public string TableResults { get; set; } = "[]";

    /// <summary>Failure detail when the run did not fully succeed. Never contains secrets.</summary>
    public string? Error { get; set; }
}
