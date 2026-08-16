namespace Eventify.Catalog.Application.Artists.Queries;

public record ArtistResponse(Guid Id, string Name, string? Bio, string? ImageUrl);
