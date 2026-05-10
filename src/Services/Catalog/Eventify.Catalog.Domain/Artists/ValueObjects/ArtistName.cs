using Eventify.SharedKernel.Domain.Exceptions;

namespace Eventify.Catalog.Domain.Artists.ValueObjects;

public record ArtistName
{
    public string Value { get; }

    private ArtistName(string value)
    {
        Value = value;
    }

    public static ArtistName Of(string value)
    {
        DomainException.ThrowIfNullOrWhiteSpace(value, "Artist name cannot be blank.");

        return new ArtistName(value);
    }
}
