using Eventify.SharedKernel.Domain;
using Eventify.SharedKernel.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Eventify.SharedKernel.Infrastructure.Interceptor;

public sealed class DispatchDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    public DispatchDomainEventsInterceptor(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            return;
        }

        var aggregates = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(a => a.Entity.DomainEvents.IsNotEmpty)
            .Select(a => a.Entity)
            .ToList();

        if (aggregates.IsEmpty)
        {
            return;
        }

        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();


        foreach (var aggregate in aggregates)
        {
            if (aggregate is IClearableAggregate clearable)
            {
                clearable.ClearDomainEvents();
            }
        }

        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
