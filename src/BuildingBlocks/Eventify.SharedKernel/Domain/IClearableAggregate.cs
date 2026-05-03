namespace Eventify.SharedKernel.Domain;

internal interface IClearableAggregate
{
    void ClearDomainEvents();
}
