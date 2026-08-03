using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.Events;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Extensions;
using FluentAssertions;

namespace Eventify.Catalog.UnitTests.Domain.Artists;

public class ArtistTests
{
    private const string Name = "Coldplay";
    private const string Bio = "Some bio";
    private const string ImageUrl = "https://example.com/image.jpg";

    private const string UpdatedName = "Linkin Park";
    private const string UpdatedBio = "Updated bio";
    private const string UpdatedImageUrl = "https://example.com/updated.jpg";

    public static readonly TheoryData<string, string?, string?> ArtistTestData = new TheoryData<string, string?, string?>
    {
        { UpdatedName, Bio, ImageUrl },
        { Name, UpdatedBio, ImageUrl },
        { Name, null, ImageUrl },
        { Name, Bio, UpdatedImageUrl },
        { Name, Bio, null },
        { Name, UpdatedBio, UpdatedImageUrl },
        { UpdatedName, UpdatedBio, UpdatedImageUrl }
    };

    public static readonly TheoryData<string, string?, string?> NotChangedTestData = new TheoryData<string, string?, string?>
    {
        { Name, Bio, ImageUrl },
        { Name, null, ImageUrl },
        { Name, Bio, null },
        { Name, null, null }
    };

    [Theory]
    [MemberData(nameof(ArtistTestData))]
    public void Create_WhenValidData_ShouldSetProperties(string name, string? bio, string? imageUrl)
    {
        // Arrange
        var artistName = ArtistName.Of(name);

        // Act
        var result = Artist.Create(artistName, bio, imageUrl);

        // Assert
        result.Should().NotBeNull();
        result.Id.Value.IsEmpty.Should().BeFalse();
        result.Name.Should().Be(artistName);
        result.Bio.Should().Be(bio);
        result.ImageUrl.Should().Be(imageUrl);
    }

    [Fact]
    public void Create_WhenCalled_ShouldRaiseArtistCreatedDomainEvent()
    {
        // Arrange
        var artistName = ArtistName.Of(Name);

        // Act
        var result = Artist.Create(artistName, Bio, ImageUrl);

        // Assert
        result.DomainEvents.Should().NotBeEmpty();
        result.DomainEvents.Should().ContainSingle(e => e is ArtistCreatedDomainEvent)
            .Which.As<ArtistCreatedDomainEvent>()
            .ArtistId.Should().Be(result.Id);
    }

    [Theory]
    [MemberData(nameof(ArtistTestData))]
    public void Update_WhenDataChanged_ShouldUpdateProperties(string name, string? bio, string? imageUrl)
    {
        // Arrange
        var artistName = ArtistName.Of(Name);
        var artist = Artist.Create(artistName, Bio, ImageUrl);
        var updatedArtistName = ArtistName.Of(name);

        // Act
        artist.Update(updatedArtistName, bio, imageUrl);

        // Assert
        artist.Name.Should().Be(updatedArtistName);
        artist.Bio.Should().Be(bio);
        artist.ImageUrl.Should().Be(imageUrl);
    }

    [Theory]
    [MemberData(nameof(ArtistTestData))]
    public void Update_WhenDataChanged_ShouldRaiseArtistUpdatedDomainEvent(string name, string? bio, string? imageUrl)
    {
        // Arrange
        var artistName = ArtistName.Of(Name);
        var artist = Artist.Create(artistName, Bio, ImageUrl);
        var updatedArtistName = ArtistName.Of(name);

        // Act
        artist.Update(updatedArtistName, bio, imageUrl);

        // Assert
        artist.DomainEvents.Should().ContainSingle(e => e is ArtistUpdatedDomainEvent)
            .Which.As<ArtistUpdatedDomainEvent>()
            .ArtistId.Should().Be(artist.Id);
    }

    [Theory]
    [MemberData(nameof(NotChangedTestData))]
    public void Update_WhenDataNotChanged_ShouldNotRaiseDomainEvent(string name, string? bio, string? imageUrl)
    {
        // Arrange
        var artistName = ArtistName.Of(name);
        var artist = Artist.Create(artistName, bio, imageUrl);

        // Act
        artist.Update(artistName, bio, imageUrl);

        // Assert
        artist.DomainEvents.Should().NotContain(e => e is ArtistUpdatedDomainEvent);
        artist.Name.Should().Be(artistName);
        artist.Bio.Should().Be(bio);
        artist.ImageUrl.Should().Be(imageUrl);
    }

    [Fact]
    public void Delete_WhenCalled_ShouldRaiseArtistDeletedDomainEvent()
    {
        // Arrange
        var artistName = ArtistName.Of(Name);
        var artist = Artist.Create(artistName, Bio, ImageUrl);

        // Act
        artist.Delete();

        // Assert
        artist.DomainEvents.Should().ContainSingle(e => e is ArtistDeletedDomainEvent)
            .Which.As<ArtistDeletedDomainEvent>()
            .ArtistId.Should().Be(artist.Id);
    }
}
