using FinanceSentry.Mcp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ToolRegistry>();

var app = builder.Build();

app.Run();
