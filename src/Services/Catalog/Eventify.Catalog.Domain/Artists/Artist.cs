using Eventify.Catalog.Domain.Artists.DomainEvents;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Domain;
using Eventify.SharedKernel.Domain.Exceptions;

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

    public void UpdateProfile(ArtistName artistName, string bio)
    {
        Name = artistName;
        Bio = bio;
    }

    public void UpdateImageUrl(string imageUrl)
    {
        DomainException.ThrowIfNullOrWhiteSpace(imageUrl, "The artist image url cannot be null or empty");

        ImageUrl = imageUrl;
    }

    public void DeleteImageUrl()
    {
        ImageUrl = null;
    }

    public void MarkDeleted()
    {
        RaiseDomainEvent(new ArtistDeletedDomainEvent(Id));
    }
}
