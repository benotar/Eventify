using Eventify.SharedKernel.Domain.Exceptions;
using Eventify.SharedKernel.Extensions;

namespace Eventify.Catalog.Domain.Artists.ValueObjects;

public readonly record struct ArtistId(Guid Value)
{
    public static ArtistId Of(Guid value)
    {
        return value.IsEmpty
            ? throw new DomainException("ArtistId cannot be empty.")
            : new ArtistId(value);
    }
}
