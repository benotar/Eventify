using Eventify.Catalog.Domain.Venues.ValueObjects;
using Eventify.SharedKernel.Domain;

namespace Eventify.Catalog.Domain.Venues;

public class Venue : AggregateRoot<VenueId>
{
    public VenueName Name { get; private set; }

    public Address Address { get; private set; }

    // Prevent the error "No suitable constructor was found for the type..." that EF Core may throw
    public Venue()
    {
    }

    private Venue(VenueId id, VenueName name, Address address)
    {
        Id = id;
        Name = name;
        Address = address;
    }

    public static Venue Create(VenueName name, Address address)
    {
        var id = VenueId.Create(Guid.CreateVersion7());

        return new Venue(id, name, address);
    }

    public void Update(VenueName name, Address address)
    {
        if (Name == name && Address == address)
        {
            return;
        }

        Name = name;
        Address = address;
    }
}
