using Eventify.SharedKernel.Domain.Exceptions;

namespace Eventify.Catalog.Domain.Venues.ValueObjects;

public sealed record VenueName
{
    public string Value { get; }

    private VenueName(string value)
    {
        Value = value;
    }

    public static VenueName Create(string value)
    {
        DomainException.ThrowIfNullOrWhiteSpace(value, "Venue name cannot be blank");

        return new VenueName(value);
    }
}
