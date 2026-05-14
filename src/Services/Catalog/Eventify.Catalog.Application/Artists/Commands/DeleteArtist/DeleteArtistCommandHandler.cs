using ErrorOr;
using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Application.CQRS;

namespace Eventify.Catalog.Application.Artists.Commands.DeleteArtist;

public sealed class DeleteArtistCommandHandler : ICommandHandler<DeleteArtistCommand>
{
    private readonly IArtistRepository _artistRepository;
    private readonly IApplicationDbContext _dbContext;

    public DeleteArtistCommandHandler(IArtistRepository artistRepository, IApplicationDbContext dbContext)
    {
        _artistRepository = artistRepository;
        _dbContext = dbContext;
    }

    public async Task<ErrorOr<Success>> Handle(DeleteArtistCommand command, CancellationToken ct)
    {
        var artistId = ArtistId.Of(command.Id);

        var artist = await _artistRepository.GetByIdAsync(artistId, ct);

        if (artist is null)
        {
            return Error.NotFound(description: CatalogConstants.ArtistNotFound);
        }

        artist.Delete();

        _artistRepository.Remove(artist);

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success;
    }
}
