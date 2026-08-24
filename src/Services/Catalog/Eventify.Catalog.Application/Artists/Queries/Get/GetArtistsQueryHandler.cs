using Eventify.Catalog.Application.Interfaces;
using Eventify.SharedKernel;
using Eventify.SharedKernel.Application.Common;
using Eventify.SharedKernel.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Artists.Queries.Get;

public sealed class GetArtistsQueryHandler : IQueryHandler<GetArtistsQuery, PagedResult<ArtistResponse>>
{
    private readonly IArtistDbContext _dbContext;

    public GetArtistsQueryHandler(IArtistDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedResult<ArtistResponse>>> Handle(GetArtistsQuery query, CancellationToken cancellationToken)
    {
        var artistQuery = _dbContext.Artists.AsNoTracking();

        var totalCount = await artistQuery.CountAsync(cancellationToken);

        var artists = await artistQuery
            .OrderBy(artist => artist.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(artist => new ArtistResponse(artist.Id.Value,
                artist.Name.Value,
                artist.Bio,
                artist.ImageUrl))
            .ToListAsync(cancellationToken);

        return new PagedResult<ArtistResponse>(artists, query.Page, query.PageSize, totalCount);
    }
}
