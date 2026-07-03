namespace FinanceSentry.Modules.BankSync.Infrastructure.Jobs;

using Hangfire;
using Hangfire.InMemory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

public static class HangfireSetup
{
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

public class SyncScheduler(
    IBankAccountRepository accounts,
    IRecurringJobManager recurringJobs)
{
    private const string PerAccountCron = "*/30 * * * *";

    private readonly IBankAccountRepository _accounts = accounts;
    private readonly IRecurringJobManager _recurringJobs = recurringJobs;

    public async Task ScheduleAllActiveAccounts(CancellationToken ct = default)
    {
        var activeAccounts = await _accounts.GetAllActiveAsync(ct);
        var activeIds = new HashSet<Guid>(activeAccounts.Select(a => a.Id));

        foreach (var account in activeAccounts)
        {
            _recurringJobs.AddOrUpdate<ScheduledSyncJob>(
                $"sync-account-{account.Id}",
                job => job.ExecuteSyncAsync(account.Id),
                PerAccountCron);
        }
    }
}
