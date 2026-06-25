using FinanceSentry.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<LedgerTools>();

var app = builder.Build();

app.MapMcp();

await app.RunAsync();
