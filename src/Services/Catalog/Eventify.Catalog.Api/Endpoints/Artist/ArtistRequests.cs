namespace Eventify.Catalog.Api.Endpoints.Artist;

public sealed record CreateArtistRequest
{
    public required string Name { get; init; }
    public string? Bio { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed record UpdateArtistRequest
{
    public required string Name { get; init; }
    public string? Bio { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed record GetArtistsRequest(int Page = 1, int PageSize = 20);
