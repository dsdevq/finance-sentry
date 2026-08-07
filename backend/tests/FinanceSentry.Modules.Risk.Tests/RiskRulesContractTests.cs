using System.Text.Json;
using FinanceSentry.Modules.Risk.API.Controllers;
using FinanceSentry.Modules.Risk.Application.Queries;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Modules.Risk.Tests;

/// <summary>
/// 039 (US4/SC-005): the /risk/rules read/write contract no longer exposes target allocation — that
/// concept lives only in the IPS now. These assert the contract shape at the type/serialization level
/// (no DB needed): the moved field is absent from both the request and the response, and the retained
/// caps still round-trip.
/// </summary>
public sealed class RiskRulesContractTests
{
    [Fact]
    public void RiskRuleSetDto_Response_HasNoAllocationTargets()
    {
        var dto = new RiskRuleSetDto(
            Guid.NewGuid(), 1, 0.25m, 0.40m, 0.10m, 0.20m, 0.15m, 5, DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.Should().NotContainEquivalentOf("allocationTargets");
        json.Should().Contain("maxPositionWeightPct");
    }

    [Fact]
    public void RiskRuleSetDto_HasNoAllocationTargetsMember()
    {
        typeof(RiskRuleSetDto).GetProperty("AllocationTargets").Should().BeNull();
        typeof(RiskRuleSetDto).GetProperty("MaxPositionWeightPct").Should().NotBeNull();
    }

    [Fact]
    public void SaveRiskRulesRequest_HasNoAllocationTargetsMember()
    {
        // Posting allocationTargets is silently ignored by the model binder because the property is
        // absent from the request contract — it cannot be routed to the (removed) allocation home.
        typeof(SaveRiskRulesRequest).GetProperty("AllocationTargets").Should().BeNull();
        typeof(SaveRiskRulesRequest).GetProperty("MaxPositionWeightPct").Should().NotBeNull();
    }
}
