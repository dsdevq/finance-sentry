namespace FinanceSentry.Modules.BankSync.Application.EventHandlers;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.BankSync.Domain.Events;

/// <summary>
/// Refreshes the user's net-worth snapshot after every successful account sync. Snapshots
/// are upserted per (user, day), so this keeps the chart's newest point tracking the live
/// position instead of freezing it at the first write of the day.
/// </summary>
public class FirstSyncSnapshotTrigger(
    INetWorthSnapshotJobScheduler jobScheduler) : IEventHandler<AccountSyncCompletedEvent>
{
    private readonly INetWorthSnapshotJobScheduler _jobScheduler = jobScheduler ?? throw new ArgumentNullException(nameof(jobScheduler));

    public Task Handle(AccountSyncCompletedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.Status == "success")
            _jobScheduler.ScheduleForUser(@event.UserId);
        return Task.CompletedTask;
    }
}
