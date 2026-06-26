using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Mcp.Tools.Banking;
using FinanceSentry.Mcp.Tools.Investments;
using FinanceSentry.Mcp.Tools.Transactions;
using FinanceSentry.Modules.BankSync.Application.Queries;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence.Repositories;
using FinanceSentry.Modules.BrokerageSync.Application.Queries;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Persistence;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration["ConnectionStrings:Default"]
    ?? throw new InvalidOperationException("Connection string 'Default' is required.");

// BankSync persistence — list_bank_accounts, list_transactions
builder.Services.AddDbContext<BankSyncDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<IBankAccountRepository, BankAccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// BrokerageSync persistence — list_brokerage_positions
builder.Services.AddDbContext<BrokerageSyncDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<IBrokerageHoldingRepository, BrokerageHoldingRepository>();

// CQRS handlers (auto-discovered from each module assembly)
builder.Services.AddCqrs(
    typeof(GetAccountsQueryHandler).Assembly,
    typeof(GetBrokerageHoldingsQueryHandler).Assembly);

// MCP tools
builder.Services.AddScoped<IMcpTool, ListBankAccountsTool>();
builder.Services.AddScoped<IMcpTool, ListTransactionsTool>();
builder.Services.AddScoped<IMcpTool, ListBrokeragePositionsTool>();

await builder.Build().RunAsync();
