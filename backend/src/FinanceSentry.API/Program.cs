using Serilog;
using FinanceSentry.API.Conventions;
using FinanceSentry.API.Hangfire;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.API.Migrations;
using FinanceSentry.Modules.Auth;
using FinanceSentry.Modules.BankSync;
using FinanceSentry.Modules.CryptoSync;
using FinanceSentry.Modules.BrokerageSync;
using FinanceSentry.Modules.Alerts;
using FinanceSentry.Modules.Budgets;
using FinanceSentry.Infrastructure.Logging;
using FinanceSentry.Modules.BankSync.Infrastructure.Jobs;
using FinanceSentry.Modules.BankSync.API.Middleware;
using FinanceSentry.Modules.CryptoSync.Infrastructure.Jobs;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Jobs;
using Hangfire;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
        .Enrich.FromLogContext());

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddControllers(options =>
    options.Conventions.Add(new ApiVersionPrefixConvention("api/v1")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Finance Sentry API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT Bearer token"
    });
});

builder.Services.AddCqrs(
    typeof(FinanceSentry.Modules.Auth.Infrastructure.Services.JwtTokenService).Assembly,
    typeof(CryptoSyncModule).Assembly,
    typeof(BrokerageSyncModule).Assembly,
    typeof(BankSyncModule).Assembly,
    typeof(AlertsModule).Assembly,
    typeof(BudgetsModule).Assembly);

builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddBankSyncModule(builder.Configuration);
builder.Services.AddCryptoSyncModule(builder.Configuration);
builder.Services.AddBrokerageSyncModule(builder.Configuration);
builder.Services.AddAlertsModule(builder.Configuration);
builder.Services.AddBudgetsModule(builder.Configuration);

builder.Services.AddHangfireServices(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Default")!,
        name: "npgsql",
        tags: ["ready"]);

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(RateLimitingPolicies.Authenticated, cfg =>
    {
        cfg.PermitLimit = 100;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter(RateLimitingPolicies.Anonymous, cfg =>
    {
        cfg.PermitLimit = 10;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

var app = builder.Build();

app.MigrateAllModules();

app.UseSerilogRequestLogging();
app.UseCors("Frontend");
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new DevDashboardAuthorizationFilter()],
        DisplayStorageConnectionString = false,
        DashboardTitle = "Finance Sentry · Hangfire",
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/ready");

var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobManager.AddOrUpdate<BinanceSyncJob>("binance-sync", job => job.ExecuteAsync(), "*/15 * * * *");
recurringJobManager.AddOrUpdate<IBKRSyncJob>("ibkr-sync", job => job.ExecuteAsync(), "*/15 * * * *");
recurringJobManager.AddOrUpdate<UnusualSpendDetectionJob>("unusual-spend-detection", job => job.ExecuteAsync(CancellationToken.None), Cron.Daily());
recurringJobManager.AddOrUpdate<FinanceSentry.Modules.Alerts.Infrastructure.Jobs.AlertPurgeJob>("alert-purge", job => job.ExecuteAsync(CancellationToken.None), Cron.Monthly());

app.Run();

public partial class Program { }
