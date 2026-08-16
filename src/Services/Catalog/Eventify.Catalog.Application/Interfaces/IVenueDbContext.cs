using Eventify.Catalog.Domain.Venues;
using Eventify.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.Interfaces;

public interface IVenueDbContext : IUnitOfWork
{
    DbSet<Venue> Venues { get; }
}
