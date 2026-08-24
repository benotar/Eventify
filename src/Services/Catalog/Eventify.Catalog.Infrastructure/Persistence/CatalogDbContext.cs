using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Venues;
using Eventify.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Infrastructure.Persistence;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : BaseDbContext(options), IArtistDbContext, IVenueDbContext
{
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Venue> Venues { get; set; }
}
