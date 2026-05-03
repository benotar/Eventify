namespace Eventify.SharedKernel.Domain;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot, IClearableAggregate
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    void IClearableAggregate.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
