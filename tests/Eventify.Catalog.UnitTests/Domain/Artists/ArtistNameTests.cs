using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Domain.Exceptions;
using FluentAssertions;

namespace Eventify.Catalog.UnitTests.Domain.Artists;

public class ArtistNameTests
{
    public static readonly TheoryData<string> OfTestData = [null!, "", " "];

    [Theory]
    [MemberData(nameof(OfTestData))]
    public void Of_WhenNameIsNullOrWhiteSpace_ShouldThrowDomainException(string name)
    {
        // Act 
        var act = () => ArtistName.Create(name);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Of_WhenNameIsValid_ShouldReturnArtistName()
    {
        // Arrange
        const string name = "Coldplay";

        // Act 
        var result = ArtistName.Create(name);

        // Assert
        result.Value.Should().Be(name);
    }
}
