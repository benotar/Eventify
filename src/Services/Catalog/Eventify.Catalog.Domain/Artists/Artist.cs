using Eventify.Catalog.Domain.Artists.Events;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Domain;

namespace Eventify.Catalog.Domain.Artists;

public class Artist : AggregateRoot<ArtistId>
{
    public ArtistName Name { get; private set; } = default!;
    public string? Bio { get; private set; }
    public string? ImageUrl { get; private set; }

    // For EF Core
    private Artist()
    {
    }

    public static Artist Create(ArtistName name, string? bio = null, string? imageUrl = null)
    {
        var id = ArtistId.Of(Guid.CreateVersion7());

        var artist = new Artist { Id = id, Name = name, Bio = bio, ImageUrl = imageUrl };

        artist.RaiseDomainEvent(new ArtistCreatedDomainEvent(id));

        return artist;
    }

    public void Update(ArtistName artistName, string bio, string imageUrl)
    {
        Name = artistName;
        Bio = bio;
        ImageUrl = imageUrl;

        RaiseDomainEvent(new ArtistUpdatedDomainEvent(Id));
    }
}
