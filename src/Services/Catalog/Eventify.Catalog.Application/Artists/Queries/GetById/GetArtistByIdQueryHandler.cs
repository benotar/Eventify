using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel;
using Eventify.SharedKernel.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Artists.Queries.GetById;

public sealed class GetArtistByIdQueryHandler : IQueryHandler<GetArtistByIdQuery, ArtistResponse>
{
    private readonly IArtistDbContext _dbContext;

    public GetArtistByIdQueryHandler(IArtistDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ArtistResponse>> Handle(GetArtistByIdQuery query, CancellationToken cancellationToken)
    {
        var artistId = ArtistId.Create(query.Id);

        var artist = await _dbContext.Artists
            .AsNoTracking()
            .Where(artist => artist.Id == artistId)
            .Select(x => new ArtistResponse(x.Id.Value, x.Name.Value, x.Bio, x.ImageUrl))
            .SingleOrDefaultAsync(cancellationToken);

        if (artist is null)
        {
            return Result.Failure<ArtistResponse>(ArtistErrors.NotFound(artistId));
        }

        return artist;
    }
}
