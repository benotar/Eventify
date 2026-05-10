using Eventify.Catalog.Domain.Artists;

namespace Eventify.Catalog.Application.Artists.Responses;

public record ArtistResponse(Guid Id, string Name, string? Bio, string? ImageUrl);

public static class ArtistResponseExtensions
{
    extension(Artist artist)
    {
        public ArtistResponse ToResponse()
        {
            return new ArtistResponse(artist.Id.Value, artist.Name.Value, artist.Bio, artist.ImageUrl);
        }
    }
}
