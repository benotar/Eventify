using ErrorOr;
using Eventify.Catalog.Application.Artists.Responses;
using Eventify.Catalog.Application.Interfaces;
using Eventify.SharedKernel.Application.Common;
using Eventify.SharedKernel.Application.CQRS;

namespace Eventify.Catalog.Application.Artists.Queries.GetArtists;

public sealed class GetArtistsQueryHandler : IQueryHandler<GetArtistsQuery, PagedResult<ArtistResponse>>
{
    private readonly IArtistRepository _repository;

    public GetArtistsQueryHandler(IArtistRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<PagedResult<ArtistResponse>>> Handle(GetArtistsQuery query, CancellationToken ct)
    {
        var artists = await _repository.GetAllAsync(query.Page, query.PageSize, ct);

        var totalCount = await _repository.CountAsync(ct);

        var items = artists.Select(artist => artist.ToResponse())
            .ToList();

        return new PagedResult<ArtistResponse>(items, query.Page, query.PageSize, totalCount);
    }
}
