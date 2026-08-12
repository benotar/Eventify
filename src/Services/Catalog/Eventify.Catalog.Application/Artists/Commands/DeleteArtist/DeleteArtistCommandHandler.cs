using ErrorOr;
using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Application.CQRS;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Artists.Commands.DeleteArtist;

public sealed class DeleteArtistCommandHandler : ICommandHandler<DeleteArtistCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteArtistCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ErrorOr<Success>> Handle(DeleteArtistCommand command, CancellationToken ct)
    {
        var artistId = ArtistId.Create(command.Id);

        var affectedRows = await _dbContext.Artists
            .Where(artist => artist.Id == artistId)
            .ExecuteDeleteAsync(ct);

        return affectedRows > 0
            ? Result.Success
            : Error.NotFound(description: CatalogConstants.ArtistNotFound);
    }
}
