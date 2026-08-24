using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel;
using Eventify.SharedKernel.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Artists.Commands.DeleteImageUrl;

internal sealed class DeleteArtistImageUrlCommandHandler : ICommandHandler<DeleteArtistImageUrlCommand>
{
    private readonly IArtistDbContext _dbContext;

    public DeleteArtistImageUrlCommandHandler(IArtistDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(DeleteArtistImageUrlCommand command, CancellationToken cancellationToken)
    {
        var artistId = ArtistId.Create(command.Id);

        var artist = await _dbContext.Artists.SingleOrDefaultAsync(a => a.Id == artistId, cancellationToken);

        if (artist is null)
        {
            return Result.Failure(ArtistErrors.NotFound(artistId));
        }

        artist.DeleteImageUrl();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
