using Serilog;
using FinanceSentry.API.Conventions;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.BankSync;
using FinanceSentry.Modules.BankSync.Domain.Interfaces;
using FinanceSentry.Modules.CryptoSync;
using FinanceSentry.Modules.CryptoSync.Application.Services;
using FinanceSentry.Modules.CryptoSync.Domain.Interfaces;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;
using FinanceSentry.Modules.CryptoSync.Infrastructure.Binance;
using FinanceSentry.Modules.CryptoSync.Infrastructure.Jobs;
using FinanceSentry.Modules.CryptoSync.Infrastructure.Persistence;
using FinanceSentry.Modules.CryptoSync.Infrastructure.Persistence.Repositories;
using FinanceSentry.Modules.BrokerageSync;
using FinanceSentry.Modules.BrokerageSync.Application.Services;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Jobs;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Persistence;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Persistence.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Monobank;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;
using FinanceSentry.Modules.BankSync.Infrastructure.Security;
using FinanceSentry.Modules.BankSync.Infrastructure.Services;
using FinanceSentry.Modules.BankSync.Infrastructure.Jobs;
using FinanceSentry.Modules.BankSync.Infrastructure.AuditLog;
using FinanceSentry.Modules.BankSync.Infrastructure.FeatureFlags;
using FinanceSentry.Modules.BankSync.Infrastructure.Performance;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain.Services;
using FinanceSentry.Modules.BankSync.API.Middleware;
using FinanceSentry.Infrastructure;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Infrastructure.Logging;
using FinanceSentry.Modules.Auth.Domain.Entities;
using FinanceSentry.Modules.Auth.Application.Interfaces;
using FinanceSentry.Modules.Auth.Infrastructure.Persistence;
using FinanceSentry.Modules.Auth.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Hangfire;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog structured logging ──────────────────────────────────────────────
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File(
            "logs/app-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14)
        .Enrich.FromLogContext()
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ── ASP.NET Core services ───────────────────────────────────────────────────
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

// ── CQRS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCqrs(
    typeof(JwtTokenService).Assembly,
    typeof(CryptoSyncModule).Assembly,
    typeof(BrokerageSyncModule).Assembly,
    typeof(BankSyncModule).Assembly);

// ── Database (EF Core + PostgreSQL) ─────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is required.");

builder.Services.AddDbContext<FinanceSentry.Modules.BankSync.Infrastructure.Persistence.BankSyncDbContext>(
    options => options.UseNpgsql(connectionString));

builder.Services.AddDbContext<AuthDbContext>(
    options => options.UseNpgsql(connectionString));

builder.Services.AddDbContext<CryptoSyncDbContext>(
    options => options.UseNpgsql(connectionString));

builder.Services.AddDbContext<BrokerageSyncDbContext>(
    options => options.UseNpgsql(connectionString));

// ── ASP.NET Core Identity ────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

// ── Token service ────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

// ── Google credential verification (GSI) ─────────────────────────────────────
builder.Services.Configure<GoogleOAuthOptions>(builder.Configuration.GetSection("GoogleOAuth"));
builder.Services.AddScoped<IGoogleCredentialVerifier, GoogleCredentialVerifier>();

// ── Repositories ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IBankAccountRepository, BankAccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ISyncJobRepository, SyncJobRepository>();
builder.Services.AddScoped<IEncryptedCredentialRepository, EncryptedCredentialRepository>();
builder.Services.AddScoped<IMonobankCredentialRepository, MonobankCredentialRepository>();

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
builder.Services.AddScoped<IPlaidAdapter>(sp => sp.GetRequiredService<PlaidAdapter>());
builder.Services.AddScoped<IBankProvider>(sp => sp.GetRequiredService<PlaidAdapter>());

// ── Monobank integration ─────────────────────────────────────────────────────
builder.Services.AddHttpClient<MonobankHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Monobank:BaseUrl"] ?? "https://api.monobank.ua");
});
builder.Services.AddScoped<IMonobankAdapter, MonobankAdapter>();
builder.Services.AddScoped<MonobankAdapter>();
builder.Services.AddScoped<IBankProvider>(sp => sp.GetRequiredService<MonobankAdapter>());

// ── Bank provider factory (resolves by BankAccount.Provider) ─────────────────
builder.Services.AddScoped<IBankProviderFactory, BankProviderFactory>();

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

// ── CryptoSync module (009-binance-integration) ───────────────────────────────
builder.Services.AddHttpClient<BinanceHttpClient>();
builder.Services.AddScoped<ICryptoExchangeAdapter, BinanceAdapter>();
builder.Services.AddScoped<IBinanceCredentialRepository, BinanceCredentialRepository>();
builder.Services.AddScoped<ICryptoHoldingRepository, CryptoHoldingRepository>();
builder.Services.AddScoped<ICryptoHoldingsReader, CryptoHoldingsReader>();
builder.Services.AddScoped<BinanceSyncJob>();

