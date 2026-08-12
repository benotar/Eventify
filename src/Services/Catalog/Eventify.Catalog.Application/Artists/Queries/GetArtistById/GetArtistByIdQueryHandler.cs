using ErrorOr;
using Eventify.Catalog.Application.Artists.Responses;
using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Application.CQRS;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Artists.Queries.GetArtistById;

public sealed class GetArtistByIdQueryHandler : IQueryHandler<GetArtistByIdQuery, ArtistResponse>
{
    private readonly IApplicationDbContext _dbContext;

    public GetArtistByIdQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ErrorOr<ArtistResponse>> Handle(GetArtistByIdQuery query, CancellationToken ct)
    {
        var artistId = ArtistId.Create(query.Id);

        var artist = await _dbContext.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(artist => artist.Id == artistId, ct);

        if (artist is null)
        {
            return Error.NotFound(description: CatalogConstants.ArtistNotFound);
        }

        return artist.ToResponse();
    }
}
