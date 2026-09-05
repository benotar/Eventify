using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel;
using Eventify.SharedKernel.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Artists.Commands.Delete;

internal sealed class DeleteArtistCommandHandler : ICommandHandler<DeleteArtistCommand>
{
    private readonly IArtistDbContext _dbContext;

    public DeleteArtistCommandHandler(IArtistDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> HandleAsync(DeleteArtistCommand command, CancellationToken cancellationToken)
    {
        var artistId = ArtistId.Create(command.Id);

        var artist = await _dbContext.Artists.SingleOrDefaultAsync(a => a.Id == artistId, cancellationToken);

        if (artist is null)
        {
            return Result.Failure(ArtistErrors.NotFound(artistId));
        }

        _dbContext.Artists.Remove(artist);

        artist.MarkDeleted();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
