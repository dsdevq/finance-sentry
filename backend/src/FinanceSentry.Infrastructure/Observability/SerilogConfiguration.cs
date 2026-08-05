namespace FinanceSentry.Infrastructure.Observability;

using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

/// <summary>
/// Central Serilog setup (FR-003/011). Keeps the existing console + rolling-file sinks, suppresses the
/// EF Core <c>Database.Command</c> SQL flood that made grep-based triage slow (FR-011, override-able via
/// config), enriches every event with a bounded <c>module</c> + <c>app</c>, and — when a Loki URL is
/// configured — ships structured logs to Loki via the batched sink (fire-and-forget: shipping failures
/// are swallowed by the sink and never affect request handling, FR-003).
/// </summary>
public static class SerilogConfiguration
{
    private const string AppName = "finance-sentry";
    private const string LokiUrlConfigKey = "Observability:Loki:Url";

    /// <summary>Matches the <c>UseSerilog</c> host callback signature.</summary>
    public static void Configure(HostBuilderContext context, LoggerConfiguration loggerConfiguration)
    {
        var configuration = context.Configuration;

        loggerConfiguration
            .ReadFrom.Configuration(configuration)
            // EF SQL is the noise that slowed incident triage — keep it at Warning by default; a
            // Serilog:MinimumLevel:Override in config can raise it back to Debug without a code change.
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With<ModuleEnricher>()
            .Enrich.WithProperty("app", AppName)
            .WriteTo.Console()
            .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);

        var lokiUrl = configuration[LokiUrlConfigKey];
        if (!string.IsNullOrWhiteSpace(lokiUrl))
        {
            loggerConfiguration.WriteTo.GrafanaLoki(
                lokiUrl,
                labels: [new LokiLabel { Key = "app", Value = AppName }],
                propertiesAsLabels: ["module", "level"]);
        }
    }
}
