using System.Collections.Concurrent;
using Eventify.SharedKernel.Application;
using Eventify.SharedKernel.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Eventify.SharedKernel.Infrastructure.DomainEvents;

public sealed class DomainEventsDispatcher : IDomainEventsDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    private static readonly ConcurrentDictionary<Type, Type> HandlerTypesDictionary = new ConcurrentDictionary<Type, Type>();
    private static readonly ConcurrentDictionary<Type, Type> WrapperTypeDictionary = new ConcurrentDictionary<Type, Type>();

    public DomainEventsDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            using var scope = _serviceProvider.CreateScope();

            var domainEventType = domainEvent.GetType();
            var handlerType =
                HandlerTypesDictionary.GetOrAdd(domainEventType, et => typeof(IDomainEventHandler<>).MakeGenericType(et));

            var handlers = scope.ServiceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }

                var handlerWrapper = HandlerWrapper.Create(handler, domainEventType);

                await handlerWrapper.HandleAsync(domainEvent, cancellationToken);
            }
        }
    }

    private abstract class HandlerWrapper
    {
        public abstract Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);

        public static HandlerWrapper Create(object handlerType, Type domainEventType)
        {
            var wrapperType = WrapperTypeDictionary.GetOrAdd(domainEventType,
                et => typeof(HandlerWrapper<>).MakeGenericType(et));

            return (HandlerWrapper)Activator.CreateInstance(wrapperType, handlerType);
        }
    }

    private sealed class HandlerWrapper<T> : HandlerWrapper where T : IDomainEvent
    {
        private readonly IDomainEventHandler<T> _handler;

        public HandlerWrapper(object handler)
        {
            _handler = (IDomainEventHandler<T>)handler;
        }

        public override async Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await _handler.HandleAsync((T)domainEvent, cancellationToken);
        }
    }
}
