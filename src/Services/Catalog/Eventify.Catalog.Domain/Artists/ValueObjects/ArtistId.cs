using Eventify.SharedKernel.Domain.Exceptions;

namespace Eventify.Catalog.Domain.Artists.ValueObjects;

public sealed record ArtistId
{
    public Guid Value { get; }

    // // EF Core
    private ArtistId()
    {
    }

    private ArtistId(Guid value)
    {
        Value = value;
    }

    public static ArtistId Create(Guid value)
    {
        DomainException.ThrowIfEmpty(value, "ArtistId cannot be empty");

        return new ArtistId(value);
    }
}
