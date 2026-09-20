using Eventify.Catalog.Domain.Venues.ValueObjects;
using Eventify.SharedKernel;

namespace Eventify.Catalog.Domain.Venues;

public static class VenueErrors
{
    public static Error AlreadyExists(VenueName name)
    {
        return Error.Conflict("Venue.Conflict", $"The venue '{name.Value}' already exists");
    }

    public static Error NotFound(VenueId id)
    {
        return Error.NotFound("Venue.NotFound", $"The Venue with the Id = '{id.Value}' was not found");
    }
}
