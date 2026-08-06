namespace FinanceSentry.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class InfrastructureModule
{
    public static IServiceCollection AddCoreInfrastructure(
        this IServiceCollection services, IConfiguration config) => services;
}
