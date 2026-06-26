using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Mcp.Tools.BankCash;
using FinanceSentry.Modules.BankSync.Application.Queries;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BankSyncDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")!));

builder.Services.AddScoped<IBankAccountRepository, BankAccountRepository>();

builder.Services.AddCqrs(typeof(GetAccountsQueryHandler).Assembly);

builder.Services.AddScoped<IMcpTool, ListBankAccountsTool>();
builder.Services.AddScoped<IMcpTool, GetAccountBalancesTool>();

var app = builder.Build();

app.Run();
