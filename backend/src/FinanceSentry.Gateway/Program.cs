using System.Globalization;
using System.Threading.RateLimiting;
using FinanceSentry.Gateway;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------------------------
// Edge gateway (feature 025) — single YARP reverse-proxy front door for frontend + API + MCP.
// Routing/clusters/health-checks are declarative in appsettings*.json (FR-002); only cross-cutting
// middleware (forwarded headers, rate limiting, TLS, metrics) is wired here.
// -----------------------------------------------------------------------------------------------

const int DefaultAuthPermitPerMinute = 10;
const int DefaultWebhookPermitPerMinute = 60;
const int TooManyRequestsStatusCode = StatusCodes.Status429TooManyRequests;

// Exports (data download) must stream through un-truncated (spec edge case): drop the body-size cap.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = null);

// YARP: routes + clusters (with health checks) loaded from the ReverseProxy config section.
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// FR-006: trust the single in-network hop so the real client IP/scheme reach the rate limiter and
// are propagated onward to backends. KnownNetworks/KnownProxies are cleared because the only hop in
// front of the gateway is Tailscale Serve / the container bridge, not an untrusted public proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// FR-004 / US3: per-client (real IP) fixed-window limits on auth + webhook routes. Limits are
// config-tunable (Gateway:RateLimits:*); rejections return 429 (SC-005) and are visible in metrics.
var authPermit = builder.Configuration.GetValue("Gateway:RateLimits:Auth:PermitPerMinute", DefaultAuthPermitPerMinute);
var webhookPermit = builder.Configuration.GetValue("Gateway:RateLimits:Webhook:PermitPerMinute", DefaultWebhookPermitPerMinute);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = TooManyRequestsStatusCode;
    options.AddPolicy(GatewayRateLimitPolicies.Auth, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(ClientPartitionKey(httpContext), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPermit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        }));
    options.AddPolicy(GatewayRateLimitPolicies.Webhook, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(ClientPartitionKey(httpContext), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = webhookPermit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        }));
});

// FR-007: expose gateway metrics (request counts, proxy latency via YARP meters, throttle events)
// on /metrics for the existing Prometheus scrape (observability stack, feature 023).
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("finance-sentry-gateway"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Yarp.ReverseProxy")
        .AddPrometheusExporter());

// US2 / FR-003: TLS termination via ACME (Let's Encrypt) — enabled ONLY when a public domain is
// configured AND the ToS is accepted. Empty/absent config (dev, or Tailscale-terminated prod) skips
// this entirely and the gateway serves plain HTTP. This is the config toggle described in research.md.
var acmeDomains = builder.Configuration.GetSection("LettuceEncrypt:DomainNames").Get<string[]>() ?? [];
var acmeAccepted = builder.Configuration.GetValue("LettuceEncrypt:AcceptTermsOfService", false);
var tlsEnabled = acmeDomains.Length > 0 && acmeAccepted;
if (tlsEnabled)
{
    builder.Services.AddLettuceEncrypt();
}

var app = builder.Build();

// Order matters: forwarded headers first (so the rate-limiter partition and proxied X-Forwarded-*
// see the real client), then rate limiting, then the proxy.
app.UseForwardedHeaders();

if (tlsEnabled)
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();

// Gateway liveness — namespaced so it never collides with a proxied path (the SPOF must be watchable).
app.MapGet("/gateway/health", () => Results.Ok(new { status = "healthy" }));

// Prometheus exposition at /metrics (exporter default path).
app.MapPrometheusScrapingEndpoint();

// All remaining traffic is proxied per the declarative route table.
app.MapReverseProxy();

app.Run();

static string ClientPartitionKey(HttpContext httpContext)
    => httpContext.Connection.RemoteIpAddress?.ToString()
       ?? httpContext.Request.Headers.Host.ToString()
       ?? CultureInfo.InvariantCulture.ToString();

// Exposed so the WebApplicationFactory-based test project can boot the gateway host.
public partial class Program;
