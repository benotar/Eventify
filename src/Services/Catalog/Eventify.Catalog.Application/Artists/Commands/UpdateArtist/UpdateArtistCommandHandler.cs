using ErrorOr;
using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Application.CQRS;

namespace Eventify.Catalog.Application.Artists.Commands.UpdateArtist;

public sealed class UpdateArtistCommandHandler : ICommandHandler<UpdateArtistCommand>
{
    private readonly IArtistRepository _artistRepository;
    private readonly IApplicationDbContext _dbContext;

    public UpdateArtistCommandHandler(IArtistRepository artistRepository, IApplicationDbContext dbContext)
    {
        _artistRepository = artistRepository;
        _dbContext = dbContext;
    }

    public async Task<ErrorOr<Success>> Handle(UpdateArtistCommand command, CancellationToken ct)
    {
        var artistId = ArtistId.Of(command.Id);

        var artist = await _artistRepository.GetByIdAsync(artistId, ct);

        if (artist is null)
        {
            return Error.NotFound(description: CatalogConstants.ArtistNotFound);
        }

        var artistName = ArtistName.Of(command.Name);

        artist.Update(artistName, command.Bio, command.ImageUrl);

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success;
    }
}
