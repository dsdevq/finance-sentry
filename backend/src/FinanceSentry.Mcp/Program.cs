using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Alerts;
using FinanceSentry.Modules.Auth;
using FinanceSentry.Modules.BankSync;
using FinanceSentry.Modules.BrokerageSync;
using FinanceSentry.Modules.Budgets;
using FinanceSentry.Modules.CryptoSync;
using FinanceSentry.Modules.Subscriptions;
using FinanceSentry.Modules.Wealth;

var builder = WebApplication.CreateBuilder(args);

// Map POSTGRES_CONNECTION_STRING → ConnectionStrings:Default consumed by all module DbContexts.
var pgConnStr = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
if (pgConnStr is not null)
    builder.Configuration["ConnectionStrings:Default"] = pgConnStr;

// Pin one exported type per module so the runtime loads each assembly before the scan.
// The internal ModuleRegistrar classes are not directly reachable; reflection finds them.
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

var app = builder.Build();

app.Run();
