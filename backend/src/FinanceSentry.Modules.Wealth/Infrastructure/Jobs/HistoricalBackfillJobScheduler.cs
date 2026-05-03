namespace FinanceSentry.Modules.Wealth.Infrastructure.Jobs;

using FinanceSentry.Core.Interfaces;
using Hangfire;

public class HistoricalBackfillJobScheduler(IBackgroundJobClient jobClient) : IHistoricalBackfillScheduler
{
    private readonly IBackgroundJobClient _jobClient = jobClient ?? throw new ArgumentNullException(nameof(jobClient));

    public void ScheduleForUser(Guid userId)
        => _jobClient.Enqueue<HistoricalBackfillJob>(j => j.ExecuteForUserAsync(userId, CancellationToken.None));
}
