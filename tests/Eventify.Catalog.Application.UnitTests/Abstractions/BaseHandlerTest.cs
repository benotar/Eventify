using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Application;
using Eventify.SharedKernel.Infrastructure.Interceptor;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.UnitTests.Abstractions;

public abstract class BaseHandlerTest
{
    protected const string Name = "Giant Rooks";

    static async protected Task<TestDbContext> CreateDbContextAsync(
        IDateTimeOffsetProvider dateTimeOffsetProvider)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var updateAuditableInterceptor = new UpdateAuditableInterceptor(dateTimeOffsetProvider);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(updateAuditableInterceptor)
            .Options;

        var dbContext = new TestDbContext(options, connection);

        await dbContext.Database.EnsureCreatedAsync();

        return dbContext;
    }

    static async protected Task<Artist> SeedArtistAsync(TestDbContext dbContext, string? bio, string? imageUrl)
    {
        var artistName = ArtistName.Create(Name);
        var artist = Artist.Create(artistName, bio, imageUrl);

        dbContext.Artists.Add(artist);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return artist;
    }
}
