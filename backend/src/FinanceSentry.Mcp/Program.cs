using System.Reflection;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Mcp;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Mcp.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// MCP_TRANSPORT selects how this process speaks MCP:
//   stdio (default) — JSON-RPC over stdin/stdout, used by Claude Desktop and the
//                     ./mcp-probe.sh harness.
//   http            — Streamable HTTP (ASP.NET Core), used by OpenClaw which can't
//                     spawn arbitrary docker-exec processes from inside the gateway
//                     container.
var transport = (Environment.GetEnvironmentVariable("MCP_TRANSPORT") ?? "stdio").Trim().ToLowerInvariant();

var bootstrapLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var cliExitCode = await McpAuthCli.TryRunAsync(args, new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build(), bootstrapLoggerFactory);
if (cliExitCode.HasValue)
{
    Environment.Exit(cliExitCode.Value);
}

Assembly[] moduleAssemblies =
[
    typeof(FinanceSentry.Modules.Alerts.AlertsModule).Assembly,
    typeof(FinanceSentry.Modules.Auth.AuthModule).Assembly,
    typeof(FinanceSentry.Modules.BankSync.BankSyncModule).Assembly,
    typeof(FinanceSentry.Modules.BrokerageSync.BrokerageSyncModule).Assembly,
    typeof(FinanceSentry.Modules.Budgets.BudgetsModule).Assembly,
    typeof(FinanceSentry.Modules.CryptoSync.CryptoSyncModule).Assembly,
    typeof(FinanceSentry.Modules.Subscriptions.SubscriptionsModule).Assembly,
    typeof(FinanceSentry.Modules.Wealth.WealthModule).Assembly,
    typeof(FinanceSentry.Modules.Research.ResearchModule).Assembly,
];

var mcpAssembly = typeof(FinanceSentry.Mcp.Tools.GetAccountSummaryTool).Assembly;

if (transport is "stdio")
{
    var builder = Host.CreateApplicationBuilder(args);

    // stdio MCP framing: stdout carries JSON-RPC only — push all log output to stderr.
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

    RegisterShared(builder.Services, builder.Configuration);
    builder.Services.AddHostedService<LocalMcpSessionRefreshService>();

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(mcpAssembly);

    var host = builder.Build();
    await host.Services.GetRequiredService<LocalMcpSession>().InitializeAsync();
    await host.RunAsync();
}
else if (transport is "http" or "streamable-http")
{
    var builder = WebApplication.CreateBuilder(args);

    // HTTP transport — logs can go to stdout freely.
    RegisterShared(builder.Services, builder.Configuration);

    builder.Services
        .AddMcpServer()
        .WithHttpTransport(o => o.Stateless = false)
        .WithToolsFromAssembly(mcpAssembly);

    var port = int.TryParse(Environment.GetEnvironmentVariable("MCP_HTTP_PORT"), out var p) ? p : 5100;
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

    var app = builder.Build();
    app.UseMiddleware<McpJwtAuthenticationMiddleware>();
    app.MapMcp();
    await app.RunAsync();
}
else
{
    Console.Error.WriteLine($"Unknown MCP_TRANSPORT={transport}. Use 'stdio' or 'http'.");
    Environment.Exit(2);
}

void RegisterShared(IServiceCollection services, IConfiguration config)
{
    services.AddCqrs([..moduleAssemblies, Assembly.GetExecutingAssembly()]);

    var registrarType = typeof(IModuleRegistrar);
    foreach (var assembly in moduleAssemblies)
    {
        foreach (var implType in assembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && registrarType.IsAssignableFrom(t)))
        {
            var registrar = (IModuleRegistrar)Activator.CreateInstance(implType)!;
            registrar.Register(services, config);
        }
    }

    services.AddHttpContextAccessor();
    services.AddSingleton<LocalMcpCredentialStore>();
    services.AddSingleton<McpOAuthTokenClient>();
    services.AddSingleton<LocalMcpSession>();
    services.AddSingleton<IIdentityResolver, TransportAwareIdentityResolver>();

    foreach (var toolType in mcpAssembly.GetTypes()
        .Where(t => !t.IsAbstract && !t.IsInterface && t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null))
    {
        services.AddScoped(toolType);
    }
}
