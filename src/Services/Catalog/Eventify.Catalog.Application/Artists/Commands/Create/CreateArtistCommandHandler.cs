using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel;
using Eventify.SharedKernel.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Artists.Commands.Create;

internal sealed class CreateArtistCommandHandler : ICommandHandler<CreateArtistCommand, Guid>
{
    private readonly IArtistDbContext _dbContext;

    public CreateArtistCommandHandler(IArtistDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<Guid>> HandleAsync(CreateArtistCommand command, CancellationToken cancellationToken)
    {
        var name = ArtistName.Create(command.Name);

        var isAlreadyExists =
            await _dbContext.Artists.AnyAsync(artist => artist.Name.Equals(name),
                cancellationToken: cancellationToken);

        if (isAlreadyExists)
        {
            return Result.Failure<Guid>(ArtistErrors.AlreadyExists(name));
        }

        var artist = Artist.Create(name, command.Bio, command.ImageUrl);

        _dbContext.Artists.Add(artist);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return artist.Id.Value;
    }
}
