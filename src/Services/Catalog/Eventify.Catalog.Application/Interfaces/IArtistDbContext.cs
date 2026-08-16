using Eventify.Catalog.Domain.Artists;
using Eventify.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Interfaces;

public interface IArtistDbContext : IUnitOfWork
{
    DbSet<Artist> Artists { get; }
}
