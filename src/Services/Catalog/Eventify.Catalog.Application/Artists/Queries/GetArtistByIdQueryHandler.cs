using ErrorOr;
using Eventify.Catalog.Application.Artists.Responses;
using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Application.CQRS;
using Eventify.SharedKernel.Extensions;

namespace Eventify.Catalog.Application.Artists.Queries;

public sealed class GetArtistByIdQueryHandler : IQueryHandler<GetArtistByIdQuery, ArtistResponse>
{
    private readonly IArtistRepository _artistRepository;

    public GetArtistByIdQueryHandler(IArtistRepository artistRepository)
    {
        _artistRepository = artistRepository;
    }

    public async Task<ErrorOr<ArtistResponse>> Handle(GetArtistByIdQuery query, CancellationToken ct)
    {
        if (query.Id.IsEmpty)
        {
            return Error.Validation(description: "Artist id is invalid.");
        }

        var artistId = ArtistId.Of(query.Id);

        var artist = await _artistRepository.GetByIdAsync(artistId, ct);

        if (artist is null)
        {
            return Error.NotFound(description: "Artist not found.");
        }

        return artist.ToResponse();
    }
}
