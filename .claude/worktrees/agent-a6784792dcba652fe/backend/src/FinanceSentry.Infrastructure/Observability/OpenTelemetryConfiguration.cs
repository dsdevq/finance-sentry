namespace FinanceSentry.Infrastructure.Observability;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

/// <summary>
/// Wires OpenTelemetry metrics (FR-001/002): ASP.NET Core request instrumentation, .NET runtime
/// instrumentation, the custom job <see cref="JobMetrics"/> meter, and the Prometheus exporter served
/// at <c>/metrics</c>. HTTP labels use route templates (not raw URLs) so cardinality stays bounded
/// (FR-007); the endpoint is intended to be reachable only over the compose network / Tailscale (FR-006).
/// </summary>
public static class OpenTelemetryConfiguration
{
    private const string ServiceName = "finance-sentry";

    public static IServiceCollection AddObservabilityMetrics(this IServiceCollection services)
    {
        // Shared singleton: the Hangfire JobMetricsFilter and the meter provider observe the same instruments.
        services.AddSingleton<JobMetrics>();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(JobMetrics.MeterName)
                .AddPrometheusExporter());

        return services;
    }

    /// <summary>Serves Prometheus exposition at <c>/metrics</c> (the exporter's default path).</summary>
    public static IEndpointRouteBuilder MapObservabilityMetricsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPrometheusScrapingEndpoint();
        return endpoints;
    }
}
