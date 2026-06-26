using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Protocol;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation { Name = "finance-sentry", Version = "1.0.0" };
    })
    .WithStdioServerTransport();

await builder.Build().RunAsync();
