using System.Reflection;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Alerts;
using FinanceSentry.Modules.Auth;
using FinanceSentry.Modules.BankSync;
using FinanceSentry.Modules.BrokerageSync;
using FinanceSentry.Modules.Budgets;
using FinanceSentry.Modules.CryptoSync;
using FinanceSentry.Modules.Subscriptions;
using FinanceSentry.Modules.Wealth;

var builder = WebApplication.CreateBuilder(args);

var pgConnStr = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
if (pgConnStr is not null)
    builder.Configuration["ConnectionStrings:Default"] = pgConnStr;

Type[] moduleAnchors =
[
    typeof(AlertsModule),
    typeof(AuthModule),
    typeof(BankSyncModule),
    typeof(BrokerageSyncModule),
    typeof(BudgetsModule),
    typeof(CryptoSyncModule),
    typeof(SubscriptionsModule),
    typeof(WealthModule),
];

var moduleAssemblies = moduleAnchors.Select(t => t.Assembly).Distinct().ToArray();

var registrars = moduleAssemblies
    .SelectMany(a => a.GetTypes())
    .Where(t => typeof(IModuleRegistrar).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
    .Select(t => (IModuleRegistrar)Activator.CreateInstance(t)!)
    .ToList();

builder.Services.AddCqrs(moduleAssemblies);

foreach (var registrar in registrars)
    registrar.Register(builder.Services, builder.Configuration);

var toolInterface = typeof(IMcpTool);
foreach (var toolType in Assembly.GetExecutingAssembly().GetTypes()
    .Where(t => toolInterface.IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false }))
{
    builder.Services.AddTransient(toolInterface, toolType);
}

var app = builder.Build();

app.Run();
