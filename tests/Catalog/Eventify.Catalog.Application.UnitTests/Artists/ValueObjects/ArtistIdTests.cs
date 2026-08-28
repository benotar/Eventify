using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Domain.Exceptions;

namespace Eventify.Catalog.Application.UnitTests.Artists.ValueObjects;

public class ArtistIdTests
{
    [Fact]
    public void Create_Should_ThrowDomainException_WhenIsEmpty()
    {
        // Arrange
        var id = Guid.Empty;

        // Act
        var func = () => ArtistId.Create(id);

        // Assert
        func.ShouldThrow<DomainException>();
    }

    [Fact]
    public void Create_Should_CreateAristId_WhenIsValid()
    {
        // Arrange
        var id = Guid.CreateVersion7();

        // Act
        var result = ArtistId.Create(id);

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldBe(id);
    }
}
