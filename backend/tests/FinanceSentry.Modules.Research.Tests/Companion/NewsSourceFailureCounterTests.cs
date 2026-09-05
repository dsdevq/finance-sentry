namespace FinanceSentry.Modules.Research.Tests.Companion;

using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using FluentAssertions;
using Xunit;

/// <summary>
/// Per-source failure counter + alert-at-2-consecutive threshold (feature 030, T040/FR-009). The
/// counter is durable (lives on the NewsSource row) and resets on success.
/// </summary>
public sealed class NewsSourceFailureCounterTests
{
    [Fact]
    public void First_failure_does_not_alert_second_consecutive_does()
    {
        var source = new NewsSource();

        NewsSourceHealthTracker.RecordFailure(source, "timeout").Should().Be(NewsSourceFailureOutcome.None);
        source.ConsecutiveFailures.Should().Be(1);

        NewsSourceHealthTracker.RecordFailure(source, "timeout again").Should().Be(NewsSourceFailureOutcome.Alert);
        source.ConsecutiveFailures.Should().Be(2);
        source.LastFailureReason.Should().Be("timeout again");
    }

    [Fact]
    public void Alert_fires_only_once_not_every_subsequent_failure()
    {
        var source = new NewsSource();

        NewsSourceHealthTracker.RecordFailure(source, "1");
        NewsSourceHealthTracker.RecordFailure(source, "2").Should().Be(NewsSourceFailureOutcome.Alert);
        // Failures past the alert threshold must not re-alert every run (this was the 600+ alert bug).
        NewsSourceHealthTracker.RecordFailure(source, "3").Should().Be(NewsSourceFailureOutcome.None);
        NewsSourceHealthTracker.RecordFailure(source, "4").Should().Be(NewsSourceFailureOutcome.None);
    }

    [Fact]
    public void Sustained_failures_auto_disable_the_source_once()
    {
        var source = new NewsSource();

        NewsSourceFailureOutcome? disableOutcome = null;
        for (var i = 0; i < NewsSourceHealthTracker.DisableThreshold; i++)
        {
            disableOutcome = NewsSourceHealthTracker.RecordFailure(source, $"fail {i}");
        }

        disableOutcome.Should().Be(NewsSourceFailureOutcome.Disable);
        source.Enabled.Should().BeFalse("a source that fails to the disable threshold is retired");
        source.ConsecutiveFailures.Should().Be(NewsSourceHealthTracker.DisableThreshold);
    }

    [Fact]
    public void Success_resets_the_counter_and_clears_the_reason()
    {
        var source = new NewsSource { ConsecutiveFailures = 3, LastFailureReason = "boom" };

        NewsSourceHealthTracker.RecordSuccess(source);

        source.ConsecutiveFailures.Should().Be(0);
        source.LastFailureReason.Should().BeNull();
        source.LastSuccessAt.Should().NotBeNull();
    }

    [Fact]
    public void ClearFailures_revives_a_retired_source_without_faking_a_success()
    {
        var source = new NewsSource
        {
            Enabled = false,
            ConsecutiveFailures = 17,
            LastFailureReason = "TrendForce press-center article list not found",
        };

        NewsSourceHealthTracker.ClearFailures(source);

        source.Enabled.Should().BeTrue();
        source.ConsecutiveFailures.Should().Be(0);
        source.LastFailureReason.Should().BeNull();
        source.LastSuccessAt.Should().BeNull("nothing was actually fetched — only the history was voided");
    }

    [Fact]
    public void Cleared_source_gets_a_full_run_of_attempts_before_retiring_again()
    {
        // The #318 trap: re-enabling a source stuck above the disable threshold without resetting the
        // counter meant the very next failure retired it again, so it could never recover.
        var source = new NewsSource { Enabled = false, ConsecutiveFailures = 17 };

        NewsSourceHealthTracker.ClearFailures(source);

        NewsSourceHealthTracker.RecordFailure(source, "one").Should().Be(NewsSourceFailureOutcome.None);
        source.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Success_between_failures_prevents_the_alert()
    {
        var source = new NewsSource();

        NewsSourceHealthTracker.RecordFailure(source, "one").Should().Be(NewsSourceFailureOutcome.None);
        NewsSourceHealthTracker.RecordSuccess(source);
        NewsSourceHealthTracker.RecordFailure(source, "two").Should().Be(NewsSourceFailureOutcome.None, "the counter reset on success");
    }
}
