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

        NewsSourceHealthTracker.RecordFailure(source, "timeout").Should().BeFalse();
        source.ConsecutiveFailures.Should().Be(1);

        NewsSourceHealthTracker.RecordFailure(source, "timeout again").Should().BeTrue();
        source.ConsecutiveFailures.Should().Be(2);
        source.LastFailureReason.Should().Be("timeout again");
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
    public void Success_between_failures_prevents_the_alert()
    {
        var source = new NewsSource();

        NewsSourceHealthTracker.RecordFailure(source, "one").Should().BeFalse();
        NewsSourceHealthTracker.RecordSuccess(source);
        NewsSourceHealthTracker.RecordFailure(source, "two").Should().BeFalse("the counter reset on success");
    }
}
