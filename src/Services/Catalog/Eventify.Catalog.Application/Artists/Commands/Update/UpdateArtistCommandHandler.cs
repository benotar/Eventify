using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel;
using Eventify.SharedKernel.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Artists.Commands.Update;

internal sealed class UpdateArtistCommandHandler : ICommandHandler<UpdateArtistCommand>
{
    private readonly IArtistDbContext _dbContext;

    public UpdateArtistCommandHandler(IArtistDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(UpdateArtistCommand command, CancellationToken ct)
    {
        var artistId = ArtistId.Create(command.Id);

        var artist = await _dbContext.Artists.FirstOrDefaultAsync(artist => artist.Id == artistId, ct);

        if (artist is null)
        {
            return Result.Failure(ArtistErrors.NotFound(artistId));
        }

        var artistName = ArtistName.Create(command.Name);

        artist.Update(artistName, command.Bio, command.ImageUrl);

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
