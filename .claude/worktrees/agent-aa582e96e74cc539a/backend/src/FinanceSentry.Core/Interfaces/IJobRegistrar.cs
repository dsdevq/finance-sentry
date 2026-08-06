namespace FinanceSentry.Core.Interfaces;

public interface IJobRegistrar
{
    void RegisterJobs(IServiceProvider services);
}
