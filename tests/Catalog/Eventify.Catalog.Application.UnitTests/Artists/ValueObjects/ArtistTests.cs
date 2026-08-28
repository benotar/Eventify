using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.DomainEvents;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Domain.Exceptions;

namespace Eventify.Catalog.Application.UnitTests.Artists.ValueObjects;

public class ArtistTests
{
    private const string Name = "Giant Rooks";
    private const string? Bio = "Bio";
    private const string? ImageUrl = "https://example.com/image.jpg";

    private const string NewName = "Linkin Park";
    private const string NewBio = "Updated bio";
    private const string NewImageUrl = "https://example.com/updated.jpg";

    public static readonly TheoryData<string, string?, string?> ArtistProfileInfo = new TheoryData<string, string?, string?>
    {
        { Name, Bio, ImageUrl }, { Name, null, ImageUrl }, { Name, Bio, null }, { Name, null, null }
    };

    public static readonly TheoryData<string> ImageUrls = [null!, "", " "];

    [Theory]
    [MemberData(nameof(ArtistProfileInfo))]
    public void Create_Should_CreateArtist_WhenValidData(string name, string? bio, string? imageUrl)
    {
        // Arrange
        var artistName = ArtistName.Create(name);

        // Act
        var result = Artist.Create(artistName, bio, imageUrl);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBeNull();
        result.Id.Value.ShouldNotBe(Guid.Empty);
        result.Name.ShouldNotBeNull();
        result.Name.ShouldBe(artistName);
    }

    [Fact]
    public void UpdateProfile_Should_UpdateArtist_WhenIsValid()
    {
        // Arrange
        var artistName = ArtistName.Create(Name);
        var artist = Artist.Create(artistName, Bio, ImageUrl);
        var newArtistName = ArtistName.Create(NewName);

        // Act
        artist.UpdateProfile(newArtistName, NewBio);

        // Assert
        artist.Name.ShouldBe(newArtistName);
        artist.Bio.ShouldBe(NewBio);
    }

    [Theory]
    [MemberData(nameof(ImageUrls))]
    public void UpdateImageUrl_Should_ThrowException_WhenImageUrlIsNullOrEmpty(string imageUrl)
    {
        // Arrange
        var artistName = ArtistName.Create(Name);
        var artist = Artist.Create(artistName, Bio, ImageUrl);

        // Act
        var func = () => artist.UpdateImageUrl(imageUrl);

        // Assert
        func.ShouldThrow<DomainException>();
    }

    [Fact]
    public void UpdateImageUrl_Should_UpdateImageUrl_WhenIsValid()
    {
        // Arrange
        var artistName = ArtistName.Create(Name);
        var artist = Artist.Create(artistName, Bio, ImageUrl);

        // Act
        artist.UpdateImageUrl(NewImageUrl);

        // Assert
        artist.ImageUrl.ShouldBe(NewImageUrl);
    }

    [Fact]
    public void DeleteImageUrl_Should_DeleteImageUrl_WhenIsValid()
    {
        // Arrange
        var artistName = ArtistName.Create(Name);
        var artist = Artist.Create(artistName, Bio, ImageUrl);

        // Act
        artist.DeleteImageUrl();

        // Assert
        artist.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public void MarkDeleted_Should_RaiseArtistDeletedDomainEventWithCorrectArtistId_WhenCalled()
    {
        // Arrange
        var artistName = ArtistName.Create(Name);
        var artist = Artist.Create(artistName, Bio, ImageUrl);

        // Act
        artist.MarkDeleted();

        // Assert
        var domainEvent = artist.DomainEvents
            .OfType<ArtistDeletedDomainEvent>()
            .Single();

        domainEvent.ArtistId.ShouldBe(artist.Id);
    }
}
