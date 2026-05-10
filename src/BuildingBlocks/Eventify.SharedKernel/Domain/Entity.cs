namespace Eventify.SharedKernel.Domain;

public abstract class Entity<TId> : IAuditable
    where TId : notnull
{
    public TId Id { get; protected init; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    void IAuditable.SetCreatedAt(DateTimeOffset value)
    {
        CreatedAt = value;
    }

    void IAuditable.SetUpdatedAt(DateTimeOffset value)
    {
        UpdatedAt = value;
    }
}
