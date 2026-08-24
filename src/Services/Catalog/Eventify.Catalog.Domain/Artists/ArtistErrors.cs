using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel;

namespace Eventify.Catalog.Domain.Artists;

public static class ArtistErrors
{
    public static Error AlreadyExists(ArtistName name)
    {
        return Error.Conflict("Artist.Conflict", $"The artist '{name.Value}' is already exists");
    }

    public static Error NotFound(ArtistId id)
    {
        return Error.NotFound("Artist.NotFound", $"The artist with the Id = '{id.Value}' was not found");
    }
}
