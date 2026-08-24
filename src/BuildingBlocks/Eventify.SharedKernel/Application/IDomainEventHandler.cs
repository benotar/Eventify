using Eventify.SharedKernel.Domain;

namespace Eventify.SharedKernel.Application;

public interface IDomainEventHandler<in T> where T : IDomainEvent
{
    Task HandleAsync(T domainEvent, CancellationToken cancellationToken);
}
