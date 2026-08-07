namespace FinanceSentry.Modules.Research.Tests;

using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Commands;
using FluentAssertions;
using Xunit;

/// <summary>
/// 039 (US4/SC-005): the IPS read/write contract no longer exposes the single-position cap — that
/// concept lives only in the Risk rule set now. Target allocation (intent) stays on the IPS.
/// </summary>
public sealed class IpsContractTests
{
    [Fact]
    public void IpsDto_HasNoMaxSinglePositionPct_ButKeepsAllocationTargets()
    {
        typeof(IpsDto).GetProperty("MaxSinglePositionPct").Should().BeNull();
        typeof(IpsDto).GetProperty("AllocationTargets").Should().NotBeNull();
        typeof(IpsDto).GetProperty("RebalancingRule").Should().NotBeNull();
    }

    [Fact]
    public void SaveIpsCommand_HasNoMaxSinglePositionPct_ButKeepsAllocationTargets()
    {
        typeof(SaveIpsCommand).GetProperty("MaxSinglePositionPct").Should().BeNull();
        typeof(SaveIpsCommand).GetProperty("AllocationTargets").Should().NotBeNull();
    }
}
