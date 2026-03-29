using Serilog;
using FinanceSentry.Modules.BankSync;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;
using FinanceSentry.Modules.BankSync.Infrastructure.Security;
using FinanceSentry.Modules.BankSync.Infrastructure.Services;
using FinanceSentry.Modules.BankSync.Infrastructure.Jobs;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Infrastructure;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Infrastructure.Logging;
using Hangfire;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog structured logging ──────────────────────────────────────────────
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
        .Enrich.FromLogContext()
        .MinimumLevel.Information()
);

// ── ASP.NET Core services ───────────────────────────────────────────────────
builder.Services.AddControllers();
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

// ── MediatR (CQRS) ──────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(Program).Assembly,
    typeof(BankSyncModule).Assembly
));

// ── Database (EF Core + PostgreSQL) ─────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is required.");

builder.Services.AddDbContext<FinanceSentry.Modules.BankSync.Infrastructure.Persistence.BankSyncDbContext>(
    options => options.UseNpgsql(connectionString));

// ── Repositories ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IBankAccountRepository, BankAccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ISyncJobRepository, SyncJobRepository>();
builder.Services.AddScoped<IEncryptedCredentialRepository, EncryptedCredentialRepository>();

// ── Encryption (AES-256-GCM, T101) ──────────────────────────────────────────
builder.Services.Configure<EncryptionOptions>(
    builder.Configuration.GetSection(EncryptionOptions.SectionName));
builder.Services.AddSingleton<ICredentialEncryptionService, CredentialEncryptionService>();

// ── Transaction deduplication (T104) ────────────────────────────────────────
var deduplicationKey = builder.Configuration["Deduplication:MasterKeyBase64"]
    ?? throw new InvalidOperationException("Deduplication:MasterKeyBase64 is required.");
builder.Services.AddSingleton<ITransactionDeduplicationService>(
    _ => new TransactionDeduplicationService(deduplicationKey));

// ── Plaid integration (T204) ─────────────────────────────────────────────────
builder.Services.AddHttpClient<IPlaidClient, PlaidHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Plaid:BaseUrl"] ?? "https://sandbox.plaid.com");
});
builder.Services.AddScoped<PlaidAdapter>();
builder.Services.AddScoped<FinanceSentry.Modules.BankSync.Infrastructure.Plaid.IPlaidAdapter>(
    sp => sp.GetRequiredService<PlaidAdapter>());

// ── Infrastructure services ──────────────────────────────────────────────────
builder.Services.AddSingleton<CorrelationIdAccessor>();
builder.Services.AddScoped<ICorrelationIdAccessor>(sp => sp.GetRequiredService<CorrelationIdAccessor>());
builder.Services.AddScoped<IBankSyncLogger, BankSyncLogger>();

// ── Webhook security (T306) ──────────────────────────────────────────────────
builder.Services.AddSingleton<IWebhookSignatureValidator, WebhookSignatureValidator>();

// ── Plaid error mapping (T306-A) ─────────────────────────────────────────────
builder.Services.AddSingleton<IPlaidErrorMapper, PlaidErrorMapper>();

// ── Sync services (T302, T307) ───────────────────────────────────────────────
builder.Services.AddScoped<IScheduledSyncService, ScheduledSyncService>();
builder.Services.AddScoped<ITransactionSyncCoordinator, TransactionSyncCoordinator>();

// ── Dashboard / aggregation services (T401–T410) ─────────────────────────────
builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.AddScoped<IMoneyFlowStatisticsService, MoneyFlowStatisticsService>();
builder.Services.AddScoped<IMerchantCategoryStatisticsService, MerchantCategoryStatisticsService>();
builder.Services.AddScoped<IDashboardQueryService, DashboardQueryService>();
builder.Services.AddScoped<ITransferDetectionService, TransferDetectionService>();

// ── Hangfire background jobs (T303, T304) ────────────────────────────────────
builder.Services.AddHangfireServices(builder.Configuration);
builder.Services.AddScoped<ScheduledSyncJob>();
builder.Services.AddScoped<SyncScheduler>();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard("/hangfire");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Expose Program for WebApplicationFactory in integration/contract tests
public partial class Program { }
