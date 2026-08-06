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
                openHandlerInterface: typeof(ICommandHandler<,>),
                openDecoratorTypes:
                [
                    typeof(CommandValidationDecorator<,>),
                    typeof(LoggingCommandDecorator<,>),
                ]);

            RegisterDecoratedHandlers(
                services,
                assembly,
                openHandlerInterface: typeof(IQueryHandler<,>),
                openDecoratorTypes:
                [
                    typeof(QueryValidationDecorator<,>),
                    typeof(LoggingQueryDecorator<,>),
                ]);

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

    /// <summary>
    /// Registers each closed handler under its concrete type, then composes the
    /// supplied open-generic decorators around it (first decorator wraps the impl,
    /// each subsequent decorator wraps the previous). The outermost decorator is
    /// what the consumer resolves through the service-type interface.
    /// </summary>
    private static void RegisterDecoratedHandlers(
        IServiceCollection services,
        Assembly assembly,
        Type openHandlerInterface,
        Type[] openDecoratorTypes)
    {
        foreach (var (implementation, serviceType) in FindClosedHandlers(assembly, openHandlerInterface))
        {
            services.AddTransient(implementation);

            var typeArgs = serviceType.GetGenericArguments();
            var closedDecorators = openDecoratorTypes.Select(t => t.MakeGenericType(typeArgs)).ToArray();

            services.AddTransient(serviceType, sp =>
            {
                object current = sp.GetRequiredService(implementation);
                foreach (var closedDecorator in closedDecorators)
                {
                    current = ActivatorUtilities.CreateInstance(sp, closedDecorator, current);
                }
                return current;
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
