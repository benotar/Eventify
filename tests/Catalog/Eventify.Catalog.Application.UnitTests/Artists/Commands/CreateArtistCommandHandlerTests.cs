using Eventify.Catalog.Application.Artists.Commands.Create;
using Eventify.Catalog.Application.UnitTests.Abstractions;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Application;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.UnitTests.Artists.Commands;

public class CreateArtistCommandHandlerTests : BaseHandlerTest
{
    private const string Bio = """
                               Giant Rooks are a German indie rock band from Hamm, Germany founded in 2014.
                               In 2019, they won the 1Live Krone Award and the Preis für Popkultur in the months following the release of their EP Wild Stare.
                               Their debut album Rookery was released in August 2020.
                               """;

    private static CreateArtistCommand Command => new CreateArtistCommand { Name = Name, Bio = Bio };

    [Fact]
    public async Task Handle_Should_ReturnAlreadyExists_WhenAlreadyExists()
    {
        // Arrange
        var dateTimeOffsetProvided = Substitute.For<IDateTimeOffsetProvider>();
        dateTimeOffsetProvided.UtcNow.Returns(DateTimeOffset.Now);
        await using var dbContext = await CreateDbContextAsync(dateTimeOffsetProvided);
        var artist = await SeedArtistAsync(dbContext, Command.Bio, Command.ImageUrl);

        var handler = new CreateArtistCommandHandler(dbContext);

        // Act
        var result = await handler.Handle(Command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ArtistErrors.AlreadyExists(artist.Name));
    }

    [Fact]
    public async Task Handle_Should_PersistArtist_WhenValid()
    {
        // Arrange
        var dateTimeOffsetProvided = Substitute.For<IDateTimeOffsetProvider>();
        dateTimeOffsetProvided.UtcNow.Returns(DateTimeOffset.Now);

        await using var dbContext = await CreateDbContextAsync(dateTimeOffsetProvided);
        var handler = new CreateArtistCommandHandler(dbContext);

        // Act
        var result = await handler.Handle(Command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var savedArtist =
            await dbContext.Artists.SingleAsync(a => a.Id == ArtistId.Create(result.Value),
                TestContext.Current.CancellationToken);
        savedArtist.CreatedAt.ShouldBe(dateTimeOffsetProvided.UtcNow);
        savedArtist.UpdatedAt.ShouldBe(dateTimeOffsetProvided.UtcNow);
    }
}
