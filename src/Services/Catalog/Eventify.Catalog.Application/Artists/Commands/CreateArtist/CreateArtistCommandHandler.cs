using ErrorOr;
using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Application.CQRS;

namespace Eventify.Catalog.Application.Artists.Commands.CreateArtist;

public sealed class CreateArtistCommandHandler : ICommandHandler<CreateArtistCommand, Guid>
{
    private readonly IArtistRepository _artistRepository;
    private readonly IApplicationDbContext _dbContext;

    public CreateArtistCommandHandler(IArtistRepository artistRepository, IApplicationDbContext dbContext)
    {
        _artistRepository = artistRepository;
        _dbContext = dbContext;
    }

    public async Task<ErrorOr<Guid>> Handle(CreateArtistCommand command, CancellationToken ct)
    {
        var name = ArtistName.Of(command.Name);

        var artist = Artist.Create(name, command.Bio, command.ImageUrl);

        _artistRepository.Add(artist);

        await _dbContext.SaveChangesAsync(ct);

        return artist.Id.Value;
    }
}
