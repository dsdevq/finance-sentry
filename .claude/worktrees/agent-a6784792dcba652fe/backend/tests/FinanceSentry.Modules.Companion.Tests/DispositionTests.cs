namespace FinanceSentry.Modules.Companion.Tests;

using FinanceSentry.Modules.Companion.Application.Services;
using FinanceSentry.Modules.Companion.Domain;
using FluentAssertions;
using Xunit;

/// <summary>Mode → disposition mapping (feature 031, US2, T024).</summary>
public sealed class DispositionTests
{
    private readonly MaterialityPolicy _policy = new();

    [Theory]
    [InlineData(NotificationMode.Quiet, EventDisposition.SuppressedByMode)]
    [InlineData(NotificationMode.Digest, EventDisposition.HeldForDigest)]
    [InlineData(NotificationMode.Scan, EventDisposition.Pending)]
    [InlineData(NotificationMode.Realtime, EventDisposition.Pending)]
    public void Mode_maps_to_disposition(NotificationMode mode, EventDisposition expected)
        => _policy.DispositionForMode(mode).Should().Be(expected);
}
