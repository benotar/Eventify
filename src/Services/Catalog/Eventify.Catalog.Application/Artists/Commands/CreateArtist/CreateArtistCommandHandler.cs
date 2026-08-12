using ErrorOr;
using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Application.CQRS;

namespace Eventify.Catalog.Application.Artists.Commands.CreateArtist;

public sealed class CreateArtistCommandHandler : ICommandHandler<CreateArtistCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateArtistCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ErrorOr<Guid>> Handle(CreateArtistCommand command, CancellationToken ct)
    {
        var name = ArtistName.Create(command.Name);

        var artist = Artist.Create(name, command.Bio, command.ImageUrl);

        _dbContext.Artists.Add(artist);

        await _dbContext.SaveChangesAsync(ct);

        return artist.Id.Value;
    }
}
