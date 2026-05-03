namespace Eventify.SharedKernel.Domain;

internal interface IAuditable : IEntity
{
    void SetCreatedAt(DateTimeOffset value);
    void SetUpdatedAt(DateTimeOffset value);
}
