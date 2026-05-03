namespace FinanceSentry.Modules.BankSync;

using FinanceSentry.Infrastructure.Logging;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;
using FinanceSentry.Modules.BankSync.Domain.Interfaces;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Domain.Services;
using FinanceSentry.Modules.BankSync.Infrastructure.AuditLog;
using FinanceSentry.Modules.BankSync.Infrastructure.FeatureFlags;
using FinanceSentry.Modules.BankSync.Infrastructure.Jobs;
using FinanceSentry.Modules.BankSync.Infrastructure.Monobank;
using FinanceSentry.Modules.BankSync.Infrastructure.Performance;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;
using FinanceSentry.Modules.BankSync.Infrastructure.Security;
using FinanceSentry.Modules.BankSync.Infrastructure.Services;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Infrastructure;
using FinanceSentry.Infrastructure.Encryption;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class BankSyncModule
{
    internal sealed class ModuleRegistrar : IModuleRegistrar
    {
        public void Register(IServiceCollection services, IConfiguration config)
            => services.AddBankSyncModule(config);
    }

    private sealed class JobRegistrar : IJobRegistrar
    {
        public void RegisterJobs(IServiceProvider sp)
        {
            var mgr = sp.GetRequiredService<IRecurringJobManager>();
            mgr.AddOrUpdate<UnusualSpendDetectionJob>(
                "unusual-spend-detection",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily());
        }
    }


    public static IServiceCollection AddBankSyncModule(
        this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")!;

        services.AddDbContext<BankSyncDbContext>(o => o.UseNpgsql(connectionString));

        services.Configure<EncryptionOptions>(config.GetSection(EncryptionOptions.SectionName));
        services.AddSingleton<ICredentialEncryptionService, CredentialEncryptionService>();

        services.AddScoped<IBankAccountRepository, BankAccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ISyncJobRepository, SyncJobRepository>();
        services.AddScoped<IEncryptedCredentialRepository, EncryptedCredentialRepository>();
        services.AddScoped<IMonobankCredentialRepository, MonobankCredentialRepository>();

        var deduplicationKey = config["Deduplication:MasterKeyBase64"]
            ?? throw new InvalidOperationException("Deduplication:MasterKeyBase64 is required.");
        services.AddSingleton<ITransactionDeduplicationService>(
            _ => new TransactionDeduplicationService(deduplicationKey));

        services.AddHttpClient<IPlaidClient, PlaidHttpClient>(client =>
            client.BaseAddress = new Uri(config["Plaid:BaseUrl"] ?? "https://sandbox.plaid.com"));
        services.AddSingleton<PlaidCategoryMapper>();
        services.AddScoped<PlaidAdapter>();
        services.AddScoped<IPlaidAdapter>(sp => sp.GetRequiredService<PlaidAdapter>());
        services.AddScoped<IBankProvider>(sp => sp.GetRequiredService<PlaidAdapter>());

        services.AddHttpClient<MonobankHttpClient>(client =>
            client.BaseAddress = new Uri(config["Monobank:BaseUrl"] ?? "https://api.monobank.ua"));
        services.AddSingleton<MonobankCategoryMapper>();
        services.AddScoped<IMonobankAdapter, MonobankAdapter>();
        services.AddScoped<MonobankAdapter>();
        services.AddScoped<IBankProvider>(sp => sp.GetRequiredService<MonobankAdapter>());

        services.AddScoped<IBankProviderFactory, BankProviderFactory>();

        services.AddSingleton<CorrelationIdAccessor>();
        services.AddScoped<ICorrelationIdAccessor>(sp => sp.GetRequiredService<CorrelationIdAccessor>());
        services.AddScoped<IBankSyncLogger, BankSyncLogger>();
        services.AddSingleton<IWebhookSignatureValidator, WebhookSignatureValidator>();
        services.AddSingleton<IPlaidErrorMapper, PlaidErrorMapper>();
        services.AddScoped<IScheduledSyncService, ScheduledSyncService>();
        services.AddScoped<ITransactionSyncCoordinator, TransactionSyncCoordinator>();

        services.AddScoped<IAggregationService, AggregationService>();
        services.AddScoped<IMoneyFlowStatisticsService, MoneyFlowStatisticsService>();
        services.AddScoped<IMerchantCategoryStatisticsService, MerchantCategoryStatisticsService>();
        services.AddScoped<IDashboardQueryService, DashboardQueryService>();
        services.AddScoped<ITransferDetectionService, TransferDetectionService>();
        services.AddScoped<IWealthAggregationService, WealthAggregationService>();

        services.AddScoped<ScheduledSyncJob>();
        services.AddScoped<SyncScheduler>();
        services.AddScoped<DataRetentionJob>();
        services.AddScoped<CredentialBackupJob>();
        services.AddScoped<UnusualSpendDetectionJob>();

        services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
        services.AddSingleton<IAuditLogService, AuditLogService>();
        services.AddScoped<EFQueryLoggerInterceptor>();

        services.AddSingleton<IJobRegistrar, JobRegistrar>();

        return services;
    }
}
