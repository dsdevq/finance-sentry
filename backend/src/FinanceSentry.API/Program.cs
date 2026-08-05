using Serilog;
using FinanceSentry.API.Commands;
using FinanceSentry.API.Conventions;
using FinanceSentry.API.Hangfire;
using FinanceSentry.API.Migrations;
using FinanceSentry.API.Modules;
using FinanceSentry.Infrastructure.Fx;
using FinanceSentry.Infrastructure.Logging;
using FinanceSentry.Infrastructure.Observability;
using FinanceSentry.Infrastructure.Observability.HealthChecks;
using FinanceSentry.Modules.BankSync.API.Middleware;
using FinanceSentry.Modules.BankSync.Infrastructure.Jobs;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.OpenApi;
using DashboardObservability = FinanceSentry.Infrastructure.Observability.Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(SerilogConfiguration.Configure);

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddControllers(options =>
    options.Conventions.Add(new ApiVersionPrefixConvention("api/v1")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Finance Sentry API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT Bearer token"
    });
});

builder.Services.AddAllModules(builder.Configuration);

builder.Services.AddExchangeRates(builder.Configuration);

builder.Services.AddHangfireServices(builder.Configuration, builder.Environment);

// OpenTelemetry metrics (FR-001/002) — ASP.NET Core + runtime + custom job meter, exposed at /metrics.
builder.Services.AddObservabilityMetrics();

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Default")!,
        name: "database",
        tags: ["ready"])
    .AddHangfire(
        options => options.MinimumAvailableServers = 1,
        name: "hangfire",
        tags: ["ready"]);

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(RateLimitingPolicies.Authenticated, cfg =>
    {
        cfg.PermitLimit = 100;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter(RateLimitingPolicies.Anonymous, cfg =>
    {
        cfg.PermitLimit = 10;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

var app = builder.Build();

app.MigrateAllModules();

// One-shot maintenance verb (e.g. `dotnet FinanceSentry.API.dll recategorize [userId]`).
// Runs the task and exits without ever starting the web server.
if (args.Length > 0 && args[0] == RecategorizationCommand.Verb)
{
    await RecategorizationCommand.RunAsync(app.Services, args);
    return;
}

if (args.Length > 0 && args[0] == SubscriptionDetectionCommand.Verb)
{
    await SubscriptionDetectionCommand.RunAsync(app.Services, args);
    return;
}

app.UseSerilogRequestLogging();
app.UseCors("Frontend");
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Hangfire dashboard (FR-004): permissive locally; loopback/Tailscale-only in every other environment,
// backed by durable Postgres storage so history/schedule survive restarts.
IDashboardAuthorizationFilter dashboardFilter = app.Environment.IsDevelopment()
    ? new DevDashboardAuthorizationFilter()
    : new DashboardObservability.DashboardAuthorizationFilter();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [dashboardFilter],
    DisplayStorageConnectionString = false,
    DashboardTitle = "Finance Sentry · Hangfire",
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Prometheus exposition (FR-001) — scrape-only / not on the public funnel (FR-006).
app.MapObservabilityMetricsEndpoint();

// Readiness (SC-003): overall + per-dependency status; JWT-exempt via /api/v1/health prefix.
app.MapHealthChecks("/api/v1/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = ReadinessResponseWriter.WriteAsync,
});

// Record per-job outcome + duration metrics as jobs reach terminal states (FR-002).
GlobalJobFilters.Filters.Add(
    new DashboardObservability.JobMetricsFilter(app.Services.GetRequiredService<JobMetrics>()));

app.RegisterAllModuleJobs();

// Live FX rates: refresh daily, and once immediately so we leave the hardcoded
// fallback table behind as soon as the app is up.
var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate<ExchangeRateRefreshJob>(
    "exchange-rate-refresh",
    job => job.RunAsync(CancellationToken.None),
    Cron.Daily());
BackgroundJob.Enqueue<ExchangeRateRefreshJob>(job => job.RunAsync(CancellationToken.None));

app.Run();

public partial class Program { }
