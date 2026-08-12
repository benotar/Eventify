using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Venues;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Artist> Artists { get; set; }
    DbSet<Venue> Venues { get; set; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
