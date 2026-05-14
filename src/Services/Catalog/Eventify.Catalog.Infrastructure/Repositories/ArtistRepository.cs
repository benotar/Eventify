using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Infrastructure.Repositories;

public class ArtistRepository : IArtistRepository
{
    private readonly CatalogDbContext _context;

    public ArtistRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Artist?> GetByIdAsync(ArtistId id, CancellationToken ct = default)
    {
        return await _context.Artists
            .FirstOrDefaultAsync(prop => prop.Id == id, ct);
    }

    public async Task<IReadOnlyList<Artist>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        return await _context.Artists
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        return await _context.Artists.CountAsync(ct);
    }

    public void Add(Artist artist)
    {
        _context.Artists.Add(artist);
    }

    public void Remove(Artist artist)
    {
        _context.Artists.Remove(artist);
    }
}
