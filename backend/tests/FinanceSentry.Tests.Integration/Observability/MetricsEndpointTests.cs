namespace FinanceSentry.Tests.Integration.Observability;

using System.Net;
using FluentAssertions;
using Xunit;

/// <summary>
/// Contract test for <c>GET /metrics</c> (T016, contracts §1): reachable without a JWT and its Prometheus
/// exposition carries the custom <c>finance_jobs_*</c> series.
/// </summary>
public class MetricsEndpointTests : IClassFixture<ObservabilityApiFactory>
{
    private readonly ObservabilityApiFactory _factory;

    public MetricsEndpointTests(ObservabilityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Metrics_WithoutAuth_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Metrics_Exposition_ContainsCustomJobMetrics()
    {
        var client = _factory.CreateClient();

        var body = await client.GetStringAsync("/metrics");

        // finance_jobs_scheduled is an observable gauge, so it appears even before any job runs.
        body.Should().Contain("finance_jobs_");
    }
}
