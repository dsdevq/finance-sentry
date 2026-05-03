namespace FinanceSentry.API.Modules;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ModuleRegistrationExtensions
{
    public static IServiceCollection AddAllModules(
        this IServiceCollection services, IConfiguration config)
    {
        var registrarType = typeof(IModuleRegistrar);

        var registrars = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => registrarType.IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
            .Select(t => (IModuleRegistrar)Activator.CreateInstance(t)!)
            .ToList();

        var moduleAssemblies = registrars.Select(r => r.GetType().Assembly).Distinct().ToArray();
        services.AddCqrs(moduleAssemblies);

        foreach (var registrar in registrars)
            registrar.Register(services, config);

        return services;
    }
}
