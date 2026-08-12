using ErrorOr;
using Eventify.Catalog.Application.Artists.Responses;
using Eventify.Catalog.Application.Interfaces;
using Eventify.SharedKernel.Application.Common;
using Eventify.SharedKernel.Application.CQRS;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Artists.Queries.GetArtists;

public sealed class GetArtistsQueryHandler : IQueryHandler<GetArtistsQuery, PagedResult<ArtistResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetArtistsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ErrorOr<PagedResult<ArtistResponse>>> Handle(GetArtistsQuery query, CancellationToken ct)
    {
        var artistQuery = _dbContext.Artists.AsNoTracking();

        var totalCount = await artistQuery.CountAsync(ct);

        var artists = await artistQuery
            .OrderBy(artist => artist.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(artist => new ArtistResponse(artist.Id.Value, artist.Name.Value, artist.Bio, artist.ImageUrl))
            .ToListAsync(ct);

        return new PagedResult<ArtistResponse>(artists, query.Page, query.PageSize, totalCount);
    }
}
