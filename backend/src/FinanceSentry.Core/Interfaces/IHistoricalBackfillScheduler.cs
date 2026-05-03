namespace FinanceSentry.Core.Interfaces;

public interface IHistoricalBackfillScheduler
{
    void ScheduleForUser(Guid userId);
}
