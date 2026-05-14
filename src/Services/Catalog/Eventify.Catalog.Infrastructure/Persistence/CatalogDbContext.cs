using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists;
using Eventify.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Infrastructure.Persistence;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : BaseDbContext(options), IApplicationDbContext
{
    public DbSet<Artist> Artists { get; set; }
}
