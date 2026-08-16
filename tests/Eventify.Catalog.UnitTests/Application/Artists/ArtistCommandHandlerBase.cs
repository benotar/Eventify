using Eventify.Catalog.Application.Interfaces;
using Moq;

namespace Eventify.Catalog.UnitTests.Application.Artists;

public abstract class ArtistCommandHandlerBase
{
    protected readonly Mock<IArtistDbContext> DbContextMock;

    protected ArtistCommandHandlerBase()
    {
        DbContextMock = new Mock<IArtistDbContext>();
    }
}
