namespace FinanceSentry.Modules.BankSync.Infrastructure.Jobs;

using Hangfire;
using Hangfire.InMemory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

/// <summary>
/// Extension methods that wire up Hangfire for the BankSync module.
/// </summary>
public static class HangfireSetup
{
    /// <summary>
    /// Registers Hangfire with an in-memory storage back-end (development / test).
    /// For production, swap InMemory for Hangfire.PostgreSql or Hangfire.SqlServer.
    /// </summary>
    public static IServiceCollection AddHangfireServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddHangfire(cfg =>
            cfg.UseInMemoryStorage(new InMemoryStorageOptions
            {
                MaxExpirationTime = TimeSpan.FromHours(24)
            }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
            options.Queues = ["default"];
        });

        return services;
    }
}

/// <summary>
/// Schedules recurring sync jobs for all active accounts.
/// Call <see cref="ScheduleAllActiveAccounts"/> once at startup after the database is ready.
/// </summary>
public class SyncScheduler(
    IBankAccountRepository accounts,
    IRecurringJobManager recurringJobs)
{
    private readonly IBankAccountRepository _accounts = accounts;
    private readonly IRecurringJobManager _recurringJobs = recurringJobs;

    /// <summary>
    /// Registers (or updates) a recurring Hangfire job for every active bank account.
    /// Job cadence: every 2 hours (configurable via the cron expression).
    /// </summary>
    public async Task ScheduleAllActiveAccounts(CancellationToken ct = default)
    {
        var activeAccounts = await _accounts.GetAllActiveAsync(ct);

        foreach (var account in activeAccounts)
        {
            var jobId = $"sync-account-{account.Id}";

            _recurringJobs.AddOrUpdate<ScheduledSyncJob>(
                jobId,
                job => job.ExecuteSyncAsync(account.Id),
                Cron.HourInterval(2)); // every 2 hours
        }

        // Register monthly data retention job (T527 — FR-008)
        _recurringJobs.AddOrUpdate<DataRetentionJob>(
            "data-retention",
            job => job.RunAsync(false, CancellationToken.None),
            Cron.Monthly());

        // Register weekly credential backup job (T513)
        _recurringJobs.AddOrUpdate<CredentialBackupJob>(
            "credential-backup",
            job => job.RunAsync(CancellationToken.None),
            Cron.Weekly());
    }
}
