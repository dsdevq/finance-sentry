using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Mcp.Tools.Banking;
using FinanceSentry.Mcp.Tools.Investments;
using FinanceSentry.Modules.BankSync;
using FinanceSentry.Modules.BankSync.Application.Queries;
using FinanceSentry.Modules.BrokerageSync;
using FinanceSentry.Modules.BrokerageSync.Application.Queries;
using FinanceSentry.Modules.CryptoSync;
using FinanceSentry.Modules.CryptoSync.Application.Queries;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCqrs(
    typeof(GetAccountsQuery).Assembly,
    typeof(GetCryptoHoldingsQuery).Assembly,
    typeof(GetBrokerageHoldingsQuery).Assembly);

builder.Services.AddBankSyncModule(builder.Configuration);
builder.Services.AddCryptoSyncModule(builder.Configuration);
builder.Services.AddBrokerageSyncModule(builder.Configuration);

builder.Services.AddTransient<IMcpTool, ListBankAccountsTool>();
builder.Services.AddTransient<IMcpTool, ListBrokeragePositionsTool>();
builder.Services.AddTransient<IMcpTool, ListCryptoHoldingsTool>();

var app = builder.Build();
app.Run();
