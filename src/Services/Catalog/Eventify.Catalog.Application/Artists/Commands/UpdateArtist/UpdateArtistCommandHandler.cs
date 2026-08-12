using ErrorOr;
using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Application.CQRS;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Artists.Commands.UpdateArtist;

public sealed class UpdateArtistCommandHandler : ICommandHandler<UpdateArtistCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateArtistCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async Task<ErrorOr<Success>> Handle(UpdateArtistCommand command, CancellationToken ct)
    {
        var artistId = ArtistId.Create(command.Id);

        var artist = await _dbContext.Artists.FirstOrDefaultAsync(artist => artist.Id == artistId, ct);

        if (artist is null)
        {
            return Error.NotFound(description: CatalogConstants.ArtistNotFound);
        }

        var artistName = ArtistName.Create(command.Name);

        artist.Update(artistName, command.Bio, command.ImageUrl);

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success;
    }
}