// ── BrokerageSync module (010-ibkr-integration) ──────────────────────────────
// IBeam serves the Client Portal API over HTTPS with a self-signed cert. Allow it
// in dev only; production deployments must terminate TLS at a real proxy.
builder.Services.AddHttpClient<IBKRGatewayClient>()
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var env = sp.GetRequiredService<IHostEnvironment>();
        var allowSelfSigned = builder.Configuration.GetValue<bool>("IBKR:AllowSelfSignedCert")
            || env.IsDevelopment();
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = allowSelfSigned
                ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                : null,
        };
    });
builder.Services.AddScoped<IBrokerAdapter, IBKRAdapter>();
builder.Services.AddScoped<IIBKRCredentialRepository, IBKRCredentialRepository>();
builder.Services.AddScoped<IBrokerageHoldingRepository, BrokerageHoldingRepository>();
builder.Services.AddScoped<IBrokerageHoldingsReader, BrokerageHoldingsReader>();
builder.Services.AddScoped<IBKRSyncJob>();

// ── Dashboard / aggregation services (T401–T410) ─────────────────────────────
builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.AddScoped<IMoneyFlowStatisticsService, MoneyFlowStatisticsService>();
builder.Services.AddScoped<IMerchantCategoryStatisticsService, MerchantCategoryStatisticsService>();
builder.Services.AddScoped<IDashboardQueryService, DashboardQueryService>();
builder.Services.AddScoped<ITransferDetectionService, TransferDetectionService>();
builder.Services.AddScoped<IWealthAggregationService, WealthAggregationService>();

// ── Hangfire background jobs (T303, T304) ────────────────────────────────────
builder.Services.AddHangfireServices(builder.Configuration);
builder.Services.AddScoped<ScheduledSyncJob>();
builder.Services.AddScoped<SyncScheduler>();
builder.Services.AddScoped<DataRetentionJob>();
builder.Services.AddScoped<CredentialBackupJob>();

// ── Feature flags (T521) ─────────────────────────────────────────────────────
builder.Services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

// ── Audit logging (T524–T525) ────────────────────────────────────────────────
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();

// ── EF query performance interceptor (T505) ─────────────────────────────────
builder.Services.AddScoped<EFQueryLoggerInterceptor>();

// ── Health checks (T512) ─────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "npgsql", tags: ["ready"]);

// ── Rate limiting (T502) ─────────────────────────────────────────────────────
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

// ── Database initialization and migrations ──────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    try
    {
        var bankSyncDbContext = scope.ServiceProvider.GetRequiredService<FinanceSentry.Modules.BankSync.Infrastructure.Persistence.BankSyncDbContext>();
        app.Logger.LogInformation("Applying BankSync database migrations...");
        bankSyncDbContext.Database.Migrate();
        app.Logger.LogInformation("BankSync migrations applied successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "BankSync migration failed. Startup will continue.");
    }

    try
    {
        var authDbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        app.Logger.LogInformation("Applying Auth database migrations...");
        authDbContext.Database.Migrate();
        app.Logger.LogInformation("Auth migrations applied successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Auth migration failed. Startup will continue.");
    }

    try
    {
        var cryptoSyncDbContext = scope.ServiceProvider.GetRequiredService<CryptoSyncDbContext>();
        app.Logger.LogInformation("Applying CryptoSync database migrations...");
        cryptoSyncDbContext.Database.Migrate();
        app.Logger.LogInformation("CryptoSync migrations applied successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "CryptoSync migration failed. Startup will continue.");
    }

    try
    {
        var brokerageSyncDbContext = scope.ServiceProvider.GetRequiredService<BrokerageSyncDbContext>();
        app.Logger.LogInformation("Applying BrokerageSync database migrations...");
        brokerageSyncDbContext.Database.Migrate();
        app.Logger.LogInformation("BrokerageSync migrations applied successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "BrokerageSync migration failed. Startup will continue.");
    }
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
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
    app.UseHangfireDashboard("/hangfire");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/ready");

// ── Hangfire recurring jobs ───────────────────────────────────────────────────
var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobManager.AddOrUpdate<BinanceSyncJob>(
    "binance-sync",
    job => job.ExecuteAsync(),
    "*/15 * * * *");
recurringJobManager.AddOrUpdate<IBKRSyncJob>(
    "ibkr-sync",
    job => job.ExecuteAsync(),
    "*/15 * * * *");

app.Run();

// Expose Program for WebApplicationFactory in integration/contract tests
public partial class Program { }
