using System.Reflection;
using FluentValidation;
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
            RegisterDecoratedHandlers(
                services,
                assembly,
                typeof(ICommandHandler<,>),
                typeof(CommandValidationDecorator<,>));

            RegisterDecoratedHandlers(
                services,
                assembly,
                typeof(IQueryHandler<,>),
                typeof(QueryValidationDecorator<,>));

            RegisterClosedHandlers(services, assembly, typeof(IEventHandler<>));

            services.AddValidatorsFromAssembly(assembly);
        }

        return services;
    }

    private static void RegisterClosedHandlers(
        IServiceCollection services,
        Assembly assembly,
        Type openHandlerInterface)
    {
        foreach (var (impl, iface) in FindClosedHandlers(assembly, openHandlerInterface))
        {
            services.AddTransient(iface, impl);
        }
    }

    private static void RegisterDecoratedHandlers(
        IServiceCollection services,
        Assembly assembly,
        Type openHandlerInterface,
        Type openDecoratorType)
    {
        foreach (var (implementation, serviceType) in FindClosedHandlers(assembly, openHandlerInterface))
        {
            services.AddTransient(implementation);

            var typeArgs = serviceType.GetGenericArguments();
            var closedDecorator = openDecoratorType.MakeGenericType(typeArgs);

            services.AddTransient(serviceType, sp =>
            {
                var inner = sp.GetRequiredService(implementation);
                return ActivatorUtilities.CreateInstance(sp, closedDecorator, inner);
            });
        }
    }

    private static IEnumerable<(Type Implementation, Type ServiceType)> FindClosedHandlers(
        Assembly assembly,
        Type openHandlerInterface)
    {
        return assembly.GetTypes()
            .Where(t => t is {IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false})
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openHandlerInterface)
                .Select(i => (Implementation: t, ServiceType: i)));
    }
}
