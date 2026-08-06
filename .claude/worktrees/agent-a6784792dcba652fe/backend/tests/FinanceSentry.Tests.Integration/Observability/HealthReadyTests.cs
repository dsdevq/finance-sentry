namespace FinanceSentry.Tests.Integration.Observability;

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

/// <summary>
/// Contract tests for <c>GET /api/v1/health/ready</c> (T017, contracts §2). The readiness report names
/// each dependency; with the database unreachable the overall status is 503 and the failing check is
/// named. The all-Healthy → 200 path needs a live database and is validated in quickstart T021.
/// </summary>
public class HealthReadyTests : IClassFixture<ObservabilityApiFactory>
{
    private readonly ObservabilityApiFactory _factory;

    public HealthReadyTests(ObservabilityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Ready_ReportNamesDatabaseAndHangfireChecks()
    {
        var client = _factory.CreateClient();

        // Read the body regardless of status — an unreachable DB makes the overall report unhealthy (503),
        // but the per-dependency checks must still be named.
        var response = await client.GetAsync("/api/v1/health/ready");
        var json = await response.Content.ReadAsStringAsync();
        var names = CheckNames(json);

        names.Should().Contain("database");
        names.Should().Contain("hangfire");
    }

    [Fact]
    public async Task Ready_WhenDatabaseUnreachable_Returns503AndNamesDatabaseUnhealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health/ready");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var database = doc.RootElement.GetProperty("checks").EnumerateArray()
            .Single(check => check.GetProperty("name").GetString() == "database");

        database.GetProperty("status").GetString().Should().Be("Unhealthy");
    }

    private static List<string?> CheckNames(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("checks").EnumerateArray()
            .Select(check => check.GetProperty("name").GetString())
            .ToList();
    }
}
