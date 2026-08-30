using Eventify.Catalog.Application.Artists.Commands.Delete;
using Eventify.Catalog.Application.UnitTests.Abstractions;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.DomainEvents;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Application;
using Microsoft.EntityFrameworkCore;

namespace Eventify.Catalog.Application.UnitTests.Artists.Commands;

public class DeleteArtistCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenDoesNotExist()
    {
        // Arrange
        var dateTimeOffsetProvider = Substitute.For<IDateTimeOffsetProvider>();
        dateTimeOffsetProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        await using var dbContext = await CreateDbContextAsync(dateTimeOffsetProvider);

        var command = new DeleteArtistCommand { Id = Guid.CreateVersion7() };
        var handler = new DeleteArtistCommandHandler(dbContext);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        var artistId = ArtistId.Create(command.Id);
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ArtistErrors.NotFound(artistId));
    }

    [Fact]
    public async Task Handle_Should_RemoveArtistAndRaiseDomainEvent_WhenIsValid()
    {
        // Arrange
        var dateTimeOffsetProvider = Substitute.For<IDateTimeOffsetProvider>();
        dateTimeOffsetProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        await using var dbContext = await CreateDbContextAsync(dateTimeOffsetProvider);
        var seededArtist = await SeedArtistAsync(dbContext, "Bio");

        var command = new DeleteArtistCommand { Id = seededArtist.Id.Value };
        var handler = new DeleteArtistCommandHandler(dbContext);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var exists = await dbContext.Artists
            .AnyAsync(a => a.Id == ArtistId.Create(command.Id), TestContext.Current.CancellationToken);
        exists.ShouldBeFalse();
        seededArtist.DomainEvents.ShouldContain(domainEvent => domainEvent is ArtistDeletedDomainEvent);
    }
}
