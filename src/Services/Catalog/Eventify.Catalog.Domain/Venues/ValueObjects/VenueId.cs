using Eventify.SharedKernel.Domain.Exceptions;

namespace Eventify.Catalog.Domain.Venues.ValueObjects;

public sealed record VenueId
{
    public Guid Value { get; }

    private VenueId(Guid value)
    {
        Value = value;
    }

    public static VenueId Create(Guid value)
    {
        DomainException.ThrowIfEmpty(value, "VenueId cannot be blank");

        return new VenueId(value);
    }
}
