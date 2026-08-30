using Eventify.Catalog.Application.Artists.Commands.UpdateProfile;
using Eventify.Catalog.Application.UnitTests.Abstractions;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Application;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.UnitTests.Artists.Commands;

public class UpdateArtistProfileCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenDoesNotExist()
    {
        // Arrange
        var dateTimeOffsetProvider = Substitute.For<IDateTimeOffsetProvider>();
        dateTimeOffsetProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        await using var dbContext = await CreateDbContextAsync(dateTimeOffsetProvider);

        var command = new UpdateArtistProfileCommand { Id = Guid.CreateVersion7(), Name = Name, Bio = "UpdatedBio" };
        var handler = new UpdateArtistProfileCommandHandler(dbContext);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        var artistId = ArtistId.Create(command.Id);
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ArtistErrors.NotFound(artistId));
    }

    [Fact]
    public async Task Handle_Should_UpdateProfile_WhenIsValid()
    {
        // Arrange
        var dateTimeOffsetProvider = Substitute.For<IDateTimeOffsetProvider>();
        dateTimeOffsetProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        await using var dbContext = await CreateDbContextAsync(dateTimeOffsetProvider);
        var originalArtist = await SeedArtistAsync(dbContext, "Original Bio", "Original ImageUrl");

        var command = new UpdateArtistProfileCommand { Id = originalArtist.Id.Value, Name = "Updated Name", Bio = "Updated Bio" };
        var handler = new UpdateArtistProfileCommandHandler(dbContext);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        dbContext.ChangeTracker.Clear();

        var updatedArtist =
            await dbContext.Artists.SingleAsync(a => a.Id == originalArtist.Id, TestContext.Current.CancellationToken);

        updatedArtist.Name.Value.ShouldBe(command.Name);
        updatedArtist.Bio.ShouldBe(command.Bio);
    }
}
