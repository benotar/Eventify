using ErrorOr;
using Eventify.Catalog.Application.Artists.Commands.DeleteArtist;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using FluentAssertions;
using Moq;

namespace Eventify.Catalog.UnitTests.Application.Artists.Commands;

public class DeleteArtistCommandHandlerTests : ArtistCommandHandlerBase
{
    private readonly DeleteArtistCommandHandler _sut;
    private static DeleteArtistCommand Command => new DeleteArtistCommand(Guid.CreateVersion7());
    private static CancellationToken Ct => CancellationToken.None;

    public DeleteArtistCommandHandlerTests()
    {
        _sut = new DeleteArtistCommandHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenArtistDoesNotExist_ReturnsNotFound()
    {
        // Act
        var result = await _sut.Handle(Command, Ct);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);

        //ArtistRepositoryMock.Verify(repo => repo.Remove(It.IsAny<Artist>()), Times.Never);
        DbContextMock.Verify(db => db.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenArtistExists_ReturnsSuccess()
    {
        // Arrange
        var artist = Artist.Create(ArtistName.Create("Eminem"));
        // ArtistRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<ArtistId>(), Ct))
        //     .ReturnsAsync(artist);

        // Act
        var result = await _sut.Handle(Command, Ct);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeOfType<Success>();

        //ArtistRepositoryMock.Verify(repo => repo.Remove(artist), Times.Once);
        DbContextMock.Verify(db => db.SaveChangesAsync(Ct), Times.Once);
    }
}
