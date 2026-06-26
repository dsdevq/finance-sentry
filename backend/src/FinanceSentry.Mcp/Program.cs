using System.Reflection;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Mcp.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;

var builder = Host.CreateApplicationBuilder(args);

// Enumerate module assemblies explicitly so they are guaranteed to be loaded.
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
];

// Register CQRS handlers and validators from all module assemblies and this assembly.
builder.Services.AddCqrs([..moduleAssemblies, Assembly.GetExecutingAssembly()]);

// Register each module's services (DbContexts, repositories, etc.) via IModuleRegistrar.
var registrarType = typeof(IModuleRegistrar);
foreach (var assembly in moduleAssemblies)
{
    foreach (var implType in assembly.GetTypes()
        .Where(t => !t.IsInterface && !t.IsAbstract && registrarType.IsAssignableFrom(t)))
    {
        var registrar = (IModuleRegistrar)Activator.CreateInstance(implType)!;
        registrar.Register(builder.Services, builder.Configuration);
    }
}

// Scan for IReadOnlyMcpTool implementations and register each under its concrete type
// and IReadOnlyMcpTool so callers can resolve IEnumerable<IReadOnlyMcpTool>.
var mcpAssembly = typeof(IReadOnlyMcpTool).Assembly;
var toolInterface = typeof(IReadOnlyMcpTool);
foreach (var toolType in mcpAssembly.GetTypes()
    .Where(t => !t.IsAbstract && !t.IsInterface && toolInterface.IsAssignableFrom(t)))
{
    builder.Services.AddScoped(toolType);
    builder.Services.AddScoped(toolInterface, sp => sp.GetRequiredService(toolType));
}

// Wire MCP server with stdio transport. Types decorated with [McpServerToolType] in the
// Mcp assembly are discovered and registered as MCP protocol tools.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(mcpAssembly, McpJsonUtilities.DefaultOptions);

await builder.Build().RunAsync();
