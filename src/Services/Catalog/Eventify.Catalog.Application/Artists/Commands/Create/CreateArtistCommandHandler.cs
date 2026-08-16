using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel;
using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.Application.Artists.Commands.Create;

internal sealed class CreateArtistCommandHandler : ICommandHandler<CreateArtistCommand, Guid>
{
    private readonly IArtistDbContext _dbContext;

    public CreateArtistCommandHandler(IArtistDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<Guid>> Handle(CreateArtistCommand command, CancellationToken cancellationToken)
    {
        var name = ArtistName.Create(command.Name);

        var artist = Artist.Create(name, command.Bio, command.ImageUrl);

        _dbContext.Artists.Add(artist);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return artist.Id.Value;
    }
}
