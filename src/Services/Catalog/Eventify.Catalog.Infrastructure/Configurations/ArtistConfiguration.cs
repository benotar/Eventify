using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eventify.Catalog.Infrastructure.Configurations;

public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.ToTable(SharedConstants.ArtistsTableName);

        builder.HasKey(artist => artist.Id);

        builder.Ignore(artist => artist.DomainEvents);

        builder.Property(prop => prop.Id)
            .HasConversion(id => id.Value, value => ArtistId.Create(value));

        builder.Property(artist => artist.Name)
            .HasConversion(name => name.Value, value => ArtistName.Create(value))
            .HasMaxLength(SharedConstants.MaxNameLength)
            .IsRequired();

        builder.HasIndex(artist => artist.Name);

        builder.Property(prop => prop.Bio)
            .HasMaxLength(SharedConstants.MaxBioLength);

        builder.Property(prop => prop.ImageUrl)
            .HasMaxLength(SharedConstants.MaxImageUrlLength);
    }
}
