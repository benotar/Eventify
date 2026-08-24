using Eventify.SharedKernel.Domain;

namespace Eventify.SharedKernel.Infrastructure;

public interface IDomainEventsDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
