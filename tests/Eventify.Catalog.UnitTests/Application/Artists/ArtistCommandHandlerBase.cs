using Eventify.Catalog.Application.Interfaces;
using Moq;

namespace Eventify.Catalog.UnitTests.Application.Artists;

public abstract class ArtistCommandHandlerBase
{
    protected readonly Mock<IApplicationDbContext> DbContextMock;

    protected ArtistCommandHandlerBase()
    {
        DbContextMock = new Mock<IApplicationDbContext>();
    }
}
