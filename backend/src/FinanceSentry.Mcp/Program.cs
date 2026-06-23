using FinanceSentry.Mcp;
using FinanceSentry.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<FinanceSentryApiOptions>()
    .BindConfiguration("FinanceSentryApi")
    .PostConfigure(options =>
    {
        options.ApiBaseUrl ??= Environment.GetEnvironmentVariable("FINANCESENTRY_API_BASE_URL");
        options.ApiToken ??= Environment.GetEnvironmentVariable("FINANCESENTRY_API_TOKEN");
    });

builder.Services.AddHttpClient<FinanceSentryApiClient>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<AlertTools>()
    .WithTools<BankingTools>()
    .WithTools<BrokerageTools>()
    .WithTools<BudgetTools>()
    .WithTools<CryptoTools>()
    .WithTools<ResearchTools>()
    .WithTools<SubscriptionTools>()
    .WithTools<WealthTools>();

await builder.Build().RunAsync();
