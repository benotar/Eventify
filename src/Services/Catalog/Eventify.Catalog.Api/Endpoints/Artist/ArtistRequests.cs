namespace Eventify.Catalog.Api.Endpoints.Artist;

public sealed record CreateArtistRequest(string Name, string? Bio, string? ImageUrl);

public sealed record UpdateArtistRequest(string Name, string? Bio, string? ImageUrl);

public sealed record GetArtistsRequest(int Page = 1, int PageSize = 20);
