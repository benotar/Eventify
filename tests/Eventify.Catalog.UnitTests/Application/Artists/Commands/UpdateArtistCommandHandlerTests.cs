using ErrorOr;
using Eventify.Catalog.Application.Artists.Commands.UpdateArtist;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using FluentAssertions;
using Moq;

namespace Eventify.Catalog.UnitTests.Application.Artists.Commands;

public class UpdateArtistCommandHandlerTests : ArtistCommandHandlerBase
{
    private readonly UpdateArtistCommandHandler _sut;

    private static UpdateArtistCommand Command =>
        new UpdateArtistCommand(Guid.CreateVersion7(), "Coldplay", "Coldplay bio", null);

    private static CancellationToken Ct => CancellationToken.None;

    public UpdateArtistCommandHandlerTests()
    {
        _sut = new UpdateArtistCommandHandler(ArtistRepositoryMock.Object, DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenArtistDoesNotExist_ReturnsNotFound()
    {
        // Act
        var result = await _sut.Handle(Command, Ct);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);

        DbContextMock.Verify(db => db.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenArtistExists_ReturnsSuccess()
    {
        // Arrange
        var artist = Artist.Create(ArtistName.Of("Eminem"));
        ArtistRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<ArtistId>(), Ct))
            .ReturnsAsync(artist);

        // Act
        var result = await _sut.Handle(Command, Ct);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeOfType<Success>();

        DbContextMock.Verify(db => db.SaveChangesAsync(Ct), Times.Once);
    }
}
