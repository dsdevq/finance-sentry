namespace FinanceSentry.Core.Interfaces;

public interface INetWorthSnapshotJobScheduler
{
    void ScheduleForUser(Guid userId);
}
