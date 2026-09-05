using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Domain.Exceptions;

namespace Eventify.Catalog.Application.UnitTests.Artists.ValueObjects;

public class ArtistNameTests
{
    public static readonly TheoryData<string> Names = [null!, "", " "];

    [Theory]
    [MemberData(nameof(Names))]
    public void Create_Should_ThrowDomainException_WhenIsNullOrWhiteSpace(string name)
    {
        // Act
        var func = () => ArtistName.Create(name);

        // Assert
        func.ShouldThrow<DomainException>();
    }

    [Fact]
    public void Create_Should_CreateArtistName_WhenIsValid()
    {
        // Arrange
        const string name = "Giant Rooks";

        // Act
        var result = ArtistName.Create(name);

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldBe(name);
    }
}
