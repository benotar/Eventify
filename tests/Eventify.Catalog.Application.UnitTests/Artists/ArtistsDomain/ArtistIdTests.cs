// using Eventify.Catalog.Domain.Artists.ValueObjects;
// using Eventify.SharedKernel.Domain.Exceptions;
// using FluentAssertions;
//
// namespace Eventify.Catalog.Application.UnitTests.Domain.Artists;
//
// public class ArtistIdTests
// {
//     [Fact]
//     public void Of_WhenIdIsEmpty_ShouldThrowDomainException()
//     {
//         // Arrange
//         var id = Guid.Empty;
//
//         // Act
//         var act = () => ArtistId.Create(id);
//
//         // Assert
//         act.Should().Throw<DomainException>();
//     }
//
//     [Fact]
//     public void Of_WhenIdIsValid_ShouldReturnArtistId()
//     {
//         // Arrange
//         var id = Guid.CreateVersion7();
//
//         // Act
//         var result = ArtistId.Create(id);
//
//         // Assert
//         result.Value.Should().Be(id);
//     }
// }
