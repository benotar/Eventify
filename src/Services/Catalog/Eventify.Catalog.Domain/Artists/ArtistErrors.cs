using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel;

namespace Eventify.Catalog.Domain.Artists;

public static class ArtistErrors
{
    public static Error NotFound(ArtistId id)
    {
        return Error.NotFound("Artist.NotFound", $"The artist with the Id = '{id.Value}' was not found");
    }
}
