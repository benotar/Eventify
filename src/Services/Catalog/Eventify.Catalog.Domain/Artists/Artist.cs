using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Domain;

namespace Eventify.Catalog.Domain.Artists;

public class Artist : AggregateRoot<ArtistId>
{
    public ArtistName Name { get; private set; }
    public string? Bio { get; private set; }
    public string? ImageUrl { get; private set; }

    // Prevent the error "No suitable constructor was found for the type..." that EF Core can throw
    private Artist()
    {
    }

    private Artist(ArtistId id, ArtistName name, string? bio, string? imageUrl)
    {
        Id = id;
        Name = name;
        Bio = bio;
        ImageUrl = imageUrl;
    }

    public static Artist Create(ArtistName name, string? bio = null, string? imageUrl = null)
    {
        var id = ArtistId.Create(Guid.CreateVersion7());

        var artist = new Artist(id, name, bio, imageUrl);

        return artist;
    }

    public void Update(ArtistName artistName, string? bio, string? imageUrl)
    {
        if (Name == artistName && Bio == bio && ImageUrl == imageUrl)
        {
            return;
        }

        Name = artistName;
        Bio = bio;
        ImageUrl = imageUrl;
    }
}
