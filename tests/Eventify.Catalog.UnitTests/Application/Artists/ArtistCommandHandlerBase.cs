using Eventify.Catalog.Application.Interfaces;
using Moq;

namespace Eventify.Catalog.UnitTests.Application.Artists;

public abstract class ArtistCommandHandlerBase
{
    protected readonly Mock<IArtistRepository> ArtistRepositoryMock;
    protected readonly Mock<IApplicationDbContext> DbContextMock;

    protected ArtistCommandHandlerBase()
    {
        ArtistRepositoryMock = new Mock<IArtistRepository>();
        DbContextMock = new Mock<IApplicationDbContext>();
    }
}
