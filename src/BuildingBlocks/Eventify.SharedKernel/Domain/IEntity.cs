namespace Eventify.SharedKernel.Domain;

public interface IEntity
{
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset? UpdatedAt { get; }
}
