using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel;
using Eventify.SharedKernel.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Artists.Commands.UpdateProfile;

internal sealed class UpdateArtistProfileCommandHandler : ICommandHandler<UpdateArtistProfileCommand>
{
    private readonly IArtistDbContext _dbContext;

    public UpdateArtistProfileCommandHandler(IArtistDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> HandleAsync(UpdateArtistProfileCommand profileCommand, CancellationToken cancellationToken)
    {
        var artistId = ArtistId.Create(profileCommand.Id);

        var artist = await _dbContext.Artists.SingleOrDefaultAsync(artist => artist.Id == artistId, cancellationToken);

        if (artist is null)
        {
            return Result.Failure(ArtistErrors.NotFound(artistId));
        }

        var artistName = ArtistName.Create(profileCommand.Name);

        artist.UpdateProfile(artistName, profileCommand.Bio);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
