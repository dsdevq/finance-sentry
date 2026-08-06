namespace FinanceSentry.Infrastructure.Observability;

using System.Diagnostics.Metrics;
using global::Hangfire;

/// <summary>
/// Custom metrics for the scheduled-job fleet (FR-002). Registered as a singleton and shared with the
/// Hangfire <c>JobMetricsFilter</c>; the OpenTelemetry meter provider subscribes to <see cref="MeterName"/>
/// and the Prometheus exporter renders these at <c>/metrics</c>.
///
/// Labels are deliberately bounded to the job name only — no per-user/account/PII dimensions (FR-007).
/// </summary>
public sealed class JobMetrics : IDisposable
{
    /// <summary>OpenTelemetry meter name; must be registered via <c>AddMeter(JobMetrics.MeterName)</c>.</summary>
    public const string MeterName = "FinanceSentry.Jobs";

    private const string DefaultQueue = "default";

    private readonly Meter _meter;
    private readonly Counter<long> _succeeded;
    private readonly Counter<long> _failed;
    private readonly Histogram<double> _duration;

    public JobMetrics()
    {
        _meter = new Meter(MeterName);

        // No unit on the counters so the Prometheus exporter renders clean *_total names.
        _succeeded = _meter.CreateCounter<long>(
            "finance_jobs_succeeded",
            description: "Scheduled-job runs that reached the Succeeded state.");
        _failed = _meter.CreateCounter<long>(
            "finance_jobs_failed",
            description: "Scheduled-job runs that reached the terminal Failed state.");
        _duration = _meter.CreateHistogram<double>(
            "finance_job_duration",
            unit: "s",
            description: "Wall-clock duration of a scheduled-job run to its terminal state.");

        _meter.CreateObservableGauge(
            "finance_jobs_scheduled",
            ObserveScheduled,
            description: "Jobs currently enqueued or scheduled and awaiting execution.");
    }

    /// <summary>Records a job that reached <c>Succeeded</c>.</summary>
    public void RecordSuccess(string job, double durationSeconds)
    {
        var tag = new KeyValuePair<string, object?>("job", job);
        _succeeded.Add(1, tag);
        if (durationSeconds >= 0)
            _duration.Record(durationSeconds, tag);
    }

    /// <summary>Records a job that reached the terminal <c>Failed</c> state.</summary>
    public void RecordFailure(string job, double durationSeconds)
    {
        var tag = new KeyValuePair<string, object?>("job", job);
        _failed.Add(1, tag);
        if (durationSeconds >= 0)
            _duration.Record(durationSeconds, tag);
    }

    private static IEnumerable<Measurement<long>> ObserveScheduled()
    {
        yield return new Measurement<long>(SafeScheduledCount());
    }

    // Reads Hangfire's own counts defensively — storage may not be configured yet during startup/tests.
    private static long SafeScheduledCount()
    {
        try
        {
            var monitor = JobStorage.Current?.GetMonitoringApi();
            if (monitor is null)
                return 0;
            return monitor.EnqueuedCount(DefaultQueue) + monitor.ScheduledCount();
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose() => _meter.Dispose();
}
