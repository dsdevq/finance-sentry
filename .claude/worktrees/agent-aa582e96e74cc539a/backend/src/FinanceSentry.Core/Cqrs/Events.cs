using Microsoft.Extensions.DependencyInjection;

namespace FinanceSentry.Core.Cqrs;

public interface IEvent;

public interface IEventHandler<in TEvent>
    where TEvent : IEvent
{
    Task Handle(TEvent @event, CancellationToken cancellationToken);
}

public interface IEventBus
{
    Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}

public sealed class EventBus(IServiceProvider services) : IEventBus
{
    public async Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        await using var scope = services.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            await handler.Handle(@event, cancellationToken);
        }
    }
}
