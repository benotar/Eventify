using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;

namespace Eventify.Catalog.Application.Interfaces;

public interface IArtistRepository
{
    Task<Artist?> GetByIdAsync(ArtistId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Artist>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(Artist artist);

    void Remove(Artist artist);
}
