using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Infrastructure.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.UnitTests.Abstractions;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options, SqliteConnection connection)
    : DbContext(options), IArtistDbContext
{
    public DbSet<Artist> Artists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArtistConfiguration).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public async override ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await connection.DisposeAsync();
    }
}
