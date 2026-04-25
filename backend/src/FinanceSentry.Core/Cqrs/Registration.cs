using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceSentry.Core.Cqrs;

public static class CqrsServiceCollectionExtensions
{
    public static IServiceCollection AddCqrs(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddSingleton<IEventBus, EventBus>();

        foreach (var assembly in assemblies)
        {
            RegisterClosedHandlers(services, assembly, typeof(ICommandHandler<,>));
            RegisterClosedHandlers(services, assembly, typeof(IQueryHandler<,>));
            RegisterClosedHandlers(services, assembly, typeof(IEventHandler<>));
        }

        return services;
    }

    private static void RegisterClosedHandlers(
        IServiceCollection services,
        Assembly assembly,
        Type openHandlerInterface)
    {
        var matches = assembly.GetTypes()
            .Where(t => t is {IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false})
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openHandlerInterface)
                .Select(i => (Implementation: t, ServiceType: i)));

        foreach (var (implementation, serviceType) in matches)
        {
            services.AddTransient(serviceType, implementation);
        }
    }
}
