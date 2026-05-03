using Serilog;
using FinanceSentry.API.Conventions;
using FinanceSentry.API.Hangfire;
using FinanceSentry.API.Migrations;
using FinanceSentry.API.Modules;
using FinanceSentry.Infrastructure.Logging;
using FinanceSentry.Modules.BankSync.API.Middleware;
using FinanceSentry.Modules.BankSync.Infrastructure.Jobs;
using Hangfire;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;

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
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT Bearer token"
    });
});

builder.Services.AddAllModules(builder.Configuration);

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

app.RegisterAllModuleJobs();

app.Run();

public partial class Program { }
