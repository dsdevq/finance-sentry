namespace FinanceSentry.Modules.BankSync.Application.EventHandlers;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.BankSync.Domain.Events;

public class FirstSyncSnapshotTrigger(
    INetWorthSnapshotService snapshotService,
    INetWorthSnapshotJobScheduler jobScheduler) : IEventHandler<AccountSyncCompletedEvent>
{
    private readonly INetWorthSnapshotService _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
    private readonly INetWorthSnapshotJobScheduler _jobScheduler = jobScheduler ?? throw new ArgumentNullException(nameof(jobScheduler));

    public async Task Handle(AccountSyncCompletedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.Status != "success")
            return;

        if (await _snapshotService.HasSnapshotForCurrentMonthAsync(@event.UserId, cancellationToken))
            return;

        _jobScheduler.ScheduleForUser(@event.UserId);
    }
}
