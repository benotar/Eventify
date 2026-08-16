using Eventify.Catalog.Application.Artists.Commands.Create;
using FluentAssertions;
using Moq;

namespace Eventify.Catalog.UnitTests.Application.Artists.Commands;

public class CreateArtistCommandHandlerTests : ArtistCommandHandlerBase
{
    private readonly CreateArtistCommandHandler _sut;

    public CreateArtistCommandHandlerTests()
    {
        _sut = new CreateArtistCommandHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_ShouldCreateArtist()
    {
        // Arrange
        var command = new CreateArtistCommand("Eminem", "American rapper", "https://image.com/eminem.jpg");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        //result.IsError.Should().BeFalse();
        result.Value.Should().NotBeEmpty();

        // ArtistRepositoryMock.Verify(repo => repo.Add(It.Is<Artist>(artist =>
        //         artist.Name.Value == command.Name && artist.Bio == command.Bio && artist.ImageUrl == command.ImageUrl)),
        //     Times.Once);

        DbContextMock.Verify(dbContext => dbContext.SaveChangesAsync(CancellationToken.None), Times.Once);
    }
}
