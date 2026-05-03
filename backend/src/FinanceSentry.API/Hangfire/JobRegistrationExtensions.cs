namespace FinanceSentry.API.Hangfire;

using FinanceSentry.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public static class JobRegistrationExtensions
{
    public static WebApplication RegisterAllModuleJobs(this WebApplication app)
    {
        foreach (var registrar in app.Services.GetServices<IJobRegistrar>())
            registrar.RegisterJobs(app.Services);

        return app;
    }
}
